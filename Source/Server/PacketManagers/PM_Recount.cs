using RTShared;
using RTNetwork;
using RTNetwork.PacketManagers;
using RTNetwork.Components;

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