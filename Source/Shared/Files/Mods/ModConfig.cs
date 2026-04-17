using static Shared.Files.Configs.FL_ModConfig;

namespace Shared.Files.Mods
{
    public class ModConfig
    {
        public string FileName { get; set; } = string.Empty;

        public string ConfigString { get; set; } = string.Empty;

        public ModType Type { get; set; } = ModType.Required;
    }
}
