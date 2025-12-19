using static Shared.CommonEnumerators;

namespace TCPNetwork.Packets.Goodwills;

public class SiteGoodwill
{
    public int Tile { get; set; } = -1;

    public Goodwill Goodwill { get; set; } = Goodwill.Neutral;
}