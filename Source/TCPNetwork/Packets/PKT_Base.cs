using Shared;

namespace TCPNetwork
{
    public class PKT_Base
    {
        public PacketHeader Header { get; set; } = byte.MinValue;

        public bool MainThread { get; set; } = false;

        public byte[] Contents { get; set; } = null;
    }
}
