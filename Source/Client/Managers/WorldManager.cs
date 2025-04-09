using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameClient.Core;
using GameClient.Dialogs;
using GameClient.Misc;
using GameClient.TCP;
using GameClient.Values;
using RimWorld;
using RimWorld.Planet;
using Shared;
using UnityEngine;
using Verse;
using Verse.Profile;
using static Shared.CommonEnumerators;

namespace GameClient.Managers
{

    public static class WorldManager
    {
        public static string tempWorldPath => Path.Combine(Master.appdataTempPath, "World.temp");

        private static IEnumerable<WorldGenStepDef> GenStepsInOrder => from x in DefDatabase<WorldGenStepDef>.AllDefs
                                                                       orderby x.order, x.index
                                                                       select x;

        private static readonly List<Type> stepsToUseIfNotFresh = new List<Type>()
        {
            typeof(WorldGenStep_Components),
            typeof(WorldGenStep_Terrain),
            typeof(WorldGenStep_Factions),
            typeof(WorldGenStep_Features)
        };

        [HandlesPacket(PacketHeader.WorldManager)]
        private static void ParsePacket(byte[] bytes)
        {
            WorldData data = Serializer.ConvertBytesToObject<WorldData>(bytes);

            switch (data._stepMode)
            {
                case WorldStepMode.AskFor:
                    OnAskForWorld();
                    break;

                case WorldStepMode.Sent:
                    WorldManagerReceiver.ReceiveWorld(data);
                    break;
            }
        }

        public static void OnAskForWorld()
        {
            DialogManager.PopWaitDialog();

            ClientValues.ToggleGenerateWorld(true);

            RT_Dialog_Message d1 = new RT_Dialog_Message("MESSAGE", new string[] { "You are the first person joining the server!",
                "Configure the world that everyone will play on" }, delegate { ModManager.OpenModManagerMenu(true); });

            DialogManager.PushNewDialog(d1);
        }

        public static void OnExistingWorld()
        {
            DialogManager.PopWaitDialog();

            DialogManager.PushNewDialog(new Page_SelectScenario());
        }

        public static void SetValuesFromGame(string seedString, float planetCoverage, OverallRainfall rainfall, OverallTemperature temperature, OverallPopulation population, List<FactionDef> factions, float pollution)
        {
            SessionValues.WorldFile = new WorldValuesFile();
            SessionValues.WorldFile.SeedString = seedString;
            SessionValues.WorldFile.PersistentRandomValue = GenText.StableStringHash(seedString);
            SessionValues.WorldFile.PlanetCoverage = planetCoverage;
            SessionValues.WorldFile.Rainfall = (int)rainfall;
            SessionValues.WorldFile.Temperature = (int)temperature;
            SessionValues.WorldFile.Population = (int)population;
            SessionValues.WorldFile.Pollution = pollution;
            SessionValues.WorldFile.NPCFactions = WorldManagerH.GetNPCFactionsFromDef(factions.ToArray());
        }

        public static void GeneratePatchedWorld()
        {
            LongEventHandler.QueueLongEvent(delegate
            {
                Find.GameInitData.ResetWorldRelatedMapInitData();
                Current.Game.World = GenerateWorld();
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    Find.World.renderer.RegenerateAllLayersNow();
                    MemoryUtility.UnloadUnusedUnityAssets();
                    Current.CreatingWorld = null;
                    PostWorldGeneration();
                });
            }, "GeneratingWorld", doAsynchronously: true, null);
        }

        private static World GenerateWorld()
        {
            Rand.PushState(SessionValues.WorldFile.PersistentRandomValue);

            Current.CreatingWorld = new World();
            Current.CreatingWorld.info.seedString = SessionValues.WorldFile.SeedString;
            Current.CreatingWorld.info.persistentRandomValue = SessionValues.WorldFile.PersistentRandomValue;
            Current.CreatingWorld.info.planetCoverage = SessionValues.WorldFile.PlanetCoverage;
            Current.CreatingWorld.info.overallRainfall = (OverallRainfall)SessionValues.WorldFile.Rainfall;
            Current.CreatingWorld.info.overallTemperature = (OverallTemperature)SessionValues.WorldFile.Temperature;
            Current.CreatingWorld.info.overallPopulation = (OverallPopulation)SessionValues.WorldFile.Population;
            Current.CreatingWorld.info.name = NameGenerator.GenerateName(RulePackDefOf.NamerWorld);
            Current.CreatingWorld.info.factions = WorldManagerH.GetFactionDefsFromNPCFaction(SessionValues.WorldFile.NPCFactions);
            Current.CreatingWorld.info.pollution = SessionValues.WorldFile.Pollution;

            WorldGenStepDef[] worldGenSteps = GenStepsInOrder.ToArray();
            for (int i = 0; i < worldGenSteps.Count(); i++)
            {
                WorldGenStep toGenerate = worldGenSteps[i].worldGenStep;

                if (ClientValues.IsGeneratingFreshWorld || stepsToUseIfNotFresh.Contains(toGenerate.GetType()))
                {
                    toGenerate.GenerateFresh(SessionValues.WorldFile.SeedString);
                }
                else continue;
            }

            if (!ClientValues.IsGeneratingFreshWorld && SessionValues.WorldFile.Tiles != null && SessionValues.WorldFile.Tiles.Length > 0)
            {
                Current.CreatingWorld.grid.tiles = new List<Tile>();
                foreach (string str in SessionValues.WorldFile.Tiles) Current.CreatingWorld.grid.tiles.Add(ScribeManager.ScribeToTile(str));
            }

            Current.CreatingWorld.grid.StandardizeTileData();
            Current.CreatingWorld.FinalizeInit();
            Find.Scenario.PostWorldGenerate();

            if (!ModsConfig.IdeologyActive) Find.Scenario.PostIdeoChosen();
            return Current.CreatingWorld;
        }

        public static void PostWorldGeneration()
        {
            Page_SelectStartingSite newSelectStartingSite = new Page_SelectStartingSite();
            Page_ConfigureStartingPawns newConfigureStartingPawns = new Page_ConfigureStartingPawns();
            newConfigureStartingPawns.nextAct = PageUtility.InitGameStart;

            if (ModsConfig.IdeologyActive)
            {
                Page_ChooseIdeoPreset newChooseIdeoPreset = new Page_ChooseIdeoPreset();
                newChooseIdeoPreset.prev = newSelectStartingSite;
                newChooseIdeoPreset.next = newConfigureStartingPawns;

                newSelectStartingSite.next = newChooseIdeoPreset;
            }

            else
            {
                newSelectStartingSite.next = newConfigureStartingPawns;
                newConfigureStartingPawns.prev = newSelectStartingSite;
            }

            Find.WindowStack.Add(newSelectStartingSite);
        }

        public static void SetPlanetFeatures()
        {
            WorldFeature[] worldFeatures = Find.WorldFeatures.features.ToArray();
            foreach (WorldFeature feature in worldFeatures) Find.WorldFeatures.features.Remove(feature);

            for (int i = 0; i < SessionValues.WorldFile.Features.Length; i++)
            {
                PlanetFeatureDetails planetFeature = SessionValues.WorldFile.Features[i];

                try
                {
                    WorldFeature worldFeature = new WorldFeature();
                    worldFeature.def = DefDatabase<FeatureDef>.AllDefs.First(fetch => fetch.defName == planetFeature.defName);
                    worldFeature.uniqueID = i;
                    worldFeature.name = planetFeature.name;
                    worldFeature.maxDrawSizeInTiles = planetFeature.maxDrawSizeInTiles;
                    worldFeature.drawCenter = new Vector3(planetFeature.drawCenter[0], planetFeature.drawCenter[1], planetFeature.drawCenter[2]);

                    Find.WorldFeatures.features.Add(worldFeature);
                }
                catch (Exception e) { Printer.Warning($"Failed set planet feature from def '{planetFeature.defName}'. Reason: {e}"); }
            }

            Find.WorldFeatures.textsCreated = false;
            Find.WorldFeatures.UpdateFeatures();
        }

        public static void SetPlanetFactions()
        {
            Faction[] planetFactions = Find.World.factionManager.AllFactions.ToArray();

            for (int i = 0; i < SessionValues.WorldFile.NPCFactions.Length; i++)
            {
                try
                {
                    PlanetNPCFactionDetails faction = SessionValues.WorldFile.NPCFactions[i];

                    Faction toModify = planetFactions.First(fetch => fetch.def.defName == SessionValues.WorldFile.NPCFactions[i].defName);

                    toModify.Name = faction.name;

                    toModify.color = new Color(faction.color[0],
                        faction.color[1],
                        faction.color[2],
                        faction.color[3]);
                }
                catch (Exception e) { Printer.Warning($"Failed set planet faction from def '{SessionValues.WorldFile.NPCFactions[i].defName}'. Reason: {e}"); }
            }
        }
    }

    public static class WorldManagerH
    {
        public static void PopulateWorldValues()
        {
            SessionValues.WorldFile.Tiles = GetPlanetTiles();
            SessionValues.WorldFile.Features = GetPlanetFeatures();
            SessionValues.WorldFile.Roads = RoadManagerHelper.GetPlanetRoads();
            SessionValues.WorldFile.Rivers = RiverManagerHelper.GetPlanetRivers();
            SessionValues.WorldFile.PollutedTiles = PollutionManagerHelper.GetPlanetPollutedTiles();
            SessionValues.WorldFile.NPCSettlements = GetPlanetNPCSettlements();
            SessionValues.WorldFile.NPCFactions = GetPlanetNPCFactions();
        }

        public static string[] GetPlanetTiles()
        {
            List<string> toGet = new List<string>();
            foreach (Tile tile in Find.WorldGrid.tiles) toGet.Add(ScribeManager.TileToScribe(tile));
            return toGet.ToArray();
        }

        public static PlanetNPCFactionDetails[] GetNPCFactionsFromDef(FactionDef[] factionDefs)
        {
            List<PlanetNPCFactionDetails> npcFactions = new List<PlanetNPCFactionDetails>();
            foreach (FactionDef faction in factionDefs)
            {
                try
                {
                    PlanetNPCFactionDetails toCreate = new PlanetNPCFactionDetails();
                    toCreate.defName = faction.defName;
                    npcFactions.Add(toCreate);
                }
                catch (Exception e) { Printer.Warning($"Failed to get faction '{faction.defName}' from game. Reason: {e}"); }
            }
            return npcFactions.ToArray();
        }

        public static List<FactionDef> GetFactionDefsFromNPCFaction(PlanetNPCFactionDetails[] factions)
        {
            List<FactionDef> defList = new List<FactionDef>();
            List<PlanetNPCFactionDetails> serverFactions = factions.ToList();
            foreach (PlanetNPCFactionDetails faction in factions)
            {
                FactionDef newFaction = DefDatabase<FactionDef>.GetNamedSilentFail(faction.defName);
                if (newFaction == null)
                {
                    Printer.Warning($"Failed to get FactionDef '{faction.defName}' from server.", LogImportanceMode.Verbose);

                    switch (faction.defName)
                    {
                        case "OutlanderRoughPig":
                            newFaction = FactionDefOf.OutlanderRough;
                            defList.Add(newFaction);
                            serverFactions.Add(new PlanetNPCFactionDetails() { defName = FactionDefOf.OutlanderRough.defName, color = faction.color, name = faction.name });
                            break;

                        case "PirateYttakin":
                            newFaction = FactionDefOf.Pirate;
                            defList.Add(newFaction);
                            serverFactions.Add(new PlanetNPCFactionDetails() { defName = FactionDefOf.Pirate.defName, color = faction.color, name = faction.name });
                            break;

                        case "PirateWaster":
                            newFaction = FactionDefOf.Pirate;
                            defList.Add(newFaction);
                            serverFactions.Add(new PlanetNPCFactionDetails() { defName = FactionDefOf.Pirate.defName, color = faction.color, name = faction.name });
                            break;

                        case "TribeRoughNeanderthal":
                            newFaction = FactionDefOf.TribeRough;
                            defList.Add(newFaction);
                            serverFactions.Add(new PlanetNPCFactionDetails() { defName = FactionDefOf.TribeRough.defName, color = faction.color, name = faction.name });
                            break;

                        case "TribeSavageImpid":
                            newFaction = FactionDefOf.TribeRough;
                            defList.Add(newFaction);
                            serverFactions.Add(new PlanetNPCFactionDetails() { defName = FactionDefOf.TribeRough.defName, color = faction.color, name = faction.name });
                            break;

                        case "TribeCannibal":
                            newFaction = FactionDefOf.TribeRough;
                            defList.Add(newFaction);
                            serverFactions.Add(new PlanetNPCFactionDetails() { defName = FactionDefOf.TribeRough.defName, color = faction.color, name = faction.name });
                            break;

                        case "Empire":
                            newFaction = FactionDefOf.OutlanderCivil;
                            defList.Add(newFaction);
                            serverFactions.Add(new PlanetNPCFactionDetails() { defName = FactionDefOf.OutlanderCivil.defName, color = faction.color, name = faction.name });
                            break;

                        default:
                            break;
                    }

                    if (newFaction != null) Printer.Warning($"Replaced {faction.defName} with {newFaction.defName}", LogImportanceMode.Verbose);
                    serverFactions.Remove(faction);
                }

                else
                {
                    defList.Add(newFaction);
                    Printer.Warning($"Loaded {newFaction.defName}", LogImportanceMode.Verbose);
                }

                SessionValues.WorldFile.NPCFactions = serverFactions.ToArray();
            }

            return defList;
        }

        public static PlanetNPCFactionDetails[] GetPlanetNPCFactions()
        {
            List<PlanetNPCFactionDetails> planetFactions = new List<PlanetNPCFactionDetails>();
            Faction[] existingFactions = Find.World.factionManager.AllFactions.ToArray();

            foreach (Faction faction in existingFactions)
            {
                try
                {
                    if (faction == Faction.OfPlayer) continue;
                    else
                    {
                        PlanetNPCFactionDetails planetFaction = new PlanetNPCFactionDetails();
                        planetFaction.defName = faction.def.defName;
                        planetFaction.name = faction.Name;
                        planetFaction.color = new float[] { faction.Color.r, faction.Color.g, faction.Color.b, faction.Color.a };

                        planetFactions.Add(planetFaction);
                    }
                }
                catch (Exception e) { Printer.Warning($"Failed to get NPC faction '{faction.def.defName}' to populate. Reason: {e}"); }
            }

            return planetFactions.ToArray();
        }

        public static PlanetNPCSettlementDetails[] GetPlanetNPCSettlements()
        {
            Faction[] worldNPCFactions = Find.FactionManager.AllFactions.Where(fetch => !ClientValues.playerFactions.Contains(fetch) &&
                fetch != Faction.OfPlayer).ToArray();

            List<FactionDef> worldNPCFactionDefs = new List<FactionDef>();
            foreach (Faction faction in worldNPCFactions) worldNPCFactionDefs.Add(faction.def);

            List<PlanetNPCSettlementDetails> npcSettlements = new List<PlanetNPCSettlementDetails>();
            foreach (Settlement settlement in Find.World.worldObjects.Settlements.Where(fetch => worldNPCFactionDefs.Contains(fetch.Faction.def)))
            {
                try
                {
                    PlanetNPCSettlementDetails PlanetNPCSettlementDetails = new PlanetNPCSettlementDetails();
                    PlanetNPCSettlementDetails.tile = settlement.Tile;
                    PlanetNPCSettlementDetails.defName = settlement.Faction.def.defName;
                    PlanetNPCSettlementDetails.name = settlement.Name;
                    PlanetNPCSettlementDetails.factionName = settlement.Faction.Name;
                    npcSettlements.Add(PlanetNPCSettlementDetails);
                }
                catch (Exception e) { Printer.Warning($"Failed to get NPC settlement '{settlement.Tile}' to populate. Reason: {e}"); }
            }
            return npcSettlements.ToArray();
        }

        public static PlanetFeatureDetails[] GetPlanetFeatures()
        {
            List<PlanetFeatureDetails> planetFeatures = new List<PlanetFeatureDetails>();
            WorldFeature[] worldFeatures = Find.World.features.features.ToArray();
            foreach (WorldFeature worldFeature in worldFeatures)
            {
                try
                {
                    PlanetFeatureDetails planetFeature = new PlanetFeatureDetails();
                    planetFeature.name = worldFeature.name;
                    planetFeature.defName = worldFeature.def.defName;
                    planetFeature.maxDrawSizeInTiles = worldFeature.maxDrawSizeInTiles;
                    planetFeature.drawCenter = new float[] { worldFeature.drawCenter.x, worldFeature.drawCenter.y, worldFeature.drawCenter.z };

                    planetFeatures.Add(planetFeature);
                }
                catch (Exception e) { Printer.Warning($"Failed to get feature '{worldFeature.def.defName}' to populate. Reason: {e}"); }
            }

            return planetFeatures.ToArray();
        }
    }

    public static class WorldManagerSender
    {
        public static void SendWorld()
        {
            WorldManagerH.PopulateWorldValues();

            WorldData data = new WorldData();
            data._stepMode = WorldStepMode.Sent;
            data._fileBytes = Serializer.ConvertObjectToBytes(SessionValues.WorldFile);

            Network.listener.EnqueuePacket(PacketHeader.WorldManager, data);

            OnWorldSent();
        }

        private static void OnWorldSent()
        {
            File.Delete(WorldManager.tempWorldPath);

            ClientValues.ToggleGenerateWorld(false);

            SaveManager.ForceSave();
        }
    }

    public static class WorldManagerReceiver
    {
        public static void ReceiveWorld(WorldData data)
        {
            SessionValues.WorldFile = Serializer.ConvertBytesToObject<WorldValuesFile>(data._fileBytes);

            WorldManager.OnExistingWorld();
        }
    }
}
