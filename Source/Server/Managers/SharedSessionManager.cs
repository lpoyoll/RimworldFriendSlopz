using RTNetwork.Components;
using RTServer.Hooks.TCPNetwork;
using RTShared.Files.Player;
using RTShared.Misc;

namespace RTServer.Managers
{
    public sealed class PendingSharedSession
    {
        public string RequesterUsername { get; set; }
        public string TargetUsername { get; set; }
        public DateTime CreatedUtc { get; set; }
    }

    public static class SharedSessionManager
    {
        private static readonly object SessionLock = new object();
        private static readonly Dictionary<string, PendingSharedSession> PendingByTarget =
            new Dictionary<string, PendingSharedSession>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, PendingSharedSession> NextTargetByRequester =
            new Dictionary<string, PendingSharedSession>(StringComparer.OrdinalIgnoreCase);

        public static void SetNextTarget(ServerClient requester, string targetUsername)
        {
            lock (SessionLock)
            {
                RemoveExpired();
                string requesterUsername = requester.GetData<FL_Player>().Username;
                NextTargetByRequester[requesterUsername] = new PendingSharedSession
                {
                    RequesterUsername = requesterUsername,
                    TargetUsername = targetUsername,
                    CreatedUtc = DateTime.UtcNow
                };
                Printer.Message($"[SHARED-SESSION] Explicit target queued | Requester={requesterUsername} | Target={targetUsername}", Printer.Verbosity.Verbose);
            }
        }

        public static string ConsumeNextTarget(ServerClient requester)
        {
            lock (SessionLock)
            {
                RemoveExpired();
                string requesterUsername = requester.GetData<FL_Player>().Username;
                if (!NextTargetByRequester.Remove(requesterUsername, out PendingSharedSession pending))
                {
                    Printer.Message($"[SHARED-SESSION] No queued target | Requester={requesterUsername}", Printer.Verbosity.Verbose);
                    return null;
                }
                Printer.Message($"[SHARED-SESSION] Queued target consumed | Requester={requesterUsername} | Target={pending.TargetUsername}", Printer.Verbosity.Verbose);
                return pending.TargetUsername;
            }
        }

        public static bool TryRegister(ServerClient requester, ServerClient target)
        {
            lock (SessionLock)
            {
                RemoveExpired();
                string requesterUsername = requester.GetData<FL_Player>().Username;
                string targetUsername = target.GetData<FL_Player>().Username;
                if (PendingByTarget.ContainsKey(targetUsername))
                {
                    Printer.Message($"[SHARED-SESSION] Pair registration refused | Requester={requesterUsername} | Target={targetUsername} | Reason=Target already has a pending request", Printer.Verbosity.Verbose);
                    return false;
                }

                PendingByTarget[targetUsername] = new PendingSharedSession
                {
                    RequesterUsername = requesterUsername,
                    TargetUsername = targetUsername,
                    CreatedUtc = DateTime.UtcNow
                };
                Printer.Message($"[SHARED-SESSION] Pair registered | Requester={requesterUsername} | Target={targetUsername}", Printer.Verbosity.Verbose);
                return true;
            }
        }

        public static ServerClient ConsumeRequester(ServerClient target)
        {
            lock (SessionLock)
            {
                RemoveExpired();
                string targetUsername = target.GetData<FL_Player>().Username;
                if (!PendingByTarget.Remove(targetUsername, out PendingSharedSession pending))
                {
                    Printer.Message($"[SHARED-SESSION] Accept had no pending requester | Target={targetUsername}", Printer.Verbosity.Verbose);
                    return null;
                }

                ServerClient requester = ServerNetwork.GetConnectedClientFromUsername(pending.RequesterUsername);
                Printer.Message($"[SHARED-SESSION] Pending requester consumed | Target={targetUsername} | Requester={pending.RequesterUsername} | RequesterOnline={requester != null}", Printer.Verbosity.Verbose);
                return requester;
            }
        }

        private static void RemoveExpired()
        {
            DateTime cutoff = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(2));
            foreach (string key in PendingByTarget.Where(fetch => fetch.Value.CreatedUtc < cutoff).Select(fetch => fetch.Key).ToArray())
            {
                PendingSharedSession expired = PendingByTarget[key];
                PendingByTarget.Remove(key);
                Printer.Message($"[SHARED-SESSION] Pending pair expired | Requester={expired.RequesterUsername} | Target={expired.TargetUsername}", Printer.Verbosity.Verbose);
            }

            foreach (string key in NextTargetByRequester.Where(fetch => fetch.Value.CreatedUtc < cutoff).Select(fetch => fetch.Key).ToArray())
            {
                PendingSharedSession expired = NextTargetByRequester[key];
                NextTargetByRequester.Remove(key);
                Printer.Message($"[SHARED-SESSION] Queued target expired | Requester={expired.RequesterUsername} | Target={expired.TargetUsername}", Printer.Verbosity.Verbose);
            }
        }
    }
}
