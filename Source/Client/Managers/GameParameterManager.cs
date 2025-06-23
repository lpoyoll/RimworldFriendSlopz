using System.Linq;
using System.Reflection;
using GameClient.Misc;
using GameClient.TCP;
using GameClient.Values;
using RimWorld;
using Shared;
using Verse;
using static Shared.CommonEnumerators;

namespace GameClient.Managers
{

    public static class GameParameterManager
    {
        public static void SetValues(ServerGlobalData data)
        {
            SessionValues.ScenarioFile = data._scenarioValues;
            SessionValues.StorytellerFile = data._storytellerValues;
            SessionValues.DifficultyFile = data._difficultyValues;
        }

        public static ScenarioValuesFile GetScenario(Page_SelectScenario __instance)
        {
            ScenarioValuesFile file = new ScenarioValuesFile();

            file.ScenarioName = GameParameterManagerH.GetScenarioReference(__instance).name;

            return file;
        }

        public static void SetScenario(ScenarioValuesFile file)
        {
            if (!file.EnforceScenario) return;
            else Current.Game.Scenario = ScenarioLister.AllScenarios().First(fetch => fetch.name == file.ScenarioName);
        }

        public static void SendScenario(ScenarioValuesFile file, bool mode)
        {
            file.EnforceScenario = mode;

            GameParameterData data = new GameParameterData();
            data._stepMode = GenStepMode.Scenario;
            data._scenario = file;

            Network.Listener.EnqueuePacket(PacketHeader.GameParameterManager, data);
        }

        public static StorytellerValuesFile GetStoryteller(Page_SelectStoryteller __instance)
        {
            StorytellerValuesFile file = new StorytellerValuesFile();

            file.StorytellerDefname = GameParameterManagerH.GetStorytellerReference(__instance).def.defName;

            return file;
        }

        public static void SetStoryteller(StorytellerValuesFile file, bool bypassCheck = false)
        {
            if (!file.EnforceStoryteller && !bypassCheck) return;
            else
            {
                StorytellerDef storytellerDef = DefDatabase<StorytellerDef>.AllDefs.First(fetch => fetch.defName == file.StorytellerDefname);
                DifficultyDef difficultyDef = Current.Game.storyteller.difficultyDef == null ? DifficultyDefOf.Easy : Current.Game.storyteller.difficultyDef;
                Difficulty difficulty = Current.Game.storyteller.difficulty == null ? new Difficulty(difficultyDef) : Current.Game.storyteller.difficulty;

                Current.Game.storyteller = new Storyteller(storytellerDef, difficultyDef, difficulty);
            }
        }

        public static void SendStoryteller(StorytellerValuesFile file, bool mode)
        {
            file.EnforceStoryteller = mode;

            GameParameterData data = new GameParameterData();
            data._stepMode = GenStepMode.Storyteller;
            data._storyteller = file;

            Network.Listener.EnqueuePacket(PacketHeader.GameParameterManager, data);
        }

        public static DifficultyValuesFile GetDifficulty(Page_SelectStoryteller __instance)
        {
            Difficulty difficulty = GameParameterManagerH.GetDifficultyReference(__instance);

            DifficultyValuesFile file = new DifficultyValuesFile();
            
            file.ScribeData = ScribeManager.DifficultyToString(difficulty);
            
            return file;
        }

        public static void SetDifficulty(DifficultyValuesFile file, bool bypass = false)
        {
            if (!file.EnforceDifficulty && !bypass) return;

            Current.Game.storyteller.difficultyDef = DifficultyDefOf.Rough;
            Current.Game.storyteller.difficulty = ScribeManager.StringToDifficulty(file.ScribeData);
        }

        public static void SendDifficulty(DifficultyValuesFile file, bool mode)
        {
            file.EnforceDifficulty = mode;

            GameParameterData data = new GameParameterData();
            data._stepMode = GenStepMode.Difficulty;
            data._difficulty = file;

            Network.Listener.EnqueuePacket(PacketHeader.GameParameterManager, data);
        }
    }

    public static class GameParameterManagerH
    {
        public static Scenario GetScenarioReference(Page_SelectScenario __instance)
        {
            return (Scenario)typeof(Page_SelectScenario).GetField("curScen", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
        }

        public static Storyteller GetStorytellerReference(Page_SelectStoryteller __instance)
        {
            StorytellerDef toGet = (StorytellerDef)typeof(Page_SelectStoryteller).GetField("storyteller", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(__instance);

            return new Storyteller(toGet, DifficultyDefOf.Rough, new Difficulty(DifficultyDefOf.Rough));
        }

        public static Difficulty GetDifficultyReference(Page_SelectStoryteller __instance)
        {
            Difficulty toGet = (Difficulty)typeof(Page_SelectStoryteller).GetField("difficultyValues", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(__instance);

            return toGet;
        }
    }
}
