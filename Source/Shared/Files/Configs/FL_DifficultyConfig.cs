namespace Shared.Files.Configs
{
    public class FL_DifficultyConfig : FL_Base
    {
        public static string SavePath { get; set; } = string.Empty;

        public bool IsEnforced { get; set; } = false;

        public string ScribeData { get; set; } = string.Empty;
    }
}
