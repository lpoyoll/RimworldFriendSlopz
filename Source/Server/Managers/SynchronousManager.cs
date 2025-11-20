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
        private static void ParsePacket(ServerClient client, byte[] bytes, PacketHeader header)
        {
            client.Listener.EnqueuePacket(header, bytes);
        }
    }
}
