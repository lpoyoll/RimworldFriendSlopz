using static Shared.CommonEnumerators;

namespace Shared.Files.ServerClient
{
    public class FL_PlayerGoodwill
    {
        public string Name { get; set; } = string.Empty;

        public Goodwill Goodwill { get; set; } = Goodwill.Neutral;
    }
}
