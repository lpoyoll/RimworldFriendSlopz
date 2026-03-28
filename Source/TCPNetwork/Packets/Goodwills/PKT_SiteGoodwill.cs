using static Shared.CommonEnumerators;

namespace TCPNetwork.Packets.Goodwills
{
    public class PKT_SiteGoodwill : PKT_Base
    {
        public int Tile { get; set; } = -1;

        public Goodwill Goodwill { get; set; } = Goodwill.Neutral;
    }
}
