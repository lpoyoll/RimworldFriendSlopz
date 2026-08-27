using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RTClient.Managers;
using RimWorld.Planet;
using Verse;

namespace RWTSharedColony
{
    /// <summary>
    /// v0.1.14: makes the same-tile live-map join deterministic.
    ///
    /// v0.1.13 sent the synchronous Ask directly from Game.InitNewGame and
    /// assumed the stock Settlement packet had already been processed by the
    /// server. That is not guaranteed: both are queued around the same postfix,
    /// so the Ask can reach PM_Synchronous before the requester's settlement file
    /// exists. The server then cannot resolve FromTile and the live handover never
    /// starts even though the occupied tile itself was accepted.
    ///
    /// This shim suppresses the original BeginPendingJoin until the server sends
    /// an explicit SETTLED acknowledgement. It also recovers the remote owner from
    /// the selected/starting tile if RimWorld cleared FirstSelectedObject before
    /// the original capture patch ran.
    /// </summary>
    public static class SharedTileLiveSyncHandshake
    {
        private static Game PendingGame;
        private static int RegisteredOwnTile = -1;
        private static bool AllowOriginalBegin;
        private static bool JoinStarted;
        private static int KnownSharedTile = -1;
        private static string KnownHostUsername;
        private static long LastAttemptUtcTicks;
        private static int JoinAttemptCount;
        private const int MaximumJoinAttempts = 6;
        private const long RetryIntervalTicks = TimeSpan.TicksPerSecond * 10;

        public static void ResetForNewJoin()
        {
            PendingGame = null;
            RegisteredOwnTile = -1;
            AllowOriginalBegin = false;
            JoinStarted = false;
            KnownSharedTile = -1;
            KnownHostUsername = null;
            LastAttemptUtcTicks = 0;
            JoinAttemptCount = 0;
            SharedTileLiveSync.ResetForNewJoin();
            Log.Message("[Rimjob] Shared-map join state reset for a new attempt.");
        }

        public static void Update()
        {
            if (SharedTileLiveSync.SharedGuestActive || PendingGame == null) return;
            if (SharedTileLiveSync.PendingTile < 0 || RegisteredOwnTile != SharedTileLiveSync.PendingTile) return;

            long now = DateTime.UtcNow.Ticks;
            if (SharedTileLiveSync.AwaitingAccept)
            {
                if (LastAttemptUtcTicks == 0 || now - LastAttemptUtcTicks < RetryIntervalTicks) return;

                SetPrivateAutoProperty(nameof(SharedTileLiveSync.AwaitingAccept), false);
                JoinStarted = false;
                Log.Warning($"[Rimjob] Host-map request timed out after 10 seconds; retrying ({JoinAttemptCount}/{MaximumJoinAttempts}).");
            }

            TryStartAfterRegistration();
        }

        public static bool DelayBegin(Game game)
        {
            if (AllowOriginalBegin)
            {
                AllowOriginalBegin = false;
                return true;
            }

            RecoverTargetFromCurrentTile(game);
            if (SharedTileLiveSync.PendingTile < 0 || string.IsNullOrWhiteSpace(SharedTileLiveSync.PendingHostUsername))
                return true;

            PendingGame = game;
            Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
            Log.Message($"[Rimjob] Shared live-map join armed for {SharedTileLiveSync.PendingHostUsername} on tile {SharedTileLiveSync.PendingTile}; waiting for server settlement acknowledgement.");
            TryStartAfterRegistration();
            return false;
        }

        public static bool HandleProtocol(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            string[] parts = message.Split('|');
            if (parts.Length < 2 || parts[0] != SharedColonyState.ProtocolPrefix)
                return false;

            if (parts[1] == "SETTLED" && parts.Length >= 4)
            {
                if (!int.TryParse(parts[2], out int settledTile)) return true;
                string username = parts[3];
                if (!string.Equals(username, SessionManager.Username, StringComparison.OrdinalIgnoreCase)) return true;

                RegisteredOwnTile = settledTile;
                Log.Message($"[Rimjob] Server confirmed local settlement registration on tile {settledTile}.");
                TryStartAfterRegistration();
                return true;
            }

            if (parts[1] != "TILE" || parts.Length < 5) return false;
            if (!int.TryParse(parts[2], out int sharedTile)) return true;
            if (!int.TryParse(parts[4], out int memberCount) || memberCount < 2) return true;

            string hostUsername = parts[3];
            if (string.IsNullOrWhiteSpace(hostUsername)) return true;

            // TILE is a fresh server-side session advertisement (normally after
            // login/reconnect).  Any JoinStarted/AwaitingAccept values belong to
            // the previous socket and must not suppress this attempt.
            ResetForNewJoin();
            KnownSharedTile = sharedTile;
            KnownHostUsername = hostUsername;

            // The canonical host already owns the live map. Every other member
            // should request that map after a save load or reconnect.
            if (string.Equals(hostUsername, SessionManager.Username, StringComparison.OrdinalIgnoreCase))
            {
                Log.Message($"[Rimjob] Local player is canonical host for shared tile {sharedTile} ({memberCount} members).");
                return true;
            }

            RegisteredOwnTile = sharedTile;
            SetPendingTarget(sharedTile, hostUsername);
            Log.Message($"[Rimjob] Server identified {hostUsername} as canonical host for shared tile {sharedTile} ({memberCount} members).");
            ArmExistingSharedTile(Current.Game);
            TryStartAfterRegistration();
            return true;
        }

        public static void ArmExistingSharedTile(Game game)
        {
            if (game?.CurrentMap == null || KnownSharedTile < 0 || string.IsNullOrWhiteSpace(KnownHostUsername)) return;
            if (game.CurrentMap.Tile.tileId != KnownSharedTile) return;
            if (string.Equals(KnownHostUsername, SessionManager.Username, StringComparison.OrdinalIgnoreCase)) return;

            SetPendingTarget(KnownSharedTile, KnownHostUsername);
            PendingGame = game;
            Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
            Log.Message($"[Rimjob] Existing shared-tile save armed for host map rejoin on tile {KnownSharedTile}.");
            TryStartAfterRegistration();
        }

        public static void RecoverTargetFromSelection()
        {
            if (SharedTileLiveSync.PendingTile >= 0 && !string.IsNullOrWhiteSpace(SharedTileLiveSync.PendingHostUsername))
                return;

            int tile = -1;
            WorldObject selected = Find.WorldSelector?.FirstSelectedObject;
            if (selected != null && selected.Tile.Valid) tile = selected.Tile.tileId;
            if (tile < 0 && Find.WorldInterface != null && Find.WorldInterface.SelectedTile.Valid)
                tile = Find.WorldInterface.SelectedTile.tileId;
            if (tile < 0 && Find.GameInitData != null && Find.GameInitData.startingTile.Valid)
                tile = Find.GameInitData.startingTile.tileId;

            RecoverTarget(tile);
        }

        private static void RecoverTargetFromCurrentTile(Game game)
        {
            if (SharedTileLiveSync.PendingTile >= 0 && !string.IsNullOrWhiteSpace(SharedTileLiveSync.PendingHostUsername))
                return;

            int tile = -1;
            if (game?.CurrentMap != null && game.CurrentMap.Tile.Valid) tile = game.CurrentMap.Tile.tileId;
            if (tile < 0 && Find.GameInitData != null && Find.GameInitData.startingTile.Valid)
                tile = Find.GameInitData.startingTile.tileId;

            RecoverTarget(tile);
        }

