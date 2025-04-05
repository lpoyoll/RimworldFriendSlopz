using GameServer.TCP;
using Shared;

namespace GameServer.Managers
{
    [RTManager]
    public static class KeepAliveManager
    {
        [HandlesPacket(PacketHeader.KeepAliveManager)]
        private static void ParsePacket(ServerClient client, byte[] bytes)
        {

        }
    }
}