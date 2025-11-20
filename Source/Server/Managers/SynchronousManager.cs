using Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCPNetwork.Packets;
using TCPNetwork.Server;

namespace GameServer.Managers
{
    public static class SynchronousManager
    {
        [HandlesPacket(PacketHeader.SPlayerDraft)]
        private static void HandleSPacket(ServerClient client, byte[] bytes)
        {
            client.Listener.EnqueuePacket(PacketHeader.SPlayerDraft, bytes);
        }
    }
}
