namespace Shared.Files.Configs
{
    public class FL_ScenarioConfig : FL_Base
    {
        public static string SavePath { get; set; } = string.Empty;

        public bool IsEnforced { get; set; } = false;

        public string Name { get; set; } = string.Empty;
    }
}