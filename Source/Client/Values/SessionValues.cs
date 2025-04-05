using System.Collections.Generic;
using GameClient.Patches.Pages;
using RimWorld;
using RimWorld.Planet;
using Shared;
using static Shared.CommonEnumerators;

namespace GameClient.Values
{
    public static class SessionValues
    {
        public static ActivityType latestActivity = ActivityType.None;

        public static bool IsActivityHost = false;

        public static bool IsActivityReady = false;

        public static Settlement ChosenSettlement = null;

        public static Caravan ChosenCaravan = null;

        public static Site ChosenSite = null;

        public static CompLaunchable ChosendPods = null;

        public static TransferData OutgoingManifest = new TransferData();

        public static TransferData IncomingManifest = new TransferData();

        public static List<Tradeable> ListToShowInTradesMenu = new List<Tradeable>();

        public static ActionValuesFile ActionValues = null;

        public static ModConfigFile ConfigFile = null;

        public static ScenarioValuesFile ScenarioFile = null;

        public static StorytellerValuesFile StorytellerFile = null;

        public static DifficultyValuesFile DifficultyFile = null;

        public static WorldValuesFile WorldFile = null;

        public static void SetValues(ServerGlobalData serverGlobalData)
        {
            ActionValues = serverGlobalData._actionValues;
        }

        public static void ToggleActivity(ActivityType type) { latestActivity = type; }

        public static void CleanValues()
        {
            ToggleActivity(ActivityType.None);

            ChosenSettlement = null;
            ChosenCaravan = null;
            ChosenSite = null;

            OutgoingManifest = new TransferData();
            IncomingManifest = new TransferData();
            ListToShowInTradesMenu = new List<Tradeable>();

            PatchSelectScenarioPage.executedMessage = false;
            PreventModOptionsButton.executedMessage = false;
            PatchSelectStorytellerPage.executedMessage = false;
        }
    }
}