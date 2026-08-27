namespace RWTSharedColony
{
    // Keeps the companion diagnostics independent of RTClient's internal using
    // layout while exposing the same endpoint used by the existing live-sync code.
    internal static class Network
    {
        public static RTNetwork.Components.ServerClient ServerEndpoint => RTNetwork.Components.Network.ServerEndpoint;
    }
}
