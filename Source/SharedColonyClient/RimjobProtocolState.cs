using System;
using HarmonyLib;

namespace RWTSharedColony
{
    public static class RimjobProtocolState
    {
        public const string ExpectedBuild = "0.1.27";
        public const string ExpectedPrivateProtocol = "RJ25";

        private static bool _waitingLogged;

        public static string ServerBuild { get; private set; }
        public static string ServerPrivateProtocol { get; private set; }
        public static bool PrivateSyncReady { get; private set; }

        public static bool TryConsumeProtocol(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            string[] parts = message.Split('|');
            if (parts.Length < 2 || parts[0] != SharedColonyState.ProtocolPrefix)
                return false;

            if (parts[1] == "SESSION_END")
            {
                string reason = parts.Length >= 3 ? parts[2] : "The paired player disconnected.";
                SharedTileLiveSync.EndSession(reason);
                Reset();
                return true;
            }

            if (parts.Length < 4 || parts[1] != "BUILD") return false;

            ServerBuild = parts[2];
            ServerPrivateProtocol = parts[3];
            PrivateSyncReady =
                string.Equals(ServerBuild, ExpectedBuild, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(ServerPrivateProtocol, ExpectedPrivateProtocol, StringComparison.OrdinalIgnoreCase);
            _waitingLogged = false;

            if (PrivateSyncReady)
            {
                RimjobClientDiagnostics.Important(
                    $"Compatible Rimjob server protocol confirmed. ServerBuild={ServerBuild}; Protocol={ServerPrivateProtocol}.");
            }
            else
            {
                RimjobClientDiagnostics.Error(
                    $"Rimjob private sync disabled: client expects {ExpectedPrivateProtocol} but server advertised '{ServerPrivateProtocol ?? "<none>"}' (build {ServerBuild ?? "<unknown>"}). Update the server executable as well as both clients.");
            }

            return true;
        }

        public static void Reset()
        {
            if (ServerBuild == null && ServerPrivateProtocol == null && !PrivateSyncReady) return;
            ServerBuild = null;
            ServerPrivateProtocol = null;
            PrivateSyncReady = false;
            _waitingLogged = false;
        }

        public static void LogWaitingOnce()
        {
            if (_waitingLogged) return;
            _waitingLogged = true;
            RimjobClientDiagnostics.Important(
                $"Shared session exists but private pawn/world sync is waiting for server protocol {ExpectedPrivateProtocol}. If this persists, replace the server with the same Rimjob release as the clients.");
        }
    }

    [HarmonyPatch(typeof(SharedColonyState), nameof(SharedColonyState.HandleProtocol))]
    public static class RimjobBuildProtocolPatch
    {
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(string message) => !RimjobProtocolState.TryConsumeProtocol(message);
    }

    /// <summary>
    /// v0.1.22 emitted pawn position packets at roughly 10 Hz immediately after
    /// a session started. Against an older server that became an exception/log
    /// storm. v0.1.23 requires a matching server advertisement and caps the
    /// publisher to 5 Hz even though SharedPawnStateSync keeps its own timer too.
    /// </summary>
    [HarmonyPatch(typeof(SharedPawnStateSync), nameof(SharedPawnStateSync.Update))]
    public static class RimjobPrivateSyncGuardPatch
    {
        private static long _lastAllowedUtcTicks;

        [HarmonyPriority(Priority.First)]
        public static bool Prefix()
        {
            if (!SharedTileLiveSync.IsSharedSessionActive)
            {
                // BUILD is deliberately sent immediately before the Accept
                // packet.  The guest may therefore receive it while it is still
                // awaiting the host map.  Do not erase that valid negotiation
                // state in the frame between the two packets.
                if (!SharedTileLiveSync.AwaitingAccept)
                    RimjobProtocolState.Reset();
                _lastAllowedUtcTicks = 0;
                return true;
            }

            if (!RimjobProtocolState.PrivateSyncReady)
            {
                RimjobProtocolState.LogWaitingOnce();
                return false;
            }

            long now = DateTime.UtcNow.Ticks;
            if (_lastAllowedUtcTicks != 0 && now - _lastAllowedUtcTicks < TimeSpan.TicksPerMillisecond * 200)
                return false;

            _lastAllowedUtcTicks = now;
            return true;
        }
    }
}
