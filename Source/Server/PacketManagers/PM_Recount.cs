using RTNetwork.Components;
using RTNetwork.PacketManagers;
using RTShared.Misc;

namespace RTServer.PacketManagers
{
    public class PM_Recount : PM_Base
    {
        [HandlesPacket(PacketHeader.PlayerRecount)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {

        }
    }
}