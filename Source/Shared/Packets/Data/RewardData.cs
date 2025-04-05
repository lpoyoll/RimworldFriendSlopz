using MessagePack;

namespace Shared 
{
    [MessagePackObject]
    public class RewardData 
    {
        public SiteRewardFile[] _rewardData;
    }
}