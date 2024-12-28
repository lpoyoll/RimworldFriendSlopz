using Shared;

namespace GameServer
{
    [RTManager]
    public static class KeepAliveManager
    {
        public static void ParsePacket(ServerClient client, Packet packet)
        {
            client.listener.KAFlag = true;
        }
    }
}