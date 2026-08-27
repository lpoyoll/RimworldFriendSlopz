using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RTClient.Managers;
using RTClient.Misc;
using RTNetwork.Components;
using RTNetwork.Packets;
using Verse;

namespace RWTSharedColony
{
    /// <summary>
    /// Rimjob pawn mirror transport.
    ///
    /// The normal RWT synchronous protocol forwards jobs/draft/etc, but two
    /// separate RimWorld simulations can still disagree about a pawn's exact
    /// location. Each player is therefore authoritative for their own pawns.
    /// We publish a lightweight position snapshot several times per second and
    /// an occasional full pawn manifest so missing remote mirrors can be rebuilt.
    /// </summary>
    public static class SharedPawnStateSync
    {
        public const int PawnStateActionCode = 9022;
        public const int PawnManifestActionCode = 9023;

        private const int StateMagic = 0x52505332;    // RPS2
        private const int ManifestMagic = 0x52504D32; // RPM2
        private const long StateIntervalTicks = TimeSpan.TicksPerMillisecond * 100;
        private const long ManifestIntervalTicks = TimeSpan.TicksPerSecond * 2;

        private static long _lastStateUtcTicks;
        private static long _lastManifestUtcTicks;
        private static bool _wasActive;
        private static readonly HashSet<string> MissingMirrorWarnings = new HashSet<string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Pawn> RemotePawnAliases =
            new Dictionary<string, Pawn>(StringComparer.OrdinalIgnoreCase);

        public static void Update()
        {
            try
            {
                bool active = SharedTileLiveSync.IsSharedSessionActive &&
                              SessionManager.SynchronousMap != null &&
                              Network.ServerEndpoint != null;

                if (!active)
                {
                    _wasActive = false;
                    _lastStateUtcTicks = 0;
                    _lastManifestUtcTicks = 0;
                    MissingMirrorWarnings.Clear();
                    RemotePawnAliases.Clear();
                    return;
                }

                long now = DateTime.UtcNow.Ticks;
                if (!_wasActive)
                {
                    _wasActive = true;
                    _lastStateUtcTicks = 0;
                    _lastManifestUtcTicks = 0;
                    RimjobClientDiagnostics.Important(
                        $"Pawn authority sync active. Owner={SessionManager.Username ?? "<unknown>"}; Tile={SessionManager.SynchronousMap.Tile.tileId}");
                }

                if (_lastManifestUtcTicks == 0 || now - _lastManifestUtcTicks >= ManifestIntervalTicks)
                {
                    SendManifest();
                    _lastManifestUtcTicks = now;
                }

                if (_lastStateUtcTicks == 0 || now - _lastStateUtcTicks >= StateIntervalTicks)
                {
                    SendState();
                    _lastStateUtcTicks = now;
                }
            }
            catch (Exception exception)
            {
                RimjobClientDiagnostics.Error("Pawn authority update failed: " + exception);
            }
        }

        public static bool TryHandleAction(PKT_Synchronous packet)
        {
            if (packet == null || packet.CurrentStepMode != PKT_Synchronous.StepMode.Action)
                return false;

            int code = Convert.ToInt32(packet.CurrentActionType);
            if (code == PawnStateActionCode)
            {
                ApplyState(packet.Data);
                return true;
            }

            if (code == PawnManifestActionCode)
            {
                ApplyManifest(packet.Data);
                return true;
            }

            return false;
        }

        private static void SendState()
        {
            Map map = SessionManager.SynchronousMap;
            if (map?.mapPawns == null || Network.ServerEndpoint == null) return;

            List<Pawn> pawns = GetLocallyOwnedPawns(map);
            if (pawns.Count == 0) return;

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(StateMagic);
                writer.Write(SessionManager.Username ?? string.Empty);
                writer.Write(map.Tile.tileId);
                writer.Write(pawns.Count);
                foreach (Pawn pawn in pawns)
                {
                    writer.Write(pawn.ThingID ?? string.Empty);
                    writer.Write(pawn.Position.x);
                    writer.Write(pawn.Position.z);
                }
                writer.Flush();

                EnqueueCustomAction(PawnStateActionCode, stream.ToArray());
                RimjobClientDiagnostics.Verbose(
                    $"Pawn state sent. Owner={SessionManager.Username ?? "<unknown>"}; Count={pawns.Count}; Tile={map.Tile.tileId}");
            }
        }

