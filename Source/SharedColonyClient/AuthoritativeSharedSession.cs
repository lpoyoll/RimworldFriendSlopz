using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RTClient.Managers;
using RTClient.Misc;
using RTNetwork.Packets;
using RTShared.Files;
using RTShared.Misc;
using Verse;

namespace RWTSharedColony
{
    /// <summary>
    /// v0.1.23 authoritative shared-session transport.
    ///
    /// The host map is canonical. Pawns are the one exception: each connected
    /// player owns the authoritative simulation of their own Faction.OfPlayer
    /// pawns and every other client keeps non-controllable mirrors.
    ///
    /// Raw RimWorld ThingIDs are never trusted as cross-machine object IDs. Every
    /// pawn/owned construction gets an owner-scoped network key and remote copies
    /// are deserialised with fresh local ThingIDs. This prevents the two separately
    /// created games from aliasing Human/Thing IDs during the map handover.
    /// </summary>
    public static class AuthoritativeSharedSession
    {
        private const int AcceptMagic = 0x524A3233;        // RJ23
        private const int PawnStateMagic = 0x50533233;     // PS23
        private const int PawnManifestMagic = 0x504D3233;  // PM23
        private const int ConstructionMagic = 0x43533233;  // CS23

        public const int PawnStateActionCode = 9022;
        public const int PawnManifestActionCode = 9023;
        public const int HostConstructionActionCode = 9030;
        public const int GuestConstructionActionCode = 9031;

        private const long PawnStateInterval = TimeSpan.TicksPerMillisecond * 100;
        private const long PawnManifestInterval = TimeSpan.TicksPerSecond * 5;
        private const long ConstructionInterval = TimeSpan.TicksPerMillisecond * 1500;

        private static readonly Dictionary<string, Pawn> RemotePawns =
            new Dictionary<string, Pawn>(StringComparer.Ordinal);

        private static readonly Dictionary<string, Thing> ConstructionByKey =
            new Dictionary<string, Thing>(StringComparer.Ordinal);

        private static readonly Dictionary<Thing, string> ConstructionKeyByThing =
            new Dictionary<Thing, string>();

        private static long _lastPawnState;
        private static long _lastPawnManifest;
        private static long _lastConstruction;
        private static bool _wasReplicationActive;

        public static bool IsReplicationActive
        {
            get
            {
                if (Network.ServerEndpoint == null || SessionManager.SynchronousMap == null) return false;
                return SharedTileLiveSync.SharedGuestActive || SharedTileLiveSync.SharedHostActiveOrPending;
            }
        }

        public static bool IsCanonicalHost =>
            SharedTileLiveSync.SharedHostActiveOrPending && !SharedTileLiveSync.SharedGuestActive;

