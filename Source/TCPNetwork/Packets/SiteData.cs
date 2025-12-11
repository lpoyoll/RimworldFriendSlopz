using Shared.Files;
using Shared.Files.Sites;
using static Shared.CommonEnumerators;

namespace TCPNetwork.Packets
{

    public class SiteData
    {
        public SiteStepMode _stepMode { get; set; } = SiteStepMode.Accept;

        public SiteFile _file { get; set; } = new SiteFile();

        public SiteRewardConfigData _rewardConfig { get; set; } = null;

        public SiteReward[] _rewardFiles { get; set; } = null;
    }
}
