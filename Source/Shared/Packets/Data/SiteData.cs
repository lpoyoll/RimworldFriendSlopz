using static Shared.CommonEnumerators;

namespace Shared
{

    public class SiteData
    {
        public SiteStepMode _stepMode;

        public SiteFile _file = new SiteFile();

        public SiteRewardConfigData _rewardConfig;

        public SiteRewardFile[] _rewardFiles;

        public MapFile _siteMap;
    }
}
