using static Shared.CommonEnumerators;

namespace Shared
{

    public class PlayerGuildData
    {
        public GuildStepMode _stepMode { get; set; }

        public GuildFile _file { get; set; } = new GuildFile();

        public int _dataInt { get; set; }
    }
}
