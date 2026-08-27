using System;
using System.Reflection;
using HarmonyLib;
using RTClient.Managers;
using Verse;

namespace RWTSharedColony
{
    /// <summary>
    /// v0.1.17 visibility/rejoin tracing. These messages go to the dedicated
    /// Rimjob client log and become visible in the F9 Verbose Log tab.
    /// </summary>
    [HarmonyPatch]
    public static class SharedTileProtocolTracePatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(SharedTileLiveSyncHandshake), nameof(SharedTileLiveSyncHandshake.HandleProtocol));

        [HarmonyPriority(Priority.First)]
        public static void Prefix(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (message.StartsWith(SharedColonyState.ProtocolPrefix + "|TILE|", StringComparison.Ordinal) ||
                message.StartsWith(SharedColonyState.ProtocolPrefix + "|SETTLED|", StringComparison.Ordinal))
            {
                RimjobClientDiagnostics.Important("Shared-tile protocol check: " + message);
            }
            else
            {
                RimjobClientDiagnostics.Verbose("Shared protocol check: " + message);
            }
        }
    }

    [HarmonyPatch]
    public static class ExistingSharedTileArmTracePatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(SharedTileLiveSyncHandshake), nameof(SharedTileLiveSyncHandshake.ArmExistingSharedTile));

        [HarmonyPriority(Priority.First)]
        public static void Prefix(Game game)
        {
            int currentTile = game?.CurrentMap?.Tile.tileId ?? -1;
            RimjobClientDiagnostics.Important(
                $"Checking existing tile session after load. User={SessionManager.Username ?? "<unknown>"}; CurrentTile={currentTile}; " +
                $"PendingTile={SharedTileLiveSync.PendingTile}; PendingHost={SharedTileLiveSync.PendingHostUsername ?? "<none>"}; " +
                $"Ready={SessionManager.IsReadyToPlay}; Network={SessionManager.CurrentNetworkState}");
        }

        [HarmonyPostfix]
        public static void Postfix()
        {
            RimjobClientDiagnostics.Verbose(
                $"Existing tile check completed. PendingTile={SharedTileLiveSync.PendingTile}; " +
                $"PendingHost={SharedTileLiveSync.PendingHostUsername ?? "<none>"}; AwaitingAccept={SharedTileLiveSync.AwaitingAccept}; " +
                $"GuestActive={SharedTileLiveSync.SharedGuestActive}; HostActive={SharedTileLiveSync.SharedHostActiveOrPending}");
        }
    }

    [HarmonyPatch]
    public static class SharedJoinBeginTracePatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(SharedTileLiveSync), nameof(SharedTileLiveSync.BeginPendingJoin));

        [HarmonyPriority(Priority.First)]
        public static void Prefix(Game game)
        {
            RimjobClientDiagnostics.Important(
                $"Begin shared-map join evaluated. CurrentTile={(game?.CurrentMap?.Tile.tileId ?? -1)}; " +
                $"PendingTile={SharedTileLiveSync.PendingTile}; Host={SharedTileLiveSync.PendingHostUsername ?? "<none>"}; " +
                $"ServerEndpoint={Network.ServerEndpoint != null}");
        }
    }

    [HarmonyPatch]
    public static class SharedJoinApplyTracePatch
    {
        public static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(SharedTileLiveSync), nameof(SharedTileLiveSync.TryApplyAccept));

        [HarmonyPriority(Priority.First)]
        public static void Prefix()
        {
            RimjobClientDiagnostics.Important(
                $"Host map acceptance received/evaluated. AwaitingAccept={SharedTileLiveSync.AwaitingAccept}; " +
                $"PendingTile={SharedTileLiveSync.PendingTile}; Host={SharedTileLiveSync.PendingHostUsername ?? "<none>"}");
        }

        [HarmonyPostfix]
        public static void Postfix(bool __result)
        {
            RimjobClientDiagnostics.Important(
                $"Host map acceptance result={__result}. GuestActive={SharedTileLiveSync.SharedGuestActive}; " +
                $"SyncMapTile={(SessionManager.SynchronousMap?.Tile.tileId ?? -1)}");
        }
    }
}
