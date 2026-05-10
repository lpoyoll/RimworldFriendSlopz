using Shared.Files.Sites;

namespace TCPNetwork.Packets
{
    public class PKT_Site : PKT_Base
    {
        public enum SiteStepMode { Accept, Build, Destroy, Info, Config, Rewards, Worker }

        public SiteStepMode _stepMode { get; set; } = SiteStepMode.Accept;

        public FL_Site _file { get; set; } = new FL_Site();

        public PKT_SiteRewardConfig _rewardConfig { get; set; } = null;

        public FL_SiteReward[] _rewardFiles { get; set; } = null;
    }
}