        public static bool IsV23Accept(byte[] data)
        {
            if (data == null || data.Length < 4) return false;
            try
            {
                return BitConverter.ToInt32(data, 0) == AcceptMagic;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryAutoAccept(PKT_Synchronous request)
        {
            if (request == null || request.CurrentStepMode != PKT_Synchronous.StepMode.Ask) return false;
            if (request.FromTile < 0 || request.ToTile < 0 || request.FromTile != request.ToTile) return false;
            if (string.IsNullOrWhiteSpace(request.Username)) return false;
            if (string.Equals(request.Username, SessionManager.Username, StringComparison.OrdinalIgnoreCase)) return false;

            Map hostMap = Find.AnyPlayerHomeMap;
            if (hostMap == null || hostMap.Tile.tileId != request.ToTile) return false;

            try
            {
                string guestUsername = request.Username;
                SessionManager.SynchronousMap = hostMap;
                SharedColonyState.PendingRemoteUsername = guestUsername;
                SetSharedTileProperty(nameof(SharedTileLiveSync.SharedHostActiveOrPending), true);

                // The canonical map snapshot carries terrain, roofs, weather,
                // things, wildlife and NPCs, but deliberately excludes host-owned
                // pawns. Player pawns travel through the owner-scoped pawn channel
                // so raw Human123-style IDs can never alias the other PC's pawns.
                FL_Map hostFile = MapSaveLoader.MapToString(hostMap);
                hostFile.Pawns.Clear();
                foreach (Pawn pawn in hostMap.mapPawns.AllPawns.Where(pawn =>
                             pawn != null && pawn.Faction != Faction.OfPlayer))
                {
                    string pawnData = ScribeManager.SerializeToString(
                        pawn,
                        ScribeManager.SerializableType.Pawn);
                    if (!string.IsNullOrWhiteSpace(pawnData)) hostFile.Pawns.Add(pawnData);
                }

                byte[] hostMapBytes = Serializer.ConvertObjectToBytes(hostFile, compression: false);
                Faction guestFaction = PlayerFactionRegistry.GetOrCreate(guestUsername);
                List<GuestPawnPlacement> placements = SpawnGuestParty(hostMap, request, guestUsername, guestFaction);

                byte[] payload = BuildAcceptPayload(
                    hostMapBytes,
                    placements,
                    Find.TickManager.TicksSinceSettle,
                    (int)Find.TickManager.CurTimeSpeed);

                PKT_Synchronous accept = new PKT_Synchronous
                {
                    CurrentStepMode = PKT_Synchronous.StepMode.Accept,
                    CurrentType = request.CurrentType,
                    FromTile = request.ToTile,
                    ToTile = request.FromTile,
                    Username = SessionManager.Username,
                    Party = request.Party,
                    Data = payload
                };

                Network.ServerEndpoint.EnqueuePacket(PacketHeader.Synchronous, accept);
                ForceImmediateReplication();
                RimjobClientDiagnostics.Important(
                    $"v0.1.23 canonical handover sent. Guest={guestUsername}; Tile={hostMap.Tile.tileId}; " +
                    $"GuestPawns={placements.Count}; MapBytes={hostMapBytes.Length}");
                Log.Message($"[Rimjob] v0.1.23 host-authoritative handover sent to {guestUsername} on tile {hostMap.Tile.tileId}.");
                return true;
            }
            catch (Exception exception)
            {
                RimjobClientDiagnostics.Error("v0.1.23 canonical handover failed: " + exception);
                Log.Error("[Rimjob] v0.1.23 canonical handover failed: " + exception);
                return true; // Consume same-tile request; stock Visit cannot safely interpret our flow.
            }
        }

        public static bool ApplyAccept(PKT_Synchronous packet)
        {
            if (packet?.Data == null || !IsV23Accept(packet.Data)) return false;

            try
            {
                SharedAcceptPayload payload = ReadAcceptPayload(packet.Data);
                string hostUsername = !string.IsNullOrWhiteSpace(packet.Username)
                    ? packet.Username
                    : SharedTileLiveSync.PendingHostUsername;
                if (string.IsNullOrWhiteSpace(hostUsername))
                    throw new InvalidOperationException("Canonical host username was not present in the accept packet.");

                Map localMap = Find.AnyPlayerHomeMap;
                if (localMap == null)
                    throw new InvalidOperationException("The joining client has no player home map to replace.");

                int expectedTile = SharedTileLiveSync.PendingTile >= 0
                    ? SharedTileLiveSync.PendingTile
                    : localMap.Tile.tileId;
                if (localMap.Tile.tileId != expectedTile)
                    throw new InvalidOperationException($"Joining map is on tile {localMap.Tile.tileId}, expected {expectedTile}.");

                string localUsername = SessionManager.Username ?? string.Empty;
                Dictionary<string, Pawn> localPawns = localMap.mapPawns.AllPawns
                    .Where(pawn => pawn != null && pawn.Faction == Faction.OfPlayer)
                    .ToDictionary(pawn => PawnKey(localUsername, pawn.ThingID), pawn => pawn, StringComparer.Ordinal);

                if (localPawns.Count == 0)
                    throw new InvalidOperationException("No locally-owned pawns were available during the canonical map handover.");

                foreach (Pawn pawn in localPawns.Values)
                {
                    if (pawn.Spawned) pawn.DeSpawn(DestroyMode.Vanish);
                }

                foreach (Thing thing in localMap.listerThings.AllThings.ToArray())
                {
                    try
                    {
                        if (thing != null && !thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
                    }
                    catch (Exception clearException)
                    {
                        RimjobClientDiagnostics.Verbose(
                            $"Map clear skipped {thing?.ThingID ?? "<unknown>"}: {clearException.Message}");
                    }
                }

                FL_Map hostFile = Serializer.ConvertBytesToObject<FL_Map>(payload.MapBytes, compression: false);

                // Fresh local IDs are intentional. Cross-machine ThingID equality
                // is not a valid invariant when two users generated separate maps.
                Map sharedMap = MapSaveLoader.StringToMap(hostFile, enforceIDs: false);
                if (sharedMap == null)
                    throw new InvalidOperationException("Host canonical map snapshot could not be loaded.");

                Faction hostFaction = PlayerFactionRegistry.GetOrCreate(hostUsername);
                if (hostFaction == null)
                    throw new InvalidOperationException($"Could not create remote host faction for '{hostUsername}'.");

                foreach (Thing thing in sharedMap.listerThings.AllThings.ToArray())
                {
                    if (thing?.def?.CanHaveFaction == true && thing.Faction == Faction.OfPlayer)
                        thing.SetFactionDirect(hostFaction);
                }

                Dictionary<string, IntVec3> placementByKey = payload.GuestPlacements
                    .ToDictionary(item => item.NetworkKey, item => item.Position, StringComparer.Ordinal);

                foreach (KeyValuePair<string, Pawn> entry in localPawns)
                {
                    Pawn pawn = entry.Value;
                    if (pawn == null || pawn.Destroyed) continue;
                    pawn.SetFactionDirect(Faction.OfPlayer);
                    IntVec3 position = placementByKey.TryGetValue(entry.Key, out IntVec3 assigned)
                        ? assigned
                        : sharedMap.Center;
                    RimworldManager.SetDirectThingIntoMap(pawn, sharedMap, ClampToMap(sharedMap, position));
                }

                SessionManager.SynchronousMap = sharedMap;
                SharedColonyState.PendingRemoteUsername = hostUsername;
                SetSharedTileProperty(nameof(SharedTileLiveSync.AwaitingAccept), false);
                SetSharedTileProperty(nameof(SharedTileLiveSync.SharedGuestActive), true);

                Find.TickManager.DebugSetTicksGame(payload.HostTicks);
                Find.TickManager.CurTimeSpeed = (TimeSpeed)payload.HostSpeed;

                PKT_Synchronous start = new PKT_Synchronous
                {
                    CurrentStepMode = PKT_Synchronous.StepMode.Start
                };
                Network.ServerEndpoint.EnqueuePacket(PacketHeader.Synchronous, start);
                MainThreadManager.Instance.DoOnSynchronousStartMethods();

                ForceImmediateReplication();
                RimjobClientDiagnostics.Important(
                    $"v0.1.23 canonical handover applied. Host={hostUsername}; Tile={sharedMap.Tile.tileId}; " +
                    $"LocalPawns={localPawns.Count}; HostTicks={payload.HostTicks}. Start sent.");
                Log.Message($"[Rimjob] v0.1.23 canonical host map applied from {hostUsername}; Start sent.");
                return true;
            }
            catch (Exception exception)
            {
                SetSharedTileProperty(nameof(SharedTileLiveSync.AwaitingAccept), false);
                RimjobClientDiagnostics.Error("v0.1.23 canonical accept failed: " + exception);
                Log.Error("[Rimjob] v0.1.23 canonical accept failed: " + exception);
                return true; // Consume custom accept so stock RWT cannot interpret RJ23 bytes as FL_Map.
            }
        }

        public static bool TryHandleAction(PKT_Synchronous packet)
        {
            if (packet == null || packet.CurrentStepMode != PKT_Synchronous.StepMode.Action) return false;

            int actionCode = Convert.ToInt32(packet.CurrentActionType);
            if (actionCode != PawnStateActionCode &&
                actionCode != PawnManifestActionCode &&
                actionCode != HostConstructionActionCode &&
                actionCode != GuestConstructionActionCode)
                return false;

            string sender = packet.Username ?? string.Empty;
            if (string.IsNullOrWhiteSpace(sender))
            {
                RimjobClientDiagnostics.Error($"Custom action {actionCode} arrived without authenticated sender username.");
                return true;
            }

            // Server deliberately echoes Action packets to both peers. Own echoes
            // are consumed but never applied.
            if (string.Equals(sender, SessionManager.Username, StringComparison.OrdinalIgnoreCase))
                return true;

            switch (actionCode)
            {
                case PawnStateActionCode:
                    ApplyPawnState(sender, packet.Data);
                    return true;
                case PawnManifestActionCode:
                    ApplyPawnManifest(sender, packet.Data);
                    return true;
                case HostConstructionActionCode:
                    if (!IsCanonicalHost) ApplyConstructionManifest(sender, packet.Data, canonicalFromHost: true);
                    return true;
                case GuestConstructionActionCode:
                    if (IsCanonicalHost) ApplyConstructionManifest(sender, packet.Data, canonicalFromHost: false);
                    return true;
                default:
                    return true;
            }
        }

        public static void Update()
        {
            try
            {
                if (!IsReplicationActive)
                {
                    _wasReplicationActive = false;
                    _lastPawnState = 0;
                    _lastPawnManifest = 0;
                    _lastConstruction = 0;
                    return;
                }

                if (!_wasReplicationActive)
                {
                    _wasReplicationActive = true;
                    ForceImmediateReplication();
                    RimjobClientDiagnostics.Important(
                        $"v0.1.23 authority replication ACTIVE. Role={(IsCanonicalHost ? "HOST" : "GUEST")}; " +
                        $"User={SessionManager.Username}; Tile={SessionManager.SynchronousMap.Tile.tileId}");
                }

                long now = DateTime.UtcNow.Ticks;
                if (_lastPawnManifest == 0 || now - _lastPawnManifest >= PawnManifestInterval)
                {
                    SendPawnManifest();
                    _lastPawnManifest = now;
                }

                if (_lastPawnState == 0 || now - _lastPawnState >= PawnStateInterval)
                {
                    SendPawnState();
                    _lastPawnState = now;
                }

                if (_lastConstruction == 0 || now - _lastConstruction >= ConstructionInterval)
                {
                    if (IsCanonicalHost) SendCanonicalConstructionManifest();
                    else SendGuestConstructionManifest();
                    _lastConstruction = now;
                }
            }
            catch (Exception exception)
            {
                RimjobClientDiagnostics.Error("v0.1.23 authority update failed: " + exception);
            }
        }

        public static void ForceImmediateReplication()
        {
            _lastPawnState = 0;
            _lastPawnManifest = 0;
            _lastConstruction = 0;
        }

        private static List<GuestPawnPlacement> SpawnGuestParty(
            Map hostMap,
            PKT_Synchronous request,
            string guestUsername,
            Faction guestFaction)
        {
            List<GuestPawnPlacement> placements = new List<GuestPawnPlacement>();
            if (request.Party?.Pawns == null) return placements;

            foreach (string pawnData in request.Party.Pawns)
            {
                if (string.IsNullOrWhiteSpace(pawnData)) continue;

                Pawn pawn = ScribeManager.SerializeFromString<Pawn>(
                    pawnData,
                    ScribeManager.SerializableType.Pawn,
                    enforceID: true);
                if (pawn == null) continue;

                string originalThingId = pawn.ThingID;
                string networkKey = PawnKey(guestUsername, originalThingId);
                GiveFreshLocalThingId(pawn);

                if (guestFaction != null) pawn.SetFactionDirect(guestFaction);
                RimworldManager.PlaceThingIntoMap(pawn, hostMap, hostMap.Center);
                RemotePawns[networkKey] = pawn;
                placements.Add(new GuestPawnPlacement
                {
                    NetworkKey = networkKey,
                    Position = pawn.PositionHeld
                });
            }

            return placements;
        }

        private static void SendPawnState()
        {
            Map map = SessionManager.SynchronousMap;
            if (map?.mapPawns == null || Network.ServerEndpoint == null) return;
            List<Pawn> pawns = map.mapPawns.AllPawns
                .Where(pawn => pawn != null && !pawn.Destroyed && pawn.Spawned && pawn.Faction == Faction.OfPlayer)
                .ToList();
            if (pawns.Count == 0) return;

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(PawnStateMagic);
                writer.Write(map.Tile.tileId);
                writer.Write(pawns.Count);
                foreach (Pawn pawn in pawns)
                {
                    writer.Write(PawnKey(SessionManager.Username, pawn.ThingID));
                    writer.Write(pawn.Position.x);
                    writer.Write(pawn.Position.z);
                    writer.Write(pawn.Drafted);
                }
                writer.Flush();
                EnqueueAction(PawnStateActionCode, stream.ToArray());
            }
        }

        private static void SendPawnManifest()
        {
            Map map = SessionManager.SynchronousMap;
            if (map?.mapPawns == null || Network.ServerEndpoint == null) return;
            List<Pawn> pawns = map.mapPawns.AllPawns
                .Where(pawn => pawn != null && !pawn.Destroyed && pawn.Spawned && pawn.Faction == Faction.OfPlayer)
                .ToList();
            if (pawns.Count == 0) return;

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(PawnManifestMagic);
                writer.Write(map.Tile.tileId);
                writer.Write(pawns.Count);
                foreach (Pawn pawn in pawns)
                {
                    writer.Write(PawnKey(SessionManager.Username, pawn.ThingID));
                    writer.Write(pawn.Position.x);
                    writer.Write(pawn.Position.z);
                    writer.Write(pawn.Drafted);
                    writer.Write(ScribeManager.SerializeToString(
                        pawn,
                        ScribeManager.SerializableType.Pawn) ?? string.Empty);
                }
                writer.Flush();
                EnqueueAction(PawnManifestActionCode, stream.ToArray());
                RimjobClientDiagnostics.Important(
                    $"Pawn manifest sent. Owner={SessionManager.Username}; Count={pawns.Count}; Tile={map.Tile.tileId}");
            }
        }

        private static void ApplyPawnState(string owner, byte[] payload)
        {
            if (!TryGetSharedMap(out Map map) || payload == null) return;
            try
            {
                using (MemoryStream stream = new MemoryStream(payload, false))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (reader.ReadInt32() != PawnStateMagic) return;
                    int tile = reader.ReadInt32();
                    int count = reader.ReadInt32();
                    if (tile != map.Tile.tileId || count < 0 || count > 256) return;

                    Faction remoteFaction = PlayerFactionRegistry.GetOrCreate(owner);
                    int applied = 0;
                    for (int i = 0; i < count; i++)
                    {
                        string key = reader.ReadString();
                        IntVec3 position = new IntVec3(reader.ReadInt32(), 0, reader.ReadInt32());
                        bool drafted = reader.ReadBoolean();
                        if (!KeyBelongsTo(key, owner)) continue;
                        if (!RemotePawns.TryGetValue(key, out Pawn pawn) || pawn == null || pawn.Destroyed) continue;
                        if (remoteFaction != null && pawn.Faction != remoteFaction) pawn.SetFactionDirect(remoteFaction);
                        pawn.drafter?.SetDrafted(drafted);
                        if (MoveMirrorPawn(pawn, map, position)) applied++;
                    }

                    RimjobClientDiagnostics.Verbose(
                        $"Pawn state applied. Owner={owner}; Applied={applied}/{count}; Tile={tile}");
                }
            }
            catch (Exception exception)
            {
                RimjobClientDiagnostics.Error("Could not apply v0.1.23 pawn state: " + exception);
            }
        }

