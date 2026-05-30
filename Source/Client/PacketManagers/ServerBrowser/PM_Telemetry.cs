using RTShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RTNetwork;
using RTNetwork.PacketManagers;
using RTNetwork.Components;

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
