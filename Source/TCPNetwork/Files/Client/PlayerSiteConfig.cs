using Shared.Files.Sites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPNetwork.Files.Client
{
    public class PlayerSiteConfig
    {
        public string DefName { get; set; } = string.Empty;

        public SiteReward Reward { get; set; } = null;
    }
}
