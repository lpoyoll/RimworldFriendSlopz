using RTNetwork.Components;
using RTNetwork.PacketManagers;
using RTShared.Misc;

namespace RTServer.PacketManagers.ServerBrowser
{
    public class PM_Report : PM_Base
    {
        [HandlesPacket(PacketHeader.ServerBrowserReport)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            throw new NotImplementedException();
        }
    }
}