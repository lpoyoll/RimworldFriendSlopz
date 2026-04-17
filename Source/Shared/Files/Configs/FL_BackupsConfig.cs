namespace Shared.Files.Configs
{
    public class FL_BackupsConfig : FL_Base
    {
        public static string SavePath { get; set; } = string.Empty;

        public bool AutomaticBackups { get; set; } = true;

        public float IntervalHours { get; set; } = 24f;

        public bool AutomaticDeletion { get; set; } = true;

        public int Amount { get; set; } = 3;
    }
}
