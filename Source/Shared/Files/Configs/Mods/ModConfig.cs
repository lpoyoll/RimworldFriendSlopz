using static Shared.Files.Configs.Mods.ModConfigFile;

namespace Shared.Files.Configs.Mods
{
    public class ModConfig
    {
        public string FileName { get; set; } = string.Empty;

        public string ConfigString { get; set; } = string.Empty;

        public ModType Type { get; set; } = ModType.Required;
    }
}
