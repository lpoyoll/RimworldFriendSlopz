using MessagePack;

namespace Shared 
{
    [MessagePackObject]
    public class SiteRewardConfigData 
    {
        public string _siteDef;
        
        public string _rewardDef;
    }
}