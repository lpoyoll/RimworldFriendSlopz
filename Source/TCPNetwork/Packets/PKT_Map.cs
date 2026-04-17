using Shared.Files;

namespace TCPNetwork.Packets
{
    public class PKT_Map : PKT_Base
    {
        public FL_Map File { get; set; } = new FL_Map();
    }
}