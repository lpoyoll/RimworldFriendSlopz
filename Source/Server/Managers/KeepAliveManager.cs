using Shared.Network.Server;
using Shared;

namespace GameServer.Managers
{

    public static class KeepAliveManager
    {
        [HandlesPacket(PacketHeader.KeepAliveManager)]
        private static void ParsePacket(ServerClient client, byte[] bytes)
        {

        }
    }
}