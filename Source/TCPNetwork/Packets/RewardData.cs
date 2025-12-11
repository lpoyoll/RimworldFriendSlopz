using Shared.Files.Sites;

namespace TCPNetwork.Packets 
{

    public class RewardData 
    {
        public SiteReward[] _rewardData { get; set; } = null;

        public override string ToString()
        {
            return $"RewardData:|{_rewardData?.Length ?? 0}";
        }
    }
}