using RTShared;
using RTNetwork.PacketManagers;
using RTNetwork.Components;

namespace GameServer.PacketManagers.ServerBrowser
{
    public class PM_Listing : PM_Base
    {
        [HandlesPacket(PacketHeader.ServerBrowserListing)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            throw new NotImplementedException();
        }
    }
}
