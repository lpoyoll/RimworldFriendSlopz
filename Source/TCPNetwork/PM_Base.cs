using Shared;
using TCPNetwork.Files.Client;

namespace TCPNetwork
{
    [ManagesPacket]
    public abstract class PM_Base
    {
        public abstract void Receive(ServerClient client, byte[] bytes, PacketHeader header);
    }
}
