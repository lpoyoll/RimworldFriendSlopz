using Shared;
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