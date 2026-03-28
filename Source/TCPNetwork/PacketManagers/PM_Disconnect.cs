using Shared;
using System;
using System.Collections.Generic;
using System.Text;
using TCPNetwork.Files.Client;

namespace TCPNetwork.PacketManagers
{
    public class PM_Disconnect : PM_Base
    {
        [HandlesPacket(PacketHeader.DisconnectManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            client.Listener.MarkForDisconnect(false);
        }
    }
}