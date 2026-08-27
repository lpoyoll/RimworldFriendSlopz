using System.Collections.Concurrent;

namespace RTServer.Managers
{
    /// <summary>
    /// Tracks live synchronous invitations and sessions by network client ID.
    /// World tiles are not unique once multiple players share a colony, so they
    /// must never be used as a player address.
    /// </summary>
    public sealed class SynchronousSessionRegistry
    {
        private readonly ConcurrentDictionary<int, int> pendingRequesterByHost = new();
        private readonly ConcurrentDictionary<int, int> partnerByClient = new();

        public bool TryRegisterRequest(int requesterId, int hostId)
        {
            if (requesterId == hostId) return false;
            if (partnerByClient.ContainsKey(requesterId) || partnerByClient.ContainsKey(hostId)) return false;

            pendingRequesterByHost[hostId] = requesterId;
            return true;
        }

        public bool TryAccept(int hostId, out int requesterId)
        {
            if (!pendingRequesterByHost.TryRemove(hostId, out requesterId)) return false;
            if (requesterId == hostId) return false;

            partnerByClient[hostId] = requesterId;
            partnerByClient[requesterId] = hostId;
            return true;
        }

        public bool TryReject(int hostId, out int requesterId)
        {
            return pendingRequesterByHost.TryRemove(hostId, out requesterId);
        }

        public bool TryGetPartner(int clientId, out int partnerId)
        {
            return partnerByClient.TryGetValue(clientId, out partnerId);
        }

        public void ClearClient(int clientId)
        {
            pendingRequesterByHost.TryRemove(clientId, out _);

            foreach (KeyValuePair<int, int> pending in pendingRequesterByHost)
            {
                if (pending.Value == clientId) pendingRequesterByHost.TryRemove(pending.Key, out _);
            }

            if (partnerByClient.TryRemove(clientId, out int partnerId))
            {
                partnerByClient.TryRemove(partnerId, out _);
            }
        }
    }
}
