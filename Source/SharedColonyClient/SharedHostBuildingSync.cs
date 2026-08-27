using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RTClient.Managers;
using RTNetwork.Components;
using RTNetwork.Packets;
using Verse;

namespace RWTSharedColony
{
    /// <summary>
    /// The canonical map host owns non-pawn map truth. v0.1.23 starts that
    /// authority with buildings: the host publishes a compact manifest and the
    /// guest creates/updates/removes the corresponding remote building mirrors.
    /// Pawns remain owner-authoritative through SharedPawnStateSync.
    /// </summary>
    public static class SharedHostBuildingSync
    {
        public const int HostBuildingActionCode = 9030;
        private const int BuildingMagic = 0x524A4233; // RJB3
        private const long ScanIntervalTicks = TimeSpan.TicksPerSecond;
        private const long FullRepublishIntervalTicks = TimeSpan.TicksPerSecond * 30;

        private static long _lastScanUtcTicks;
        private static long _lastFullPublishUtcTicks;
        private static ulong _lastPublishedHash;
        private static bool _hasPublishedHash;
        private static readonly Dictionary<string, Thing> HostAliases =
            new Dictionary<string, Thing>(StringComparer.Ordinal);
        private static readonly HashSet<string> PreviousHostIds =
            new HashSet<string>(StringComparer.Ordinal);

        public static void Update()
        {
            if (!SharedTileLiveSync.IsSharedSessionActive ||
                !SessionManager.IsSynchronousHost ||
                SessionManager.SynchronousMap == null ||
                Network.ServerEndpoint == null ||
                !RimjobProtocolState.PrivateSyncReady)
            {
                _lastScanUtcTicks = 0;
                _lastFullPublishUtcTicks = 0;
                _lastPublishedHash = 0;
                _hasPublishedHash = false;
                return;
            }

            long now = DateTime.UtcNow.Ticks;
            if (_lastScanUtcTicks != 0 && now - _lastScanUtcTicks < ScanIntervalTicks)
                return;
            _lastScanUtcTicks = now;

            SendHostBuildingManifest(SessionManager.SynchronousMap, now);
        }

        public static bool TryHandleAction(PKT_Synchronous packet)
        {
            if (packet == null || packet.CurrentStepMode != PKT_Synchronous.StepMode.Action)
                return false;
            if (Convert.ToInt32(packet.CurrentActionType) != HostBuildingActionCode)
                return false;

            ApplyHostBuildingManifest(packet.Data);
            return true;
        }

        private static void SendHostBuildingManifest(Map map, long now)
        {
            try
            {
                List<Thing> buildings = map.listerThings.AllThings
                    .Where(IsBuildingLike)
                    .Take(12000)
                    .ToList();

                ulong manifestHash = ComputeManifestHash(buildings);
                bool unchanged = _hasPublishedHash && manifestHash == _lastPublishedHash;
                if (unchanged && _lastFullPublishUtcTicks != 0 &&
                    now - _lastFullPublishUtcTicks < FullRepublishIntervalTicks)
                    return;

                using (MemoryStream stream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(BuildingMagic);
                    writer.Write(SessionManager.Username ?? string.Empty);
                    writer.Write(map.Tile.tileId);
                    writer.Write(buildings.Count);

                    foreach (Thing thing in buildings)
                    {
                        writer.Write(thing.ThingID ?? string.Empty);
                        writer.Write(thing.def?.defName ?? string.Empty);
                        writer.Write(thing.Stuff?.defName ?? string.Empty);
                        writer.Write(thing.Position.x);
                        writer.Write(thing.Position.z);
                        writer.Write(thing.Rotation.AsInt);
                        writer.Write(SafeHitPoints(thing));
                        writer.Write(Math.Max(1, thing.stackCount));
                    }
                    writer.Flush();

                    PKT_Synchronous packet = new PKT_Synchronous
                    {
                        CurrentStepMode = PKT_Synchronous.StepMode.Action,
                        CurrentActionType = (PKT_Synchronous.ActionType)HostBuildingActionCode,
                        Data = stream.ToArray()
                    };
                    Network.ServerEndpoint.EnqueuePacket(PacketHeader.Synchronous, packet);
                    _lastPublishedHash = manifestHash;
                    _hasPublishedHash = true;
                    _lastFullPublishUtcTicks = now;
                    RimjobClientDiagnostics.Verbose(
                        $"Host building manifest sent. Count={buildings.Count}; Tile={map.Tile.tileId}; Bytes={stream.Length}; Changed={!unchanged}.");
                }
            }
            catch (Exception exception)
            {
                RimjobClientDiagnostics.Error("Host building manifest send failed: " + exception);
            }
        }

