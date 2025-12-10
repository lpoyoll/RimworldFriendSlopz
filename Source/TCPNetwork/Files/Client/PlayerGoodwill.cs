using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Shared.CommonEnumerators;

namespace TCPNetwork.Files.Client
{
    public class PlayerGoodwill
    {
        public string Name { get; set; } = string.Empty;

        public Goodwill Goodwill { get; set; } = Goodwill.Neutral;
    }
}
