using static Shared.CommonEnumerators;

namespace Shared.Files.ServerClient
{
    public class PlayerGoodwill
    {
        public string Name { get; set; } = string.Empty;

        public Goodwill Goodwill { get; set; } = Goodwill.Neutral;
    }
}