        private static void ApplyHostBuildingManifest(byte[] payload)
        {
            if (!SharedTileLiveSync.SharedGuestActive ||
                SessionManager.SynchronousMap == null ||
                payload == null)
                return;

            try
            {
                Map map = SessionManager.SynchronousMap;
                using (MemoryStream stream = new MemoryStream(payload, false))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (reader.ReadInt32() != BuildingMagic) return;
                    string owner = reader.ReadString();
                    int tile = reader.ReadInt32();
                    int count = reader.ReadInt32();
                    if (string.IsNullOrWhiteSpace(owner) || tile != map.Tile.tileId || count < 0 || count > 12000)
                        return;
                    if (!string.IsNullOrWhiteSpace(SharedTileLiveSync.PendingHostUsername) &&
                        !string.Equals(owner, SharedTileLiveSync.PendingHostUsername, StringComparison.OrdinalIgnoreCase))
                        return;

                    Faction hostFaction = PlayerFactionRegistry.GetOrCreate(owner);
                    HashSet<string> currentHostIds = new HashSet<string>(StringComparer.Ordinal);
                    int created = 0;
                    int updated = 0;

                    for (int index = 0; index < count; index++)
                    {
                        string hostThingId = reader.ReadString();
                        string defName = reader.ReadString();
                        string stuffName = reader.ReadString();
                        IntVec3 position = new IntVec3(reader.ReadInt32(), 0, reader.ReadInt32());
                        int rotation = reader.ReadInt32();
                        int hitPoints = reader.ReadInt32();
                        int stackCount = reader.ReadInt32();

                        if (string.IsNullOrWhiteSpace(hostThingId) || string.IsNullOrWhiteSpace(defName))
                            continue;
                        currentHostIds.Add(hostThingId);

                        Thing thing = FindHostMirror(map, hostThingId, defName, position);
                        if (thing == null)
                        {
                            ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                            if (def == null) continue;
                            ThingDef stuff = string.IsNullOrWhiteSpace(stuffName)
                                ? null
                                : DefDatabase<ThingDef>.GetNamedSilentFail(stuffName);

                            try
                            {
                                thing = ThingMaker.MakeThing(def, stuff);
                                if (thing == null) continue;
                                thing.Rotation = new Rot4(rotation);
                                if (def.CanHaveFaction && hostFaction != null)
                                    thing.SetFactionDirect(hostFaction);
                                RimworldManager.SetDirectThingIntoMap(thing, map, ClampToMap(map, position));
                                HostAliases[hostThingId] = thing;
                                created++;
                            }
                            catch (Exception createException)
                            {
                                RimjobClientDiagnostics.Verbose(
                                    $"Could not create host building mirror {defName} at {position}: {createException.Message}");
                                continue;
                            }
                        }
                        else
                        {
                            HostAliases[hostThingId] = thing;
                            updated++;
                        }

                        if (thing.def?.CanHaveFaction == true && hostFaction != null && thing.Faction != hostFaction)
                            thing.SetFactionDirect(hostFaction);
                        thing.Rotation = new Rot4(rotation);
                        ApplyHitPoints(thing, hitPoints);
                        if (thing.def?.category != ThingCategory.Building)
                            thing.stackCount = Math.Max(1, stackCount);
                    }

                    // Remove mirrors that the host previously advertised but no
                    // longer owns. We only delete known host aliases, never an
                    // arbitrary guest-local object, so local commands get a chance
                    // to reach the host before the next canonical manifest.
                    foreach (string removedId in PreviousHostIds.Where(id => !currentHostIds.Contains(id)).ToArray())
                    {
                        if (HostAliases.TryGetValue(removedId, out Thing stale) && stale != null && !stale.Destroyed)
                        {
                            try { stale.Destroy(DestroyMode.Vanish); }
                            catch { }
                        }
                        HostAliases.Remove(removedId);
                    }

                    PreviousHostIds.Clear();
                    foreach (string id in currentHostIds) PreviousHostIds.Add(id);

                    RimjobClientDiagnostics.Verbose(
                        $"Host building manifest applied. Owner={owner}; Created={created}; Updated={updated}; Total={count}; Tile={tile}.");
                }
            }
            catch (Exception exception)
            {
                RimjobClientDiagnostics.Error("Host building manifest apply failed: " + exception);
            }
        }

