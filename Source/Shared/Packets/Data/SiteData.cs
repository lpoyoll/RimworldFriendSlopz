using System;
using MessagePack;
using static Shared.CommonEnumerators;

namespace Shared
{
    [MessagePackObject]
    public class SiteData
    {
        public SiteStepMode _stepMode;

        public SiteFile _file = new SiteFile();

        public SiteRewardConfigData _rewardConfig;

        public SiteRewardFile[] _rewardFiles;

        public MapFile _siteMap;
    }
}
