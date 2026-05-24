using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCPNetwork;
using TCPNetwork.PacketManagers;

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
