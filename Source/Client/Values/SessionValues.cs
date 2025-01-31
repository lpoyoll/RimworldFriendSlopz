using System.Collections.Generic;
using GameClient.Patches.Pages;
using RimWorld;
using RimWorld.Planet;
using Shared;
using Verse;
using static Shared.CommonEnumerators;

namespace GameClient.Values
{
    public static class SessionValues
    {
        public static OnlineActivityType currentRealTimeActivity;

        public static OfflineActivityType latestOfflineActivity;

        public static bool isActivityHost;

        public static bool isActivityReady;

        public static Settlement chosenSettlement;

        public static Caravan chosenCaravan;

        public static Site chosenSite;

        public static CompLaunchable chosendPods;

        public static Pawn chosenPawnForSpying;

        public static TransferData outgoingManifest = new TransferData();

        public static TransferData incomingManifest = new TransferData();

        public static List<Tradeable> listToShowInTradesMenu = new List<Tradeable>();

        public static ActionValuesFile actionValues;

        public static ModConfigFile configFile;

        public static ScenarioValuesFile scenarioFile;

        public static StorytellerValuesFile storytellerFile;

        public static DifficultyValuesFile difficultyFile;

        public static WorldValuesFile worldFile;

        public static void SetValues(ServerGlobalData serverGlobalData)
        {
            actionValues = serverGlobalData._actionValues;
        }

        public static void ToggleOnlineActivity(OnlineActivityType type) { currentRealTimeActivity = type; }

        public static void ToggleOfflineActivity(OfflineActivityType type) { latestOfflineActivity = type; }

        public static void ToggleOnlineActivityHost(bool type) { isActivityHost = type; }

        public static void ToggleOnlineActivityReady(bool type) { isActivityReady = type; }

        public static void CleanValues()
        {
            ToggleOnlineActivity(OnlineActivityType.None);
            ToggleOfflineActivity(OfflineActivityType.None);
            ToggleOnlineActivityHost(false);
            ToggleOnlineActivityReady(false);

            chosenSettlement = null;
            chosenCaravan = null;
            chosenSite = null;

            outgoingManifest = new TransferData();
            incomingManifest = new TransferData();
            listToShowInTradesMenu = new List<Tradeable>();

            PatchSelectScenarioPage.executedMessage = false;
            PreventModOptionsButton.executedMessage = false;
            PatchSelectStorytellerPage.executedMessage = false;
        }
    }
}