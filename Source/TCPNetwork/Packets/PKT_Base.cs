using Shared;

namespace TCPNetwork
{
    public class PKT_Base
    {
        public PacketHeader Header { get; set; } = byte.MinValue;

        public byte[] Contents { get; set; } = null;
    }
}
