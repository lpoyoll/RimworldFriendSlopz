using Shared.Files;

namespace TCPNetwork.Packets
{
    public class PKT_Leaderboard : PKT_Base
    {
        public LeaderboardFile _file { get; set; } = null;
    }
}
