using System.Collections;
using System.Collections.Generic;
using static Shared.CommonEnumerators;

namespace TCPNetwork.Packets.Goodwills
{
    public class FactionGoodwillData
    {
        public int _tile { get; set; } = -1;

        public string _username { get; set; } = string.Empty;

        public Goodwill _goodwill { get; set; } = Goodwill.Neutral;

        public List<SettlementGoodwill> _settlements { get; set; } = new List<SettlementGoodwill>();

        public List<SiteGoodwill> _sites { get; set; } = new List<SiteGoodwill>();
    }
}
