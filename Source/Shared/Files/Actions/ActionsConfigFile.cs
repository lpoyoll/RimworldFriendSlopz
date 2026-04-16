namespace Shared.Files.Actions
{
    public class ActionsConfigFile : BaseFile
    {
        public static string SavePath { get; set; } = string.Empty;

        public bool EnableFactions { get; set; } = true;

        public bool EnableLeaderboard { get; set; } = true;

        public bool EnableTrading { get; set; } = true;

        public bool EnableCustomScenarios { get; set; } = true;

        public ACT_NPC NPCAction { get; set; } = new ACT_NPC();

        public ACT_Pollution PollutionAction { get; set; } = new ACT_Pollution();

        public ACT_Activity ActivityAction { get; set; } = new ACT_Activity();

        public ACT_Event EventAction { get; set; } = new ACT_Event();

        public ACT_Aid AidAction { get; set; } = new ACT_Aid();

        public ACT_Roads RoadsAction { get; set; } = new ACT_Roads();

        public ACT_Site SiteAction { get; set; } = new ACT_Site();
    }
}
