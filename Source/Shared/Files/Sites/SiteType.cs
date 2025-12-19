namespace Shared.Files.Sites
{
    public class SiteType 
    {
        public string DefName { get; set; } = string.Empty;

        public int Cost { get; set; } = -1;

        public SiteReward[] Rewards { get; set; } = null;
    }
}