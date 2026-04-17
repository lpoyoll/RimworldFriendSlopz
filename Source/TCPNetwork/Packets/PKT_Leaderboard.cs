using Shared.Files;

namespace TCPNetwork.Packets
{
    public class PKT_Leaderboard : PKT_Base
    {
        public FL_Leaderboard _file { get; set; } = null;
    }
}
