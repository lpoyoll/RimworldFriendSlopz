using static Shared.CommonEnumerators;

namespace TCPNetwork.Files.Client
{
    public class PlayerGoodwill
    {
        public string Name { get; set; } = string.Empty;

        public Goodwill Goodwill { get; set; } = Goodwill.Neutral;
    }
}