        private static bool IsBuildingLike(Thing thing) =>
            thing != null &&
            !thing.Destroyed &&
            thing.Spawned &&
            thing.def != null &&
            thing.def.category == ThingCategory.Building;

        private static ulong ComputeManifestHash(IEnumerable<Thing> buildings)
        {
            const ulong offset = 14695981039346656037UL;
            ulong hash = offset;
            foreach (Thing thing in buildings)
            {
                AddHash(ref hash, thing.ThingID);
                AddHash(ref hash, thing.def?.defName);
                AddHash(ref hash, thing.Stuff?.defName);
                AddHash(ref hash, thing.Position.x);
                AddHash(ref hash, thing.Position.z);
                AddHash(ref hash, thing.Rotation.AsInt);
                AddHash(ref hash, SafeHitPoints(thing));
                AddHash(ref hash, Math.Max(1, thing.stackCount));
            }
            return hash;
        }

        private static void AddHash(ref ulong hash, string value)
        {
            const ulong prime = 1099511628211UL;
            if (value != null)
            {
                foreach (char character in value)
                {
                    hash ^= character;
                    hash *= prime;
                }
            }
            hash ^= 0xff;
            hash *= prime;
        }

        private static void AddHash(ref ulong hash, int value)
        {
            const ulong prime = 1099511628211UL;
            unchecked
            {
                hash ^= (byte)value;
                hash *= prime;
                hash ^= (byte)(value >> 8);
                hash *= prime;
                hash ^= (byte)(value >> 16);
                hash *= prime;
                hash ^= (byte)(value >> 24);
                hash *= prime;
            }
        }

        private static Thing FindHostMirror(Map map, string hostThingId, string defName, IntVec3 position)
        {
            if (HostAliases.TryGetValue(hostThingId, out Thing alias) &&
                alias != null && !alias.Destroyed && alias.Map == map)
                return alias;

            Thing exact = map.listerThings.AllThings.FirstOrDefault(thing =>
                thing != null && !thing.Destroyed && string.Equals(thing.ThingID, hostThingId, StringComparison.Ordinal));
            if (exact != null) return exact;

            // When a mirror was reconstructed locally its generated ThingID can
            // differ from the host ID. Def + cell is stable enough for buildings.
            return map.thingGrid.ThingsListAtFast(ClampToMap(map, position))
                .FirstOrDefault(thing =>
                    thing != null && !thing.Destroyed && thing.def != null &&
                    string.Equals(thing.def.defName, defName, StringComparison.Ordinal));
        }

        private static int SafeHitPoints(Thing thing)
        {
            try { return thing.HitPoints; }
            catch { return -1; }
        }

        private static void ApplyHitPoints(Thing thing, int hitPoints)
        {
            if (thing == null || hitPoints < 0) return;
            try { thing.HitPoints = Math.Max(1, Math.Min(thing.MaxHitPoints, hitPoints)); }
            catch { }
        }

        private static IntVec3 ClampToMap(Map map, IntVec3 position)
        {
            return new IntVec3(
                Math.Max(0, Math.Min(map.Size.x - 1, position.x)),
                0,
                Math.Max(0, Math.Min(map.Size.z - 1, position.z)));
        }
    }

    [HarmonyPatch]
    public static class SharedHostBuildingUpdatePatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("Verse.Root_Play"), "Update");

        [HarmonyPostfix]
        public static void Postfix() => SharedHostBuildingSync.Update();
    }

    [HarmonyPatch]
    public static class SharedHostBuildingRoutePatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(
                AccessTools.TypeByName("RTClient.PacketManagers.Synchronous.PM_Synchronous"),
                "RouteToManager");

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(object[] __args)
        {
            PKT_Synchronous packet = __args?.OfType<PKT_Synchronous>().FirstOrDefault();
            return packet == null || !SharedHostBuildingSync.TryHandleAction(packet);
        }
    }
}