        private static void RecoverTarget(int tile)
        {
            if (tile < 0 || Find.WorldObjects == null) return;

            WorldObject settlement = Find.WorldObjects.AllWorldObjects.FirstOrDefault(worldObject =>
                worldObject != null &&
                worldObject.Tile.Valid &&
                worldObject.Tile.tileId == tile &&
                SharedTileSelectionUtility.IsRemotePlayerSettlement(worldObject));
            if (settlement == null) return;

            string username = AccessTools.Property(settlement.GetType(), "Name")?.GetValue(settlement, null) as string;
            if (string.IsNullOrWhiteSpace(username)) username = settlement.Label;
            const string suffix = "'s settlement";
            if (!string.IsNullOrWhiteSpace(username) && username.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                username = username.Substring(0, username.Length - suffix.Length);
            if (string.IsNullOrWhiteSpace(username) || string.Equals(username, SessionManager.Username, StringComparison.OrdinalIgnoreCase))
                return;

            SetPendingTarget(tile, username);
            Log.Message($"[Rimjob] Recovered shared live-map target: {username} on tile {tile}.");
        }

        private static void SetPendingTarget(int tile, string username)
        {
            SetPrivateAutoProperty(nameof(SharedTileLiveSync.PendingTile), tile);
            SetPrivateAutoProperty(nameof(SharedTileLiveSync.PendingHostUsername), username);
            SharedColonyState.PendingRemoteUsername = username;
        }

        private static void TryStartAfterRegistration()
        {
            if (SharedTileLiveSync.SharedGuestActive || SharedTileLiveSync.AwaitingAccept) return;
            if (JoinStarted || PendingGame == null) return;
            if (SharedTileLiveSync.PendingTile < 0 || RegisteredOwnTile != SharedTileLiveSync.PendingTile) return;
            if (JoinAttemptCount >= MaximumJoinAttempts)
            {
                Log.Error($"[Rimjob] Shared-map join stopped after {MaximumJoinAttempts} attempts. Reopen the world screen or restart RimWorld to begin a clean attempt.");
                return;
            }

            JoinStarted = true;
            JoinAttemptCount++;
            LastAttemptUtcTicks = DateTime.UtcNow.Ticks;
            try
            {
                AllowOriginalBegin = true;
                SharedTileLiveSync.BeginPendingJoin(PendingGame);
                if (!SharedTileLiveSync.AwaitingAccept)
                {
                    JoinStarted = false;
                    Log.Warning($"[Rimjob] Shared-map request attempt {JoinAttemptCount} did not enter AwaitingAccept; it will be retried.");
                }
                else
                {
                    Log.Message($"[Rimjob] Shared-map request attempt {JoinAttemptCount} sent to {SharedTileLiveSync.PendingHostUsername} for tile {SharedTileLiveSync.PendingTile}.");
                }
            }
            catch (Exception exception)
            {
                JoinStarted = false;
                AllowOriginalBegin = false;
                Log.Error($"[Rimjob] Could not start acknowledged shared live-map join: {exception}");
            }
        }

        private static void SetPrivateAutoProperty(string propertyName, object value)
        {
            PropertyInfo property = AccessTools.Property(typeof(SharedTileLiveSync), propertyName);
            MethodInfo setter = property?.GetSetMethod(true);
            setter?.Invoke(null, new[] { value });
        }
    }

    [HarmonyPatch(typeof(SharedTileLiveSync), nameof(SharedTileLiveSync.BeginPendingJoin))]
    public static class DelaySharedTileJoinUntilSettlementAckPatch
    {
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(Game game) => SharedTileLiveSyncHandshake.DelayBegin(game);
    }

    [HarmonyPatch(typeof(SharedTileLiveSync), nameof(SharedTileLiveSync.CaptureSelectedTarget))]
    public static class RecoverSharedTileTargetPatch
    {
        [HarmonyPrefix]
        public static void Prefix() => SharedTileLiveSyncHandshake.ResetForNewJoin();

        [HarmonyPostfix]
        public static void Postfix() => SharedTileLiveSyncHandshake.RecoverTargetFromSelection();
    }

    [HarmonyPatch]
    public static class SharedTileJoinRetryPatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(AccessTools.TypeByName("Verse.Root_Play"), "Update");

        [HarmonyPostfix]
        public static void Postfix() => SharedTileLiveSyncHandshake.Update();
    }

    [HarmonyPatch(typeof(SharedColonyState), nameof(SharedColonyState.HandleProtocol))]
    public static class SharedTileSettlementAckProtocolPatch
    {
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(string message) => !SharedTileLiveSyncHandshake.HandleProtocol(message);
    }

    [HarmonyPatch(typeof(Game), "FinalizeInit")]
    public static class ResumeExistingSharedTilePatch
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Game __instance) => SharedTileLiveSyncHandshake.ArmExistingSharedTile(__instance);
    }
}
