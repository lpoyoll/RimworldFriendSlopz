using static Shared.CommonEnumerators;

namespace Shared
{

    public class SiteData
    {
        public SiteStepMode _stepMode { get; set; }

        public SiteFile _file { get; set; } = new SiteFile();

        public SiteRewardConfigData _rewardConfig { get; set; }

        public SiteRewardFile[] _rewardFiles { get; set; }

        public MapFile _siteMap { get; set; }
    }
}
