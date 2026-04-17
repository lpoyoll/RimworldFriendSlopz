using Shared.Files.Sites;

namespace TCPNetwork.Packets
{
    public class PKT_Site : PKT_Base
    {
        public enum SiteStepMode { Accept, Build, Destroy, Info, Config, Rewards, Worker }

        public SiteStepMode _stepMode { get; set; } = SiteStepMode.Accept;

        public Site _file { get; set; } = new Site();

        public PKT_SiteRewardConfig _rewardConfig { get; set; } = null;

        public SiteReward[] _rewardFiles { get; set; } = null;
    }
}
