using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GameClient.Core;
using GameClient.Dialogs;
using GameClient.Misc;
using GameClient.TCP;
using GameClient.Values;
using RimWorld;
using Shared;
using Verse;
using static Shared.CommonEnumerators;

namespace GameClient.Managers
{
    [RTManager]
    public static class GameParameterManager
    {
        public static void SetValues(ServerGlobalData data)
        {
            SessionValues.scenarioFile = data._scenarioValues;
            SessionValues.storytellerFile = data._storytellerValues;
            SessionValues.difficultyFile = data._difficultyValues;
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

            Packet packet = Packet.CreatePacketFromObject(nameof(GameParameterManager), data);
            Network.listener.EnqueuePacket(packet);
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

            Packet packet = Packet.CreatePacketFromObject(nameof(GameParameterManager), data);
            Network.listener.EnqueuePacket(packet);
        }

        public static DifficultyValuesFile GetDifficulty(Page_SelectStoryteller __instance)
        {
            Difficulty difficulty = GameParameterManagerH.GetDifficultyReference(__instance);

            DifficultyValuesFile file = new DifficultyValuesFile();

            file.ThreatScale = difficulty.threatScale;

            file.AllowBigThreats = difficulty.allowBigThreats;

            file.AllowViolentQuests = difficulty.allowViolentQuests;

            file.AllowIntroThreats = difficulty.allowIntroThreats;

            file.PredatorsHuntHumanlikes = difficulty.predatorsHuntHumanlikes;

            file.AllowExtremeWeatherIncidents = difficulty.allowExtremeWeatherIncidents;

            file.CropYieldFactor = difficulty.cropYieldFactor;

            file.MineYieldFactor = difficulty.mineYieldFactor;

            file.ButcherYieldFactor = difficulty.butcherYieldFactor;

            file.ResearchSpeedFactor = difficulty.researchSpeedFactor;

            file.QuestRewardValueFactor = difficulty.questRewardValueFactor;

            file.RaidLootPointsFactor = difficulty.raidLootPointsFactor;

            file.TradePriceFactorLoss = difficulty.tradePriceFactorLoss;

            file.MaintenanceCostFactor = difficulty.maintenanceCostFactor;

            file.ScariaRotChance = difficulty.scariaRotChance;

            file.EnemyDeathOnDownedChanceFactor = difficulty.enemyDeathOnDownedChanceFactor;

            file.ColonistMoodOffset = difficulty.colonistMoodOffset;

            file.FoodPoisonChanceFactor = difficulty.foodPoisonChanceFactor;

            file.ManhunterChanceOnDamageFactor = difficulty.manhunterChanceOnDamageFactor;

            file.PlayerPawnInfectionChanceFactor = difficulty.playerPawnInfectionChanceFactor;

            file.DiseaseIntervalFactor = difficulty.diseaseIntervalFactor;

            file.EnemyReproductionRateFactor = difficulty.enemyReproductionRateFactor;

            file.DeepDrillInfestationChanceFactor = difficulty.deepDrillInfestationChanceFactor;

            file.FriendlyFireChanceFactor = difficulty.friendlyFireChanceFactor;

            file.AllowInstantKillChance = difficulty.allowInstantKillChance;

            file.PeacefulTemples = difficulty.peacefulTemples;

            file.AllowCaveHives = difficulty.allowCaveHives;

            file.UnwaveringPrisoners = difficulty.unwaveringPrisoners;

            file.AllowTraps = difficulty.allowTraps;

            file.AllowTurrets = difficulty.allowTurrets;

            file.AllowMortars = difficulty.allowMortars;

            file.ClassicMortars = difficulty.classicMortars;

            file.AdaptationEffectFactor = difficulty.adaptationEffectFactor;

            file.AdaptationGrowthRateFactorOverZero = difficulty.adaptationGrowthRateFactorOverZero;

            file.FixedWealthMode = difficulty.fixedWealthMode;

            file.LowPopConversionBoost = difficulty.lowPopConversionBoost;

            file.NoBabiesOrChildren = difficulty.noBabiesOrChildren;

            file.BabiesAreHealthy = difficulty.babiesAreHealthy;

            file.ChildRaidersAllowed = difficulty.childRaidersAllowed;

            file.ChildAgingRate = difficulty.childAgingRate;

            file.AdultAgingRate = difficulty.adultAgingRate;

            file.WastepackInfestationChanceFactor = difficulty.wastepackInfestationChanceFactor;

            return file;
        }

        public static void SetDifficulty(DifficultyValuesFile file, bool bypass = false)
        {
            if (!file.EnforceDifficulty && !bypass) return;
            else
            {
                Current.Game.storyteller.difficultyDef = DifficultyDefOf.Rough;

                Current.Game.storyteller.difficulty = new Difficulty(Current.Game.storyteller.difficultyDef);

                Current.Game.storyteller.difficulty.threatScale = file.ThreatScale;

                Current.Game.storyteller.difficulty.allowBigThreats = file.AllowBigThreats;

                Current.Game.storyteller.difficulty.allowViolentQuests = file.AllowViolentQuests;

                Current.Game.storyteller.difficulty.allowIntroThreats = file.AllowIntroThreats;

                Current.Game.storyteller.difficulty.predatorsHuntHumanlikes = file.PredatorsHuntHumanlikes;

                Current.Game.storyteller.difficulty.allowExtremeWeatherIncidents = file.AllowExtremeWeatherIncidents;

                Current.Game.storyteller.difficulty.cropYieldFactor = file.CropYieldFactor;

                Current.Game.storyteller.difficulty.mineYieldFactor = file.MineYieldFactor;

                Current.Game.storyteller.difficulty.butcherYieldFactor = file.ButcherYieldFactor;

                Current.Game.storyteller.difficulty.researchSpeedFactor = file.ResearchSpeedFactor;

                Current.Game.storyteller.difficulty.questRewardValueFactor = file.QuestRewardValueFactor;

                Current.Game.storyteller.difficulty.raidLootPointsFactor = file.RaidLootPointsFactor;

                Current.Game.storyteller.difficulty.tradePriceFactorLoss = file.TradePriceFactorLoss;

                Current.Game.storyteller.difficulty.maintenanceCostFactor = file.MaintenanceCostFactor;

                Current.Game.storyteller.difficulty.scariaRotChance = file.ScariaRotChance;

                Current.Game.storyteller.difficulty.enemyDeathOnDownedChanceFactor = file.EnemyDeathOnDownedChanceFactor;

                Current.Game.storyteller.difficulty.colonistMoodOffset = file.ColonistMoodOffset;

                Current.Game.storyteller.difficulty.foodPoisonChanceFactor = file.FoodPoisonChanceFactor;

                Current.Game.storyteller.difficulty.manhunterChanceOnDamageFactor = file.ManhunterChanceOnDamageFactor;

                Current.Game.storyteller.difficulty.playerPawnInfectionChanceFactor = file.PlayerPawnInfectionChanceFactor;

                Current.Game.storyteller.difficulty.diseaseIntervalFactor = file.DiseaseIntervalFactor;

                Current.Game.storyteller.difficulty.enemyReproductionRateFactor = file.EnemyReproductionRateFactor;

                Current.Game.storyteller.difficulty.deepDrillInfestationChanceFactor = file.DeepDrillInfestationChanceFactor;

                Current.Game.storyteller.difficulty.friendlyFireChanceFactor = file.FriendlyFireChanceFactor;

                Current.Game.storyteller.difficulty.allowInstantKillChance = file.AllowInstantKillChance;

                Current.Game.storyteller.difficulty.peacefulTemples = file.PeacefulTemples;

                Current.Game.storyteller.difficulty.allowCaveHives = file.AllowCaveHives;

                Current.Game.storyteller.difficulty.unwaveringPrisoners = file.UnwaveringPrisoners;

                Current.Game.storyteller.difficulty.allowTraps = file.AllowTraps;

                Current.Game.storyteller.difficulty.allowTurrets = file.AllowTurrets;

                Current.Game.storyteller.difficulty.allowMortars = file.AllowMortars;

                Current.Game.storyteller.difficulty.classicMortars = file.ClassicMortars;

                Current.Game.storyteller.difficulty.adaptationEffectFactor = file.AdaptationEffectFactor;

                Current.Game.storyteller.difficulty.adaptationGrowthRateFactorOverZero = file.AdaptationGrowthRateFactorOverZero;

                Current.Game.storyteller.difficulty.fixedWealthMode = file.FixedWealthMode;

                Current.Game.storyteller.difficulty.lowPopConversionBoost = file.LowPopConversionBoost;

                Current.Game.storyteller.difficulty.noBabiesOrChildren = file.NoBabiesOrChildren;

                Current.Game.storyteller.difficulty.babiesAreHealthy = file.BabiesAreHealthy;

                Current.Game.storyteller.difficulty.childRaidersAllowed = file.ChildRaidersAllowed;

                Current.Game.storyteller.difficulty.childAgingRate = file.ChildAgingRate;

                Current.Game.storyteller.difficulty.adultAgingRate = file.AdultAgingRate;

                Current.Game.storyteller.difficulty.wastepackInfestationChanceFactor = file.WastepackInfestationChanceFactor;
            }
        }

        public static void SendDifficulty(DifficultyValuesFile file, bool mode)
        {
            file.EnforceDifficulty = mode;

            GameParameterData data = new GameParameterData();
            data._stepMode = GenStepMode.Difficulty;
            data._difficulty = file;

            Packet packet = Packet.CreatePacketFromObject(nameof(GameParameterManager), data);
            Network.listener.EnqueuePacket(packet);
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
