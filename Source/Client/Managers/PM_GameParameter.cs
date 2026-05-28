using GameClient.Dialogs;
using GameClient.Misc;
using RimWorld;
using RTShared;
using System.IO;
using System.Linq;
using Verse;
using RTNetwork.Packets;
using RTShared.Files.Configs;
using RTNetwork;
using GameClient.PacketManagers;
using static RTNetwork.Packets.GameParameterData;
using static RTNetwork.Packets.PKT_ModConfig;
using GameClient.Dialogs.Default;
using RTNetwork.PacketManagers;

namespace GameClient.Managers
{
    public class PM_GameParameter : PM_Base
    {
        [HandlesPacket(PacketHeader.GameParameter)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            throw new System.NotImplementedException();
        }

        public static void SetFirstTimeSetup()
        {
            string title = "Server Enforcements";
            string description = "Chose what features to enforce";
            string[] keys = new string[] { "Scenario", "Storyteller", "Difficulty" };
            string[] values = new string[] { "Enforced", "Free" };

            DLG_Base.PushNewDialog(new DLG_ListingWithTuple(title, description, keys, values, null,
                PM_GameParameter.SendFirstTimeSetup));
        }

        public static void SetValues()
        {
            SessionHandler.CurrentScenario = SessionHandler.GlobalData.ScenarioValues;
            SessionHandler.CurrentStoryteller = SessionHandler.GlobalData.StorytellerValues;
            SessionHandler.CurrentDifficulty = SessionHandler.GlobalData.DifficultyValues;
        }

        public static void SetScenario(FL_ScenarioConfig file)
        {
            if (!file.IsEnforced) return;
            else
            {
                Scenario toFind = ScenarioLister.AllScenarios().FirstOrDefault(fetch => fetch.name == file.Name);
                if (toFind != null) Current.Game.Scenario = toFind;
                else Current.Game.Scenario = ScenarioLister.AllScenarios().ToArray()[0];
            }
        }

        public static void SetDifficulty(FL_DifficultyConfig file, bool bypass = false)
        {
            if (!file.IsEnforced && !bypass) return;
            else
            {
                Current.Game.storyteller.difficultyDef = DifficultyDefOf.Rough;
                Current.Game.storyteller.difficulty = (Difficulty)ScribeManager.SerializeFromString<Difficulty>(file.ScribeData);
            }
        }

        public static void SetStoryteller(FL_StorytellerConfig file, bool bypassCheck = false)
        {
            if (!file.IsEnforced && !bypassCheck) return;
            else
            {
                StorytellerDef storytellerDef = DefDatabase<StorytellerDef>.AllDefs.First(fetch => fetch.defName == file.DefName);
                DifficultyDef difficultyDef = Current.Game.storyteller.difficultyDef == null ? DifficultyDefOf.Easy : Current.Game.storyteller.difficultyDef;
                Difficulty difficulty = Current.Game.storyteller.difficulty == null ? new Difficulty(difficultyDef) : Current.Game.storyteller.difficulty;

                Current.Game.storyteller = new Storyteller(storytellerDef, difficultyDef, difficulty);
            }
        }

        public static void SendCurrentScenario(bool isEnforced)
        {
            FL_ScenarioConfig file = new FL_ScenarioConfig();
            file.Name = Current.Game.Scenario.name;
            file.IsEnforced = isEnforced;

            GameParameterData data = new GameParameterData();
            data._stepMode = GenStepMode.Scenario;
            data._bytes = Serializer.ConvertObjectToBytes(file);

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.GameParameter, data);
        }

        public static void SendCurrentStoryteller(bool isEnforced)
        {
            FL_StorytellerConfig file = new FL_StorytellerConfig();
            file.DefName = Current.Game.storyteller.def.defName;
            file.IsEnforced = isEnforced;

            GameParameterData data = new GameParameterData();
            data._stepMode = GenStepMode.Storyteller;
            data._bytes = Serializer.ConvertObjectToBytes(file);

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.GameParameter, data);
        }

        public static void SendCurrentDifficulty(bool isEnforced)
        {
            FL_DifficultyConfig file = new FL_DifficultyConfig();
            file.IsEnforced = isEnforced;
            file.ScribeData = ScribeManager.SerializeToString(Current.Game.storyteller.difficulty, 
                ScribeManager.SerializableType.Other);

            GameParameterData data = new GameParameterData();
            data._stepMode = GenStepMode.Difficulty;
            data._bytes = Serializer.ConvertObjectToBytes(file);

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.GameParameter, data);
        }

        public static void SendCurrentModConfigs(bool isEnforced)
        {
            PKT_ModConfig data = new PKT_ModConfig();
            data._stepMode = ModConfigStepMode.Send;
            data._configFile = ModManagerH.SortModsIntoCategories(DLG_ModConfig.ResultMods, DLG_ModConfig.ResultInt);

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Mod, data);
        }

        private static void SendFirstTimeSetup()
        {
            if (DLG_ListingWithTuple.DialogTupleListingResultInt[0] == 0) { PM_GameParameter.SendCurrentScenario(true); }
            if (DLG_ListingWithTuple.DialogTupleListingResultInt[1] == 0) { PM_GameParameter.SendCurrentStoryteller(true); }
            if (DLG_ListingWithTuple.DialogTupleListingResultInt[2] == 0) { PM_GameParameter.SendCurrentDifficulty(true); }

            PM_World.SendWorld();
            PM_Events.SendExistingEventsToServer();
        }
    }
}