        private static void SendManifest()
        {
            Map map = SessionManager.SynchronousMap;
            if (map?.mapPawns == null || Network.ServerEndpoint == null) return;

            List<Pawn> pawns = GetLocallyOwnedPawns(map);
            if (pawns.Count == 0) return;

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(ManifestMagic);
                writer.Write(SessionManager.Username ?? string.Empty);
                writer.Write(map.Tile.tileId);
                writer.Write(pawns.Count);
                foreach (Pawn pawn in pawns)
                {
                    writer.Write(pawn.ThingID ?? string.Empty);
                    writer.Write(pawn.Position.x);
                    writer.Write(pawn.Position.z);
                    string serialized = ScribeManager.SerializeToString(
                        pawn,
                        ScribeManager.SerializableType.Pawn,
                        -1,
                        pawn.ThingID);
                    writer.Write(serialized ?? string.Empty);
                }
                writer.Flush();

                EnqueueCustomAction(PawnManifestActionCode, stream.ToArray());
                RimjobClientDiagnostics.Important(
                    $"Pawn manifest sent. Owner={SessionManager.Username ?? "<unknown>"}; Count={pawns.Count}; Tile={map.Tile.tileId}");
            }
        }

        private static void EnqueueCustomAction(int actionCode, byte[] payload)
        {
            PKT_Synchronous packet = new PKT_Synchronous
            {
                CurrentStepMode = PKT_Synchronous.StepMode.Action,
                CurrentActionType = (PKT_Synchronous.ActionType)actionCode,
                Data = payload
            };
            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Synchronous, packet);
        }

        private static List<Pawn> GetLocallyOwnedPawns(Map map)
        {
            return map.mapPawns.AllPawns
                .Where(pawn => pawn != null &&
                               !pawn.Destroyed &&
                               pawn.Spawned &&
                               pawn.Faction == Faction.OfPlayer)
                .ToList();
        }

        private static void ApplyState(byte[] payload)
        {
            if (!TryGetSharedMap(out Map map) || payload == null) return;

            try
            {
                using (MemoryStream stream = new MemoryStream(payload, false))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (reader.ReadInt32() != StateMagic) return;
                    string owner = reader.ReadString();
                    int tile = reader.ReadInt32();
                    int count = reader.ReadInt32();
                    if (!ValidateRemoteEnvelope(map, owner, tile, count)) return;

                    Faction remoteFaction = PlayerFactionRegistry.GetOrCreate(owner);
                    int applied = 0;
                    for (int index = 0; index < count; index++)
                    {
                        string thingId = reader.ReadString();
                        IntVec3 position = new IntVec3(reader.ReadInt32(), 0, reader.ReadInt32());
                        Pawn pawn = FindRemotePawn(map, owner, thingId, remoteFaction);
                        if (pawn == null)
                        {
                            if (MissingMirrorWarnings.Add(owner + "|" + thingId))
                                RimjobClientDiagnostics.Important(
                                    $"Remote pawn mirror missing for state update. Owner={owner}; ThingID={thingId}; waiting for manifest.");
                            continue;
                        }

                        if (remoteFaction != null && pawn.Faction != remoteFaction)
                            pawn.SetFactionDirect(remoteFaction);
                        if (MoveMirrorPawn(pawn, map, position)) applied++;
                    }

                    RimjobClientDiagnostics.Verbose(
                        $"Pawn state applied. Owner={owner}; Applied={applied}/{count}; Tile={tile}");
                }
            }
            catch (Exception exception)
            {
                RimjobClientDiagnostics.Error("Could not apply remote pawn state: " + exception);
            }
        }

        private static void ApplyManifest(byte[] payload)
        {
            if (!TryGetSharedMap(out Map map) || payload == null) return;

            try
            {
                using (MemoryStream stream = new MemoryStream(payload, false))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (reader.ReadInt32() != ManifestMagic) return;
                    string owner = reader.ReadString();
                    int tile = reader.ReadInt32();
                    int count = reader.ReadInt32();
                    if (!ValidateRemoteEnvelope(map, owner, tile, count)) return;

                    Faction remoteFaction = PlayerFactionRegistry.GetOrCreate(owner);
                    HashSet<string> seenAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    int created = 0;
                    int existing = 0;
                    for (int index = 0; index < count; index++)
                    {
                        string thingId = reader.ReadString();
                        IntVec3 position = new IntVec3(reader.ReadInt32(), 0, reader.ReadInt32());
                        string serialized = reader.ReadString();
                        string alias = PawnAlias(owner, thingId);
                        seenAliases.Add(alias);

                        Pawn pawn = FindRemotePawn(map, owner, thingId, remoteFaction);
                        if (pawn == null)
                        {
                            pawn = ScribeManager.SerializeFromString<Pawn>(
                                serialized,
                                ScribeManager.SerializableType.Pawn,
                                enforceID: false);
                            if (pawn == null)
                            {
                                RimjobClientDiagnostics.Error(
                                    $"Pawn manifest could not deserialize mirror. Owner={owner}; ThingID={thingId}");
                                continue;
                            }

                            if (remoteFaction != null) pawn.SetFactionDirect(remoteFaction);
                            IntVec3 safePosition = ClampToMap(map, position);
                            RimworldManager.SetDirectThingIntoMap(pawn, map, safePosition);
                            created++;
                        }
                        else
                        {
                            if (remoteFaction != null && pawn.Faction != remoteFaction)
                                pawn.SetFactionDirect(remoteFaction);
                            MoveMirrorPawn(pawn, map, position);
                            existing++;
                        }

                        RemotePawnAliases[alias] = pawn;
                        MissingMirrorWarnings.Remove(owner + "|" + thingId);
                    }

                    foreach (string staleAlias in RemotePawnAliases.Keys
                                 .Where(key => AliasBelongsTo(key, owner) && !seenAliases.Contains(key))
                                 .ToArray())
                    {
                        Pawn stale = RemotePawnAliases[staleAlias];
                        if (stale != null && !stale.Destroyed &&
                            stale.Faction != Faction.OfPlayer)
                            stale.Destroy(DestroyMode.Vanish);
                        RemotePawnAliases.Remove(staleAlias);
                    }

                    RimjobClientDiagnostics.Important(
                        $"Pawn manifest applied. Owner={owner}; Created={created}; Existing={existing}; Total={count}; Tile={tile}");
                }
            }
            catch (Exception exception)
            {
                RimjobClientDiagnostics.Error("Could not apply remote pawn manifest: " + exception);
            }
        }

        private static bool ValidateRemoteEnvelope(Map map, string owner, int tile, int count)
        {
            if (string.IsNullOrWhiteSpace(owner)) return false;
            if (string.Equals(owner, SessionManager.Username, StringComparison.OrdinalIgnoreCase)) return false;
            if (map.Tile.tileId != tile) return false;
            return count >= 0 && count <= 256;
        }

        private static bool TryGetSharedMap(out Map map)
        {
            map = SessionManager.SynchronousMap;
            return SharedTileLiveSync.IsSharedSessionActive && map != null && map.mapPawns != null;
        }

        private static Pawn FindRemotePawn(Map map, string owner, string thingId, Faction remoteFaction)
        {
            if (string.IsNullOrWhiteSpace(thingId) || map?.mapPawns == null) return null;
            string alias = PawnAlias(owner, thingId);
            if (RemotePawnAliases.TryGetValue(alias, out Pawn mapped))
            {
                if (mapped != null && !mapped.Destroyed && mapped.MapHeld == map)
                    return mapped;
                RemotePawnAliases.Remove(alias);
            }

            Pawn fallback = map.mapPawns.AllPawns.FirstOrDefault(pawn =>
                pawn != null &&
                !pawn.Destroyed &&
                string.Equals(pawn.ThingID, thingId, StringComparison.Ordinal) &&
                (remoteFaction == null ? pawn.Faction != Faction.OfPlayer : pawn.Faction == remoteFaction));
            if (fallback != null) RemotePawnAliases[alias] = fallback;
            return fallback;
        }

        private static string PawnAlias(string owner, string thingId) =>
            (owner ?? string.Empty) + "|P|" + (thingId ?? string.Empty);

        private static bool AliasBelongsTo(string alias, string owner) =>
            !string.IsNullOrWhiteSpace(alias) &&
            !string.IsNullOrWhiteSpace(owner) &&
            alias.StartsWith(owner + "|P|", StringComparison.OrdinalIgnoreCase);

        private static bool MoveMirrorPawn(Pawn pawn, Map map, IntVec3 requested)
        {
            if (pawn == null || map == null || pawn.Destroyed) return false;
            IntVec3 position = ClampToMap(map, requested);

            try
            {
                object pather = AccessTools.Field(typeof(Pawn), "pather")?.GetValue(pawn) ??
                                AccessTools.Property(typeof(Pawn), "pather")?.GetValue(pawn, null);
                InvokeNoArgMethod(pather, "StopDead");

                if (pawn.Spawned && pawn.Position == position)
                {
                    InvokeNoArgMethod(pather, "Notify_Teleported");
                    return true;
                }

                if (!pawn.Spawned)
                {
                    RimworldManager.SetDirectThingIntoMap(pawn, map, position);
                    return true;
                }

                object thingGrid = AccessTools.Field(typeof(Map), "thingGrid")?.GetValue(map) ??
                                   AccessTools.Property(typeof(Map), "thingGrid")?.GetValue(map, null);
                InvokeSingleThingMethod(thingGrid, "Deregister", pawn);
                pawn.Position = position;
                InvokeSingleThingMethod(thingGrid, "Register", pawn);

                InvokeNoArgMethod(pather, "Notify_Teleported");
                return true;
            }
            catch (Exception exception)
            {
                RimjobClientDiagnostics.Verbose(
                    $"Remote pawn position correction failed. Pawn={pawn.ThingID}; Target={position}; Error={exception.Message}");
                return false;
            }
        }

        private static IntVec3 ClampToMap(Map map, IntVec3 position)
        {
            int maxX = Math.Max(0, map.Size.x - 1);
            int maxZ = Math.Max(0, map.Size.z - 1);
            return new IntVec3(
                Math.Max(0, Math.Min(maxX, position.x)),
                0,
                Math.Max(0, Math.Min(maxZ, position.z)));
        }

        private static void InvokeSingleThingMethod(object target, string name, Thing thing)
        {
            if (target == null || thing == null) return;
            MethodInfo method = target.GetType().GetMethods(AccessTools.all)
                .FirstOrDefault(candidate =>
                {
                    if (!string.Equals(candidate.Name, name, StringComparison.Ordinal)) return false;
                    ParameterInfo[] parameters = candidate.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(typeof(Thing));
                });
            method?.Invoke(target, new object[] { thing });
        }

        private static void InvokeNoArgMethod(object target, string name)
        {
            if (target == null) return;
            MethodInfo method = target.GetType().GetMethods(AccessTools.all)
                .FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, name, StringComparison.Ordinal) && candidate.GetParameters().Length == 0);
            method?.Invoke(target, null);
        }
    }

    /// <summary>
    /// Root_Play.Update runs while the game is paused too, so initial manifests
    /// are not delayed by the host's current time speed.
    /// </summary>
    [HarmonyPatch]
    public static class SharedPawnStateUpdatePatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("Verse.Root_Play"), "Update");

        [HarmonyPostfix]
        public static void Postfix() => SharedPawnStateSync.Update();
    }

    /// <summary>
    /// Consume Rimjob's private synchronous action values before the stock RWT
    /// switch sees them. The server deliberately forwards Action packets to both
    /// paired clients, which makes this a peer transport without changing RTShared.
    /// </summary>
    [HarmonyPatch]
    public static class SharedPawnStateRoutePatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(
                AccessTools.TypeByName("RTClient.PacketManagers.Synchronous.PM_Synchronous"),
                "RouteToManager");

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(object[] __args)
        {
            PKT_Synchronous packet = __args?.OfType<PKT_Synchronous>().FirstOrDefault();
            return packet == null || !SharedPawnStateSync.TryHandleAction(packet);
        }
    }
}
