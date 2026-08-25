using RTServer.Hooks.TCPNetwork;
using RTShared.Files.Player;

namespace RTServer.Managers
{
    public sealed class PendingSharedSession
    {
        public string RequesterUsername { get; set; }

        public string TargetUsername { get; set; }

        public DateTime CreatedUtc { get; set; }
    }

    /// <summary>
    /// The stock synchronous protocol routes by world tile. That becomes
    /// ambiguous when up to four players occupy the same tile, so pending
    /// requests are keyed by explicit usernames until both clients are linked.
    /// </summary>
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
            }
        }

        public static string ConsumeNextTarget(ServerClient requester)
        {
            lock (SessionLock)
            {
                RemoveExpired();
                string requesterUsername = requester.GetData<FL_Player>().Username;
                if (!NextTargetByRequester.Remove(requesterUsername, out PendingSharedSession pending)) return null;
                return pending.TargetUsername;
            }
        }

        public static bool TryRegister(ServerClient requester, ServerClient target)
        {
            lock (SessionLock)
            {
                RemoveExpired();
                string targetUsername = target.GetData<FL_Player>().Username;
                if (PendingByTarget.ContainsKey(targetUsername)) return false;

                PendingByTarget[targetUsername] = new PendingSharedSession
                {
                    RequesterUsername = requester.GetData<FL_Player>().Username,
                    TargetUsername = targetUsername,
                    CreatedUtc = DateTime.UtcNow
                };
                return true;
            }
        }

        public static ServerClient ConsumeRequester(ServerClient target)
        {
            lock (SessionLock)
            {
                RemoveExpired();
                string targetUsername = target.GetData<FL_Player>().Username;
                if (!PendingByTarget.Remove(targetUsername, out PendingSharedSession pending)) return null;
                return ServerNetwork.GetConnectedClientFromUsername(pending.RequesterUsername);
            }
        }

        private static void RemoveExpired()
        {
            DateTime cutoff = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(2));
            foreach (string key in PendingByTarget.Where(fetch => fetch.Value.CreatedUtc < cutoff)
                         .Select(fetch => fetch.Key)
                         .ToArray())
            {
                PendingByTarget.Remove(key);
            }

            foreach (string key in NextTargetByRequester.Where(fetch => fetch.Value.CreatedUtc < cutoff)
                         .Select(fetch => fetch.Key)
                         .ToArray())
            {
                NextTargetByRequester.Remove(key);
            }
        }
    }
}
