using System.Collections.Generic;

namespace TCPNetwork.Packets
{
    public class PKT_PlayerRecount : PKT_Base
    {
        public int CurrentPlayerCount { get; set; } = int.MinValue;
    }
}
