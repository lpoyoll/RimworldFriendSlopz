using Shared;

namespace GameClient.Managers
{
    [RTManager]
    public static class KeepAliveManager
    {
        [HandlesPacket(PacketHeader.KeepAliveManager)]
        private static void ParsePacket(byte[] bytes)
        {

        }
    }
}