        private static void ApplyPawnManifest(string owner, byte[] payload)
        {
            if (!TryGetSharedMap(out Map map) || payload == null) return;
            try
            {
                using (MemoryStream stream = new MemoryStream(payload, false))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (reader.ReadInt32() != PawnManifestMagic) return;
                    int tile = reader.ReadInt32();
                    int count = reader.ReadInt32();
                    if (tile != map.Tile.tileId || count < 0 || count > 256) return;

                    Faction remoteFaction = PlayerFactionRegistry.GetOrCreate(owner);
                    HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
                    int created = 0;
                    int existing = 0;

                    for (int i = 0; i < count; i++)
                    {
                        string key = reader.ReadString();
                        IntVec3 position = new IntVec3(reader.ReadInt32(), 0, reader.ReadInt32());
                        bool drafted = reader.ReadBoolean();
                        string serialized = reader.ReadString();
                        if (!KeyBelongsTo(key, owner)) continue;
                        seen.Add(key);

                        if (!RemotePawns.TryGetValue(key, out Pawn pawn) || pawn == null || pawn.Destroyed)
                        {
                            pawn = ScribeManager.SerializeFromString<Pawn>(
                                serialized,
                                ScribeManager.SerializableType.Pawn,
                                enforceID: false);
                            if (pawn == null)
                            {
                                RimjobClientDiagnostics.Error($"Pawn manifest could not create {key}.");
                                continue;
                            }
                            if (remoteFaction != null) pawn.SetFactionDirect(remoteFaction);
                            RimworldManager.SetDirectThingIntoMap(pawn, map, ClampToMap(map, position));
                            RemotePawns[key] = pawn;
                            created++;
                        }
                        else
                        {
                            if (remoteFaction != null && pawn.Faction != remoteFaction) pawn.SetFactionDirect(remoteFaction);
                            MoveMirrorPawn(pawn, map, position);
                            existing++;
                        }

                        pawn.drafter?.SetDrafted(drafted);
                    }

                    // A manifest is authoritative for that owner's currently
                    // spawned pawns. Remove stale mirrors only; local pawns are
                    // never present in RemotePawns.
                    foreach (string staleKey in RemotePawns.Keys
                                 .Where(key => KeyBelongsTo(key, owner) && !seen.Contains(key))
                                 .ToArray())
                    {
                        Pawn stale = RemotePawns[staleKey];
                        if (stale != null && !stale.Destroyed) stale.Destroy(DestroyMode.Vanish);
                        RemotePawns.Remove(staleKey);
                    }

                    RimjobClientDiagnostics.Important(
                        $"Pawn manifest applied. Owner={owner}; Created={created}; Existing={existing}; Total={count}; Tile={tile}");
                }
            }
            catch (Exception exception)
            {
                RimjobClientDiagnostics.Error("Could not apply v0.1.23 pawn manifest: " + exception);
            }
        }

        private static void SendCanonicalConstructionManifest()
        {
            Map map = SessionManager.SynchronousMap;
            if (map == null) return;
            List<ConstructionWireEntry> entries = BuildConstructionEntries(map, hostCanonical: true);
            SendConstructionManifest(HostConstructionActionCode, map, entries);
        }

        private static void SendGuestConstructionManifest()
        {
            Map map = SessionManager.SynchronousMap;
            if (map == null) return;
            List<ConstructionWireEntry> entries = BuildConstructionEntries(map, hostCanonical: false);
            SendConstructionManifest(GuestConstructionActionCode, map, entries);
        }

        private static List<ConstructionWireEntry> BuildConstructionEntries(Map map, bool hostCanonical)
        {
            List<ConstructionWireEntry> result = new List<ConstructionWireEntry>();
            string localUsername = SessionManager.Username ?? string.Empty;

            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (!IsOwnedConstructionThing(thing)) continue;

                string key;
                string owner;
                if (hostCanonical)
                {
                    if (ConstructionKeyByThing.TryGetValue(thing, out string mappedKey))
                    {
                        key = mappedKey;
                        owner = OwnerFromKey(mappedKey);
                    }
                    else if (thing.Faction == Faction.OfPlayer)
                    {
                        owner = localUsername;
                        key = ConstructionKey(localUsername, thing.ThingID);
                        RegisterConstruction(key, thing);
                    }
                    else
                    {
                        // Remote-owned host objects must have arrived through a
                        // guest proposal so their owner-scoped key is known.
                        continue;
                    }
                }
                else
                {
                    if (thing.Faction != Faction.OfPlayer) continue;
                    if (ConstructionKeyByThing.TryGetValue(thing, out string mappedKey) &&
                        KeyBelongsTo(mappedKey, localUsername))
                        key = mappedKey;
                    else
                        key = ConstructionKey(localUsername, thing.ThingID);
                    owner = localUsername;
                    RegisterConstruction(key, thing);
                }

                result.Add(CreateConstructionWireEntry(key, owner, thing));
            }

            return result;
        }

        private static void SendConstructionManifest(int actionCode, Map map, List<ConstructionWireEntry> entries)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(ConstructionMagic);
                writer.Write(map.Tile.tileId);
                writer.Write(entries.Count);
                foreach (ConstructionWireEntry entry in entries)
                {
                    writer.Write(entry.NetworkKey);
                    writer.Write(entry.Owner);
                    writer.Write(entry.DefName);
                    writer.Write(entry.TypeName);
                    writer.Write(entry.X);
                    writer.Write(entry.Z);
                    writer.Write(entry.HitPoints);
                    writer.Write(entry.StackCount);
                    writer.Write(entry.WorkDone);
                    writer.Write(entry.Serialized ?? string.Empty);
                }
                writer.Flush();
                EnqueueAction(actionCode, stream.ToArray());
                RimjobClientDiagnostics.Verbose(
                    $"Construction manifest sent. Mode={(actionCode == HostConstructionActionCode ? "HOST-CANONICAL" : "GUEST-PROPOSAL")}; Count={entries.Count}");
            }
        }

        private static void ApplyConstructionManifest(string sender, byte[] payload, bool canonicalFromHost)
        {
            if (!TryGetSharedMap(out Map map) || payload == null) return;
            try
            {
                using (MemoryStream stream = new MemoryStream(payload, false))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (reader.ReadInt32() != ConstructionMagic) return;
                    int tile = reader.ReadInt32();
                    int count = reader.ReadInt32();
                    if (tile != map.Tile.tileId || count < 0 || count > 4096) return;

                    HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
                    for (int i = 0; i < count; i++)
                    {
                        ConstructionWireEntry entry = new ConstructionWireEntry
                        {
                            NetworkKey = reader.ReadString(),
                            Owner = reader.ReadString(),
                            DefName = reader.ReadString(),
                            TypeName = reader.ReadString(),
                            X = reader.ReadInt32(),
                            Z = reader.ReadInt32(),
                            HitPoints = reader.ReadInt32(),
                            StackCount = reader.ReadInt32(),
                            WorkDone = reader.ReadSingle(),
                            Serialized = reader.ReadString()
                        };

                        // The server stamps packet.Username from the authenticated
                        // connection. Guest proposals may only describe that guest's
                        // own construction; canonical host packets may describe host
                        // objects plus guest objects that the host already accepted.
                        if (!canonicalFromHost &&
                            (!string.Equals(entry.Owner, sender, StringComparison.OrdinalIgnoreCase) ||
                             !KeyBelongsTo(entry.NetworkKey, sender)))
                            continue;

                        seen.Add(entry.NetworkKey);
                        UpsertConstruction(map, entry);
                    }

                    if (!canonicalFromHost)
                    {
                        // Host removes only that guest's previously accepted owned
                        // structures which disappeared from the guest proposal.
                        foreach (string staleKey in ConstructionByKey.Keys
                                     .Where(key => KeyBelongsTo(key, sender) && !seen.Contains(key))
                                     .ToArray())
                            RemoveConstruction(staleKey);
                    }
                    else
                    {
                        // Guest treats the complete host list as canonical for all
                        // shared owned structures.
                        foreach (string staleKey in ConstructionByKey.Keys
                                     .Where(key => !seen.Contains(key))
                                     .ToArray())
                            RemoveConstruction(staleKey);
                    }

                    RimjobClientDiagnostics.Verbose(
                        $"Construction manifest applied. Sender={sender}; Canonical={canonicalFromHost}; Count={count}");
                }
            }
            catch (Exception exception)
            {
                RimjobClientDiagnostics.Error("Could not apply construction manifest: " + exception);
            }
        }

        private static void UpsertConstruction(Map map, ConstructionWireEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.NetworkKey) || string.IsNullOrWhiteSpace(entry.Owner)) return;
            Thing thing = null;
            if (ConstructionByKey.TryGetValue(entry.NetworkKey, out Thing mapped) && mapped != null && !mapped.Destroyed)
            {
                thing = mapped;
            }
            else
            {
                thing = FindMatchingConstruction(map, entry);
                if (thing != null) RegisterConstruction(entry.NetworkKey, thing);
            }

            if (thing == null || thing.Destroyed ||
                !string.Equals(thing.def?.defName, entry.DefName, StringComparison.Ordinal) ||
                !string.Equals(thing.GetType().FullName ?? thing.GetType().Name, entry.TypeName, StringComparison.Ordinal))
            {
                if (thing != null && !thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
                thing = ScribeManager.SerializeFromString<Thing>(
                    entry.Serialized,
                    ScribeManager.SerializableType.Thing,
                    enforceID: false);
                if (thing == null) return;

                ApplyConstructionFaction(thing, entry.Owner);
                RimworldManager.SetDirectThingIntoMap(
                    thing,
                    map,
                    ClampToMap(map, new IntVec3(entry.X, 0, entry.Z)));
                RegisterConstruction(entry.NetworkKey, thing);
            }
            else
            {
                ApplyConstructionFaction(thing, entry.Owner);
            }

            try
            {
                if (entry.HitPoints > 0) thing.HitPoints = Math.Min(entry.HitPoints, thing.MaxHitPoints);
                if (entry.StackCount > 0) thing.stackCount = entry.StackCount;
                SetWorkDone(thing, entry.WorkDone);
            }
            catch (Exception exception)
            {
                RimjobClientDiagnostics.Verbose($"Construction state update skipped for {entry.NetworkKey}: {exception.Message}");
            }
        }

        private static Thing FindMatchingConstruction(Map map, ConstructionWireEntry entry)
        {
            IntVec3 position = ClampToMap(map, new IntVec3(entry.X, 0, entry.Z));
            Faction expectedFaction = string.Equals(entry.Owner, SessionManager.Username, StringComparison.OrdinalIgnoreCase)
                ? Faction.OfPlayer
                : PlayerFactionRegistry.GetOrCreate(entry.Owner);

            return map.listerThings.AllThings.FirstOrDefault(thing =>
                thing != null && !thing.Destroyed && IsOwnedConstructionThing(thing) &&
                thing.Position == position &&
                string.Equals(thing.def?.defName, entry.DefName, StringComparison.Ordinal) &&
                (expectedFaction == null || thing.Faction == expectedFaction));
        }

        private static void ApplyConstructionFaction(Thing thing, string owner)
        {
            if (thing?.def?.CanHaveFaction != true) return;
            Faction target = string.Equals(owner, SessionManager.Username, StringComparison.OrdinalIgnoreCase)
                ? Faction.OfPlayer
                : PlayerFactionRegistry.GetOrCreate(owner);
            if (target != null && thing.Faction != target) thing.SetFactionDirect(target);
        }

        private static void RegisterConstruction(string key, Thing thing)
        {
            if (string.IsNullOrWhiteSpace(key) || thing == null) return;
            ConstructionByKey[key] = thing;
            ConstructionKeyByThing[thing] = key;
        }

        private static void RemoveConstruction(string key)
        {
            if (!ConstructionByKey.TryGetValue(key, out Thing thing)) return;
            ConstructionByKey.Remove(key);
            if (thing != null)
            {
                ConstructionKeyByThing.Remove(thing);
                if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
            }
        }

        private static bool IsOwnedConstructionThing(Thing thing)
        {
            if (thing == null || thing.Destroyed || thing is Pawn) return false;
            string typeName = thing.GetType().Name;
            bool constructionType = typeName.IndexOf("Blueprint", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    typeName.IndexOf("Frame", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    typeName.IndexOf("Building", StringComparison.OrdinalIgnoreCase) >= 0;
            bool playerOwned = thing.Faction == Faction.OfPlayer || PlayerFactionRegistry.IsRemoteFaction(thing.Faction);
            return constructionType || playerOwned;
        }

        private static ConstructionWireEntry CreateConstructionWireEntry(string key, string owner, Thing thing)
        {
            return new ConstructionWireEntry
            {
                NetworkKey = key,
                Owner = owner,
                DefName = thing.def?.defName ?? string.Empty,
                TypeName = thing.GetType().FullName ?? thing.GetType().Name,
                X = thing.Position.x,
                Z = thing.Position.z,
                HitPoints = SafeHitPoints(thing),
                StackCount = Math.Max(0, thing.stackCount),
                WorkDone = GetWorkDone(thing),
                Serialized = ScribeManager.SerializeToString(thing, ScribeManager.SerializableType.Thing)
            };
        }

        private static int SafeHitPoints(Thing thing)
        {
            try { return thing.HitPoints; }
            catch { return 0; }
        }

        private static float GetWorkDone(Thing thing)
        {
            try
            {
                FieldInfo field = AccessTools.Field(thing.GetType(), "workDone");
                if (field != null && field.FieldType == typeof(float)) return (float)field.GetValue(thing);
            }
            catch { }
            return -1f;
        }

        private static void SetWorkDone(Thing thing, float value)
        {
            if (value < 0f) return;
            try
            {
                FieldInfo field = AccessTools.Field(thing.GetType(), "workDone");
                if (field != null && field.FieldType == typeof(float)) field.SetValue(thing, value);
            }
            catch { }
        }

        private static void EnqueueAction(int actionCode, byte[] payload)
        {
            if (Network.ServerEndpoint == null) return;
            PKT_Synchronous packet = new PKT_Synchronous
            {
                CurrentStepMode = PKT_Synchronous.StepMode.Action,
                CurrentActionType = (PKT_Synchronous.ActionType)actionCode,
                Data = payload
            };
            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Synchronous, packet);
        }

        private static bool TryGetSharedMap(out Map map)
        {
            map = SessionManager.SynchronousMap;
            return IsReplicationActive && map != null && map.mapPawns != null;
        }

        private static bool MoveMirrorPawn(Pawn pawn, Map map, IntVec3 requested)
        {
            if (pawn == null || map == null || pawn.Destroyed) return false;
            IntVec3 position = ClampToMap(map, requested);
            if (pawn.Spawned && pawn.Position == position) return true;

            try
            {
                if (!pawn.Spawned)
                {
                    RimworldManager.SetDirectThingIntoMap(pawn, map, position);
                    return true;
                }

                object thingGrid = AccessTools.Field(typeof(Map), "thingGrid")?.GetValue(map) ??
                                   AccessTools.Property(typeof(Map), "thingGrid")?.GetValue(map, null);
                InvokeThingGrid(thingGrid, "Deregister", pawn);
                pawn.Position = position;
                InvokeThingGrid(thingGrid, "Register", pawn);

                object pather = AccessTools.Field(typeof(Pawn), "pather")?.GetValue(pawn) ??
                                AccessTools.Property(typeof(Pawn), "pather")?.GetValue(pawn, null);
                MethodInfo teleported = pather?.GetType().GetMethods(AccessTools.all)
                    .FirstOrDefault(method => method.Name == "Notify_Teleported" && method.GetParameters().Length == 0);
                teleported?.Invoke(pather, null);
                return true;
            }
            catch (Exception exception)
            {
                RimjobClientDiagnostics.Verbose(
                    $"Remote pawn correction failed for {pawn.ThingID} -> {position}: {exception.Message}");
                return false;
            }
        }

        private static void InvokeThingGrid(object target, string name, Thing thing)
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

        private static void GiveFreshLocalThingId(Thing thing)
        {
            if (thing == null) return;
            try
            {
                FieldInfo idField = AccessTools.Field(typeof(Thing), "thingIDNumber");
                if (idField != null) idField.SetValue(thing, Find.UniqueIDsManager.GetNextThingID());
            }
            catch (Exception exception)
            {
                RimjobClientDiagnostics.Error("Could not assign fresh local ThingID: " + exception.Message);
            }
        }

        private static IntVec3 ClampToMap(Map map, IntVec3 position)
        {
            return new IntVec3(
                Math.Max(0, Math.Min(map.Size.x - 1, position.x)),
                0,
                Math.Max(0, Math.Min(map.Size.z - 1, position.z)));
        }

        private static string PawnKey(string owner, string thingId) =>
            (owner ?? string.Empty) + "|P|" + (thingId ?? string.Empty);

        private static string ConstructionKey(string owner, string thingId) =>
            (owner ?? string.Empty) + "|C|" + (thingId ?? string.Empty);

        private static string OwnerFromKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            int separator = key.IndexOf('|');
            return separator <= 0 ? string.Empty : key.Substring(0, separator);
        }

        private static bool KeyBelongsTo(string key, string owner) =>
            !string.IsNullOrWhiteSpace(key) &&
            !string.IsNullOrWhiteSpace(owner) &&
            key.StartsWith(owner + "|", StringComparison.OrdinalIgnoreCase);

        private static void SetSharedTileProperty(string propertyName, object value)
        {
            PropertyInfo property = AccessTools.Property(typeof(SharedTileLiveSync), propertyName);
            MethodInfo setter = property?.GetSetMethod(true);
            setter?.Invoke(null, new[] { value });
        }

        private static byte[] BuildAcceptPayload(
            byte[] mapBytes,
            List<GuestPawnPlacement> placements,
            int hostTicks,
            int hostSpeed)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(AcceptMagic);
                writer.Write(hostTicks);
                writer.Write(hostSpeed);
                writer.Write(mapBytes?.Length ?? 0);
                if (mapBytes != null) writer.Write(mapBytes);
                writer.Write(placements?.Count ?? 0);
                if (placements != null)
                {
                    foreach (GuestPawnPlacement item in placements)
                    {
                        writer.Write(item.NetworkKey ?? string.Empty);
                        writer.Write(item.Position.x);
                        writer.Write(item.Position.y);
                        writer.Write(item.Position.z);
                    }
                }
                writer.Flush();
                return stream.ToArray();
            }
        }

        private static SharedAcceptPayload ReadAcceptPayload(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes, false))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                if (reader.ReadInt32() != AcceptMagic)
                    throw new InvalidDataException("Not a Rimjob v0.1.23 accept payload.");
                int hostTicks = reader.ReadInt32();
                int hostSpeed = reader.ReadInt32();
                int mapLength = reader.ReadInt32();
                if (mapLength <= 0 || mapLength > stream.Length - stream.Position)
                    throw new InvalidDataException($"Invalid canonical map length {mapLength}.");
                byte[] mapBytes = reader.ReadBytes(mapLength);
                int count = reader.ReadInt32();
                if (count < 0 || count > 256)
                    throw new InvalidDataException($"Invalid guest pawn placement count {count}.");

                List<GuestPawnPlacement> placements = new List<GuestPawnPlacement>(count);
                for (int i = 0; i < count; i++)
                {
                    placements.Add(new GuestPawnPlacement
                    {
                        NetworkKey = reader.ReadString(),
                        Position = new IntVec3(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32())
                    });
                }

                return new SharedAcceptPayload
                {
                    MapBytes = mapBytes,
                    GuestPlacements = placements,
                    HostTicks = hostTicks,
                    HostSpeed = hostSpeed
                };
            }
        }

        private sealed class GuestPawnPlacement
        {
            public string NetworkKey { get; set; }
            public IntVec3 Position { get; set; }
        }

        private sealed class SharedAcceptPayload
        {
            public byte[] MapBytes { get; set; }
            public List<GuestPawnPlacement> GuestPlacements { get; set; }
            public int HostTicks { get; set; }
            public int HostSpeed { get; set; }
        }

        private sealed class ConstructionWireEntry
        {
            public string NetworkKey { get; set; }
            public string Owner { get; set; }
            public string DefName { get; set; }
            public string TypeName { get; set; }
            public int X { get; set; }
            public int Z { get; set; }
            public int HitPoints { get; set; }
            public int StackCount { get; set; }
            public float WorkDone { get; set; }
            public string Serialized { get; set; }
        }
    }

    /// <summary>
    /// Intercept at PM_Synchronous.Receive itself. v0.1.22 patched private helper
    /// methods, so a failed/short-circuited OnAccept left the server paired but the
    /// guest never sent Start. This is the single authoritative ingress point.
    /// </summary>
    [HarmonyPatch]
    public static class AuthoritativeSynchronousReceivePatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(
                AccessTools.TypeByName("RTClient.PacketManagers.Synchronous.PM_Synchronous"),
                "Receive");

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(byte[] bytes)
        {
            try
            {
                PKT_Synchronous packet = Serializer.ConvertBytesToObject<PKT_Synchronous>(bytes);
                if (packet == null) return true;

                if (packet.CurrentStepMode == PKT_Synchronous.StepMode.Ask &&
                    AuthoritativeSharedSession.TryAutoAccept(packet))
                    return false;

                if (packet.CurrentStepMode == PKT_Synchronous.StepMode.Accept &&
                    AuthoritativeSharedSession.IsV23Accept(packet.Data))
                {
                    AuthoritativeSharedSession.ApplyAccept(packet);
                    return false;
                }

                if (packet.CurrentStepMode == PKT_Synchronous.StepMode.Action &&
                    AuthoritativeSharedSession.TryHandleAction(packet))
                    return false;
            }
            catch (Exception exception)
            {
                RimjobClientDiagnostics.Error("Top-level synchronous receive interception failed: " + exception);
            }

            return true;
        }
    }

    /// <summary>
    /// v0.1.22's Root_Play hook already calls SharedPawnStateSync.Update. Replace
    /// only the body so there is one replication loop and no duplicate 9022/9023
    /// packets from the obsolete raw-ThingID implementation.
    /// </summary>
    [HarmonyPatch(typeof(SharedPawnStateSync), nameof(SharedPawnStateSync.Update))]
    public static class AuthoritativePawnUpdateOverridePatch
    {
        [HarmonyPriority(Priority.First)]
        public static bool Prefix()
        {
            AuthoritativeSharedSession.Update();
            return false;
        }
    }
}
