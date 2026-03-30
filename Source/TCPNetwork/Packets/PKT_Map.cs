using Shared.Files;

namespace TCPNetwork.Packets
{
    public class PKT_Map : PKT_Base
    {
        public MapFile File { get; set; } = new MapFile();
    }
}