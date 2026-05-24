using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCPNetwork;
using TCPNetwork.PacketManagers;

namespace GameClient.PacketManagers.ServerBrowser
{
    public class PM_Telemetry : PM_Base
    {
        [HandlesPacket(PacketHeader.ServerBrowserTelemetry)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            throw new NotImplementedException();
        }
    }
}
