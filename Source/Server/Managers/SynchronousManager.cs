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
        private static void SPlayerDraft(ServerClient client, byte[] bytes, PacketHeader header)
        {
            client.Listener.EnqueuePacket(header, bytes);
        }

        [HandlesPacket(PacketHeader.SPlayerWeather)]
        private static void SPlayerWeather(ServerClient client, byte[] bytes, PacketHeader header)
        {
            client.Listener.EnqueuePacket(header, bytes);
        }

        [HandlesPacket(PacketHeader.SPlayerMentalState)]
        private static void SPlayerMentalState(ServerClient client, byte[] bytes, PacketHeader header)
        {
            client.Listener.EnqueuePacket(header, bytes);
        }
    }
}
