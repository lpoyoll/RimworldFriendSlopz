using Shared;
using TCPNetwork;
using TCPNetwork.PacketManagers;

namespace GameServer.PacketManager
{
    public class PM_Recount : PM_Base
    {
        [HandlesPacket(PacketHeader.PlayerRecount)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {

        }
    }
}