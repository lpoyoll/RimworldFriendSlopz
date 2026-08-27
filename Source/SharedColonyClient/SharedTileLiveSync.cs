using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RTClient.Hooks.Synchronous;
using RTClient.Managers;
using RTClient.Misc;
using RTClient.PacketManagers.Synchronous;
using RTNetwork.Components;
using RTNetwork.Packets;
using RTShared.Files;
using RTShared.Files.Synchronous;
using RTShared.Misc;
using Verse;
using Verse.AI;

namespace RWTSharedColony
{
    /// <summary>
    /// Turns the shared starting-tile exception into an actual shared live map.
    ///
    /// RimWorld Together's stock synchronous mode is a visit/raid flow. Merely
    /// allowing two new colonies to choose the same world tile still lets each
    /// client generate its own local Map. Rimjob instead captures the already
    /// occupied RTSettlement selected by the joining player, then immediately
    /// opens a synchronous session against that player after InitNewGame.
    ///
    /// The existing RWT Synchronous packet remains the transport. The host sends
    /// its live map snapshot, the joining player's pawns are spawned into that
    /// host map, and the guest replaces its freshly generated map contents with
    /// the host snapshot. The owner keeps Faction.OfPlayer locally; the other
    /// player's pawns/things use a generated remote-player faction.
    /// </summary>
    public static class SharedTileLiveSync
    {
        private const int PayloadMagic = 0x524A3133; // "RJ13"
        private const int ClockResyncIntervalTicks = 60;

        private static readonly List<Pawn> LocalJoiningPawns = new List<Pawn>();

        public static int PendingTile { get; private set; } = -1;

        public static string PendingHostUsername { get; private set; }

        public static bool AwaitingAccept { get; private set; }

        public static bool SharedGuestActive { get; private set; }

        public static bool SharedHostActiveOrPending { get; private set; }

        // SharedHostActiveOrPending is set only after this client has accepted a
        // validated same-tile request and installed the synchronous map.  Do not
        // make private pawn receive/send depend on RTClient's host flag as well:
        // our auto-accept prefix deliberately replaces the stock OnAsk path that
        // normally initialises that flag.  v0.1.25 did so and left the host deaf
        // to guest manifests while the guest displayed a frozen host snapshot.
        public static bool IsSharedSessionActive =>
            SharedGuestActive || SharedHostActiveOrPending;

        public static void CaptureSelectedTarget()
        {
            try
            {
                WorldObject selected = Find.WorldSelector?.FirstSelectedObject;
                if (!SharedTileSelectionUtility.IsRemotePlayerSettlement(selected)) return;
                if (!selected.Tile.Valid) return;

                string username = GetSettlementUsername(selected);
                if (string.IsNullOrWhiteSpace(username))
                {
                    Log.Warning("[Rimjob] Shared tile selected but the remote settlement owner could not be resolved.");
                    return;
                }

                PendingTile = selected.Tile.tileId;
                PendingHostUsername = username;
                SharedColonyState.PendingRemoteUsername = username;
                Log.Message($"[Rimjob] Shared live-map target captured: {username} on tile {PendingTile}.");
            }
            catch (Exception exception)
            {
                Log.Warning($"[Rimjob] Could not capture shared live-map target: {exception}");
            }
        }

        public static void BeginPendingJoin(Game game)
        {
            try
            {
                if (PendingTile < 0 || string.IsNullOrWhiteSpace(PendingHostUsername)) return;
                if (game?.CurrentMap == null || game.CurrentMap.Tile.tileId != PendingTile) return;
                if (Network.ServerEndpoint == null) return;

                LocalJoiningPawns.Clear();
                LocalJoiningPawns.AddRange(game.CurrentMap.mapPawns.AllPawns
                    .Where(pawn => pawn != null && pawn.Faction == Faction.OfPlayer));

                if (LocalJoiningPawns.Count == 0)
                {
                    Log.Error("[Rimjob] Shared live-map join aborted: no local player pawns were found after map generation.");
                    return;
                }

                SyncronousParty party = new SyncronousParty
                {
                    Pawns = LocalJoiningPawns
                        .Select(pawn => ScribeManager.SerializeToString(
                            pawn,
                            ScribeManager.SerializableType.Pawn,
                            -1,
                            pawn.ThingID))
                        .ToList()
                };

                // The Settlement packet from RTClient's own InitNewGame postfix
                // is queued before this packet. TCP ordering therefore lets the
                // server register the joining settlement before resolving this
                // explicit same-tile synchronous target.
                PKT_Synchronous request = new PKT_Synchronous
                {
                    CurrentStepMode = PKT_Synchronous.StepMode.Ask,
                    CurrentType = PKT_Synchronous.Type.Visit,
                    ToTile = PendingTile,
                    Username = PendingHostUsername,
                    Party = party
                };

                AwaitingAccept = true;
                Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                Network.ServerEndpoint.EnqueuePacket(PacketHeader.Synchronous, request);
                Log.Message($"[Rimjob] Requesting host-authoritative shared map from {PendingHostUsername} on tile {PendingTile}.");
            }
            catch (Exception exception)
            {
                AwaitingAccept = false;
                Log.Error($"[Rimjob] Shared live-map join request failed: {exception}");
            }
        }

        public static bool TryAutoAccept(PKT_Synchronous request)
        {
            try
            {
                if (!IsSameTileJoinRequest(request)) return false;

                Map hostMap = Find.AnyPlayerHomeMap;
                if (hostMap == null || hostMap.Tile.tileId != request.ToTile) return false;

                string guestUsername = request.Username;
                if (string.IsNullOrWhiteSpace(guestUsername)) return false;

                SharedColonyState.PendingRemoteUsername = guestUsername;
                SessionManager.SynchronousMap = hostMap;
                // AutoAcceptSharedTileLiveJoinPatch skips RTClient's stock OnAsk
                // handler.  That handler normally marks this side as the
                // synchronous host.  Set it explicitly before private BUILD and
                // pawn packets can arrive, otherwise only the guest considers
                // the live session active.
                SessionManager.IsSynchronousHost = true;
                SharedHostActiveOrPending = true;

                if (!SessionManager.IsSynchronousHost || SessionManager.SynchronousMap != hostMap)
                    throw new InvalidOperationException("The local client could not enter synchronous-host mode.");

                // Snapshot first. The guest's local pawns are added to both copies
                // afterwards at the exact positions selected on the host.
                byte[] hostMapBytes = Serializer.ConvertObjectToBytes(
                    MapSaveLoader.MapToString(hostMap),
                    compression: false);

                Faction guestFaction = PlayerFactionRegistry.GetOrCreate(guestUsername);
                List<IntVec3> guestPositions = SpawnGuestParty(hostMap, request.Party, guestFaction);
                byte[] payload = BuildPayload(
                    hostMapBytes,
                    guestPositions,
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
                Log.Message($"[Rimjob] Auto-accepted shared live-map join from {guestUsername}; host transport active={IsSharedSessionActive}; synchronous host={SessionManager.IsSynchronousHost}; host map is authoritative.");
                return true;
            }
            catch (Exception exception)
            {
                Log.Error($"[Rimjob] Failed to auto-accept shared live-map join: {exception}");
                return false;
            }
        }

        public static bool TryApplyAccept(PKT_Synchronous packet)
        {
            if (!AwaitingAccept || packet == null || packet.Data == null) return false;

            try
            {
                if (!TryReadPayload(packet.Data, out SharedMapPayload payload)) return false;

                Map localMap = Find.AnyPlayerHomeMap;
                if (localMap == null || localMap.Tile.tileId != PendingTile)
                    throw new InvalidOperationException("The joining player's local home map is missing or on the wrong tile.");

                // Remove the independently generated instance while preserving the
                // joining player's pawn objects and inventories. This keeps their
                // IDs stable for the synchronous job/draft protocol.
                foreach (Pawn pawn in LocalJoiningPawns)
                {
                    if (pawn != null && pawn.Spawned)
                        pawn.DeSpawn(DestroyMode.Vanish);
                }

                foreach (Thing thing in localMap.listerThings.AllThings.ToArray())
                {
                    try
                    {
                        if (thing != null && !thing.Destroyed)
                            thing.Destroy(DestroyMode.Vanish);
                    }
                    catch (Exception clearException)
                    {
                        Log.Warning($"[Rimjob] Could not clear local generated thing {thing}: {clearException.Message}");
                    }
                }

                FL_Map hostFile = Serializer.ConvertBytesToObject<FL_Map>(payload.MapBytes, compression: false);
                Map sharedMap = MapSaveLoader.StringToMap(hostFile, enforceIDs: true);
                if (sharedMap == null)
                    throw new InvalidOperationException("Host map snapshot could not be loaded.");

                Faction hostFaction = PlayerFactionRegistry.GetOrCreate(PendingHostUsername);
                if (hostFaction == null)
                    throw new InvalidOperationException($"Could not create remote faction for host '{PendingHostUsername}'.");

                // At snapshot time every host-owned entity is Faction.OfPlayer.
                // Convert those to the host's remote faction before our own pawns
                // are put back into the map.
                foreach (Thing thing in sharedMap.listerThings.AllThings.ToArray())
                {
                    if (thing?.def?.CanHaveFaction == true && thing.Faction == Faction.OfPlayer)
                        thing.SetFactionDirect(hostFaction);
                }

                for (int index = 0; index < LocalJoiningPawns.Count; index++)
                {
                    Pawn pawn = LocalJoiningPawns[index];
                    if (pawn == null || pawn.Destroyed) continue;

                    pawn.SetFactionDirect(Faction.OfPlayer);
                    IntVec3 position = index < payload.PawnPositions.Count
                        ? payload.PawnPositions[index]
                        : sharedMap.Center;
                    RimworldManager.SetDirectThingIntoMap(pawn, sharedMap, position);
                }

                SessionManager.SynchronousMap = sharedMap;

                // Align the guest before synchronous patches are enabled. From
                // this point on the host periodically republishes its clock and
                // guests cannot set time speed themselves.
                Find.TickManager.DebugSetTicksGame(payload.HostTicks);
                Find.TickManager.CurTimeSpeed = (TimeSpeed)payload.HostSpeed;

                PKT_Synchronous start = new PKT_Synchronous
                {
                    CurrentStepMode = PKT_Synchronous.StepMode.Start
                };
                Network.ServerEndpoint.EnqueuePacket(PacketHeader.Synchronous, start);
                MainThreadManager.Instance.DoOnSynchronousStartMethods();

                AwaitingAccept = false;
                SharedGuestActive = true;
                Log.Message($"[Rimjob] Shared live map loaded from {PendingHostUsername}. Host tick {payload.HostTicks} is authoritative.");
                return true;
            }
            catch (Exception exception)
            {
                AwaitingAccept = false;
                Log.Error($"[Rimjob] Failed to replace local map with host-authoritative shared map: {exception}");
                return false;
            }
        }

        public static void SendHostClock()
        {
            if (!SharedHostActiveOrPending || !SessionManager.IsSynchronousHost || Network.ServerEndpoint == null)
                return;

            PlayerGameSpeed clock = new PlayerGameSpeed
            {
                CurrentGameSpeed = (int)Find.TickManager.CurTimeSpeed,
                TimeTicks = Find.TickManager.TicksSinceSettle
            };

            PKT_Synchronous action = new PKT_Synchronous
            {
                CurrentStepMode = PKT_Synchronous.StepMode.Action,
                CurrentActionType = PKT_Synchronous.ActionType.SPlayerGameSpeed,
                Data = Serializer.ConvertObjectToBytes(clock, compression: false)
            };
            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Synchronous, action);
        }

        public static void MaybeResyncHostClock()
        {
            if (!SharedHostActiveOrPending || !SessionManager.IsSynchronousHost) return;
            int ticks = Find.TickManager.TicksSinceSettle;
            if (ticks >= 0 && ticks % ClockResyncIntervalTicks == 0)
                SendHostClock();
        }

        private static bool IsSameTileJoinRequest(PKT_Synchronous request)
        {
            if (request == null || request.CurrentStepMode != PKT_Synchronous.StepMode.Ask) return false;
            if (request.FromTile < 0 || request.ToTile < 0 || request.FromTile != request.ToTile) return false;
            if (string.IsNullOrWhiteSpace(request.Username)) return false;
            if (string.Equals(request.Username, SessionManager.Username, StringComparison.OrdinalIgnoreCase)) return false;

            // PM_Synchronous replaces Username and FromTile with values resolved
            // from the authenticated requester before this packet reaches the
            // host. That server-verified envelope is the authority for a shared
            // tile join. Do not also require the guest's RTSettlement world marker:
            // settlement broadcasts and synchronous packets travel independently,
            // so the marker can legitimately be missing or late on the host. In
            // v0.1.14 that timing window sent the request into RWT's stock visit
            // handler and left both players on separate local maps.
            return true;
        }

        private static List<IntVec3> SpawnGuestParty(Map hostMap, SyncronousParty party, Faction guestFaction)
        {
            List<IntVec3> positions = new List<IntVec3>();
            if (party?.Pawns == null) return positions;

            foreach (string pawnData in party.Pawns)
            {
                Pawn pawn = ScribeManager.SerializeFromString<Pawn>(
                    pawnData,
                    ScribeManager.SerializableType.Pawn,
                    enforceID: true);
                if (pawn == null) continue;

                if (guestFaction != null) pawn.SetFactionDirect(guestFaction);
                RimworldManager.PlaceThingIntoMap(pawn, hostMap, hostMap.Center);
                positions.Add(pawn.PositionHeld);
            }

            return positions;
        }

        private static byte[] BuildPayload(byte[] mapBytes, List<IntVec3> positions, int hostTicks, int hostSpeed)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(PayloadMagic);
                writer.Write(hostTicks);
                writer.Write(hostSpeed);
                writer.Write(mapBytes?.Length ?? 0);
                if (mapBytes != null) writer.Write(mapBytes);
                writer.Write(positions?.Count ?? 0);
                if (positions != null)
                {
                    foreach (IntVec3 position in positions)
                    {
                        writer.Write(position.x);
                        writer.Write(position.y);
                        writer.Write(position.z);
                    }
                }
                writer.Flush();
                return stream.ToArray();
            }
        }

        private static bool TryReadPayload(byte[] bytes, out SharedMapPayload payload)
        {
            payload = null;
            try
            {
                using (MemoryStream stream = new MemoryStream(bytes, false))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (reader.ReadInt32() != PayloadMagic) return false;
                    int hostTicks = reader.ReadInt32();
                    int hostSpeed = reader.ReadInt32();
                    int mapLength = reader.ReadInt32();
                    if (mapLength <= 0 || mapLength > stream.Length - stream.Position) return false;
                    byte[] mapBytes = reader.ReadBytes(mapLength);
                    int count = reader.ReadInt32();
                    if (count < 0 || count > 256) return false;

                    List<IntVec3> positions = new List<IntVec3>(count);
                    for (int index = 0; index < count; index++)
                        positions.Add(new IntVec3(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()));

                    payload = new SharedMapPayload
                    {
                        MapBytes = mapBytes,
                        PawnPositions = positions,
                        HostTicks = hostTicks,
                        HostSpeed = hostSpeed
                    };
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static string GetSettlementUsername(WorldObject settlement)
        {
            string name = AccessTools.Property(settlement.GetType(), "Name")?.GetValue(settlement, null) as string;
            if (string.IsNullOrWhiteSpace(name)) name = settlement.Label;
            if (string.IsNullOrWhiteSpace(name)) return null;

            const string suffix = "'s settlement";
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return name.Substring(0, name.Length - suffix.Length);
            return name;
        }

        private sealed class SharedMapPayload
        {
            public byte[] MapBytes { get; set; }

            public List<IntVec3> PawnPositions { get; set; }

            public int HostTicks { get; set; }

            public int HostSpeed { get; set; }
        }
    }

    [HarmonyPatch(typeof(Page_SelectStartingSite), "DoNext")]
    public static class CaptureSharedTileLiveTargetPatch
    {
        [HarmonyPriority(Priority.First)]
        public static void Prefix() => SharedTileLiveSync.CaptureSelectedTarget();
    }

    [HarmonyPatch(typeof(Game), "InitNewGame")]
    public static class BeginSharedTileLiveJoinPatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Game __instance) => SharedTileLiveSync.BeginPendingJoin(__instance);
    }

    [HarmonyPatch]
    public static class AutoAcceptSharedTileLiveJoinPatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("RTClient.PacketManagers.Synchronous.PM_Synchronous"), "OnAsk");

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(object[] __args)
        {
            PKT_Synchronous packet = __args.OfType<PKT_Synchronous>().FirstOrDefault();
            return packet == null || !SharedTileLiveSync.TryAutoAccept(packet);
        }
    }

    [HarmonyPatch]
    public static class ApplySharedTileLiveAcceptPatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("RTClient.PacketManagers.Synchronous.PM_Synchronous"), "OnAccept");

        [HarmonyPriority(Priority.First)]
        public static bool Prefix(object[] __args)
        {
            PKT_Synchronous packet = __args.OfType<PKT_Synchronous>().FirstOrDefault();
            return packet == null || !SharedTileLiveSync.TryApplyAccept(packet);
        }
    }

    [HarmonyPatch]
    public static class SharedTileInitialClockPatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(PM_SGameSpeed), "SendFirstSpeed");

        [HarmonyPriority(Priority.First)]
        public static bool Prefix()
        {
            if (!SharedTileLiveSync.SharedHostActiveOrPending || !SessionManager.IsSynchronousHost) return true;
            SharedTileLiveSync.SendHostClock();
            return false;
        }
    }

    [HarmonyPatch(typeof(TickManager), "DoSingleTick")]
    public static class SharedTileHostClockPatch
    {
        [HarmonyPostfix]
        public static void Postfix() => SharedTileLiveSync.MaybeResyncHostClock();
    }

    /// <summary>
    /// A remote player's pawn is a mirror. Its owner's client is the only place
    /// autonomous/ordered jobs may originate; network-applied jobs run inside
    /// RWT's bypass flag and remain allowed.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
    public static class RemotePawnJobAuthorityPatch
    {
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(Pawn ___pawn)
        {
            if (!SharedTileLiveSync.IsSharedSessionActive) return true;
            if (SharedColonyState.IsRwtBypassActive()) return true;
            return ___pawn == null || !PlayerFactionRegistry.IsRemoteFaction(___pawn.Faction);
        }
    }
}
