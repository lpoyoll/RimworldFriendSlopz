namespace Shared.Files.Configs
{
    public class FL_StorytellerConfig : FL_Base
    {
        public static string SavePath { get; set; } = string.Empty;

        public bool IsEnforced { get; set; } = false;

        public string DefName { get; set; } = string.Empty;
    }
}