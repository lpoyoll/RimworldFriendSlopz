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
            if (parts.Length < 4 || parts[0] != SharedColonyState.ProtocolPrefix || parts[1] != "SETTLED")
                return false;

            if (!int.TryParse(parts[2], out int tile)) return true;
            string username = parts[3];
            if (!string.Equals(username, SessionManager.Username, StringComparison.OrdinalIgnoreCase)) return true;

            RegisteredOwnTile = tile;
            Log.Message($"[Rimjob] Server confirmed local settlement registration on tile {tile}.");
            TryStartAfterRegistration();
            return true;
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

            SetPrivateAutoProperty(nameof(SharedTileLiveSync.PendingTile), tile);
            SetPrivateAutoProperty(nameof(SharedTileLiveSync.PendingHostUsername), username);
            SharedColonyState.PendingRemoteUsername = username;
            Log.Message($"[Rimjob] Recovered shared live-map target: {username} on tile {tile}.");
        }

        private static void TryStartAfterRegistration()
        {
            if (JoinStarted || PendingGame == null) return;
            if (SharedTileLiveSync.PendingTile < 0 || RegisteredOwnTile != SharedTileLiveSync.PendingTile) return;

            JoinStarted = true;
            try
            {
                AllowOriginalBegin = true;
                SharedTileLiveSync.BeginPendingJoin(PendingGame);
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
        [HarmonyPostfix]
        public static void Postfix() => SharedTileLiveSyncHandshake.RecoverTargetFromSelection();
    }

    [HarmonyPatch(typeof(SharedColonyState), nameof(SharedColonyState.HandleProtocol))]
    public static class SharedTileSettlementAckProtocolPatch
    {
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(string message) => !SharedTileLiveSyncHandshake.HandleProtocol(message);
    }
}
