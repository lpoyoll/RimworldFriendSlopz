using Shared.Files.Sites;

namespace TCPNetwork.Files.Client
{
    public class PlayerSiteConfig
    {
        public string DefName { get; set; } = string.Empty;

        public SiteReward Reward { get; set; } = null;
    }
}
