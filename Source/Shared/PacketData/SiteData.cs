using System;
using static Shared.CommonEnumerators;

namespace Shared
{
    [Serializable]
    public class SiteData
    {
        public SiteStepMode _stepMode;

        public SiteFile _siteFile = new SiteFile();

        public SiteRewardConfigData _siteConfigFile;
    }
}
