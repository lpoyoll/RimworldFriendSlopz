namespace Shared.Files
{
    public class SiteInfoFile 
    {
        public string DefName { get; set; } = string.Empty;

        public int Cost { get; set; } = -1;

        public SiteRewardFile[] Rewards { get; set; } = null;
    }
}