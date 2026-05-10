namespace Shared.Files.Sites
{
    public class FL_SiteType 
    {
        public string DefName { get; set; } = string.Empty;

        public int Cost { get; set; } = -1;

        public FL_SiteReward[] Rewards { get; set; } = null;
    }
}