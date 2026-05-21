namespace Shared.Files.Actions
{
    public class FL_ActionsConfig : FL_Base
    {
        public static string SavePath { get; set; } = string.Empty;

        public bool EnableFactions { get; set; } = true;

        public bool EnableLeaderboard { get; set; } = true;

        public bool EnableTrading { get; set; } = true;

        public bool EnableCustomScenarios { get; set; } = true;

        public ACT_NPC NPCAction { get; set; } = new ACT_NPC();

        public ACT_Pollution PollutionAction { get; set; } = new ACT_Pollution();

        public ACT_Raid RaidAction { get; set; } = new ACT_Raid();

        public ACT_Zoom ZoomAction { get; set; } = new ACT_Zoom();

        public ACT_Event EventAction { get; set; } = new ACT_Event();

        public ACT_Aid AidAction { get; set; } = new ACT_Aid();

        public ACT_Road RoadAction { get; set; } = new ACT_Road();

        public ACT_Site SiteAction { get; set; } = new ACT_Site();
    }
}
