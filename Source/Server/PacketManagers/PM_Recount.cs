using Shared;
using TCPNetwork.Files.Client;
using TCPNetwork.PacketManagers;

namespace GameServer.PacketManager
{
    public class PM_Recount : PM_Base
    {
        [HandlesPacket(PacketHeader.RecountManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {

        }
    }
}