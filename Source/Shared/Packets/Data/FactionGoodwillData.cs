using System.Collections.Generic;
using static Shared.CommonEnumerators;

namespace Shared
{

    public class FactionGoodwillData
    {
        public int _tile { get; set; }

        public string _uid { get; set; }

        public Goodwill _goodwill { get; set; }

        //Settlements

        public List<int> _settlementTiles { get; set; } = new List<int>();

        public Goodwill[] _settlementGoodwills { get; set; }

        //Sites

        public List<int> _siteTiles { get; set; } = new List<int>();
        
        public Goodwill[] _siteGoodwills { get; set; }
    }
}
