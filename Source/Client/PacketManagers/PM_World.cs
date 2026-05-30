using GameClient.Core;
using GameClient.Dialogs;
using RTNetwork.Packets;
using RimWorld;
using RimWorld.Planet;
using RTShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Profile;
using RTShared.Details.Planet;
using RTShared.Files.Configs;
using RTShared.Misc;
using RTNetwork;
using static RTShared.Misc.Printer;
using static RTNetwork.Packets.PKT_World;
using GameClient.Dialogs.Default;
using RTNetwork.PacketManagers;
using RTNetwork.Components;
using GameClient.Managers;

namespace GameClient.PacketManagers
{
    public class PM_World : PM_Base
    {
        public static string tempWorldPath => Path.Combine(Master.AppdataTempPath, "World.temp");

        private static IEnumerable<GameSetupStepDef> SetupStepsInOrder => from x in DefDatabase<GameSetupStepDef>.AllDefs
                                                                          orderby x.order, x.index
                                                                          select x;

        private static IEnumerable<WorldGenStepDef> GenStepsInOrder => from x in DefDatabase<WorldGenStepDef>.AllDefs
                                                                       orderby x.order, x.index
                                                                       select x;

        private static readonly List<Type> stepsToUseIfNotFresh = new List<Type>()
        {
            typeof(WorldGenStep_Tiles),
            typeof(WorldGenStep_Terrain),
            typeof(WorldGenStep_Factions),
            typeof(WorldGenStep_Features)
        };

        [HandlesPacket(PacketHeader.World)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_World data = Serializer.ConvertBytesToObject<PKT_World>(bytes);

            switch (data._stepMode)
            {
                case WorldStepMode.AskFor:
                    OnAskForWorld();
                    break;

                case WorldStepMode.Sent:
                    PM_World.OnReceiveWorld(data);
                    break;
            }
        }

        public static void OnAskForWorld()
        {
            SessionManager.IsGeneratingFreshWorld = true;

            DLG_Wait.Instance.Close();

            DLG_Base.PushNewDialog(new Page_SelectScenario());
        }

        public static void OnExistingWorld()
        {
            SessionManager.IsGeneratingFreshWorld = false;

            DLG_Wait.Instance.Close();

            DLG_Base.PushNewDialog(new Page_SelectScenario());
        }

        public static void SendWorld()
        {
            PopulateWorldValues();

            PKT_World data = new PKT_World();
            data._stepMode = WorldStepMode.Sent;
            data.File = SessionManager.CurrentWorld;

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.World, data);

            File.Delete(PM_World.tempWorldPath);
            SessionManager.IsGeneratingFreshWorld = false;
        }

        public static void OnReceiveWorld(PKT_World data)
        {
            SetValuesFromServer(data);
            PM_World.OnExistingWorld();
        }

        public static void SetValuesFromGame(string seedString, float planetCoverage, OverallRainfall rainfall, OverallTemperature temperature, 
            OverallPopulation population, LandmarkDensity density, List<FactionDef> factions, float pollution)
        {
            SessionManager.CurrentWorld = new FL_PlanetConfig();
            SessionManager.CurrentWorld.SeedString = seedString;
            SessionManager.CurrentWorld.PersistentRandomValue = GenText.StableStringHash(seedString);
            SessionManager.CurrentWorld.PlanetCoverage = planetCoverage;
            SessionManager.CurrentWorld.Rainfall = (int)rainfall;
            SessionManager.CurrentWorld.Temperature = (int)temperature;
            SessionManager.CurrentWorld.Population = (int)population;
            SessionManager.CurrentWorld.LandmarkDensity = (int)density;
            SessionManager.CurrentWorld.Pollution = pollution;
            SessionManager.CurrentWorld.NPCFactions = GetNPCFactionsFromDef(factions.ToArray());
        }

        private static void SetValuesFromServer(PKT_World data) { SessionManager.CurrentWorld = data.File; }

        public static void GenerateNormalWorld()
        {
            LongEventHandler.QueueLongEvent(delegate
            {
                Find.GameInitData.ResetWorldRelatedMapInitData();

                Rand.EnsureStateStackEmpty();
                Rand.PushState(SessionManager.CurrentWorld.PersistentRandomValue);

                Current.Game.World = WorldGenerator.GenerateWorld(
                    SessionManager.CurrentWorld.PlanetCoverage,
                    SessionManager.CurrentWorld.SeedString,
                    (OverallRainfall)SessionManager.CurrentWorld.Rainfall,
                    (OverallTemperature)SessionManager.CurrentWorld.Temperature,
                    (OverallPopulation)SessionManager.CurrentWorld.Population,
                    (LandmarkDensity)SessionManager.CurrentWorld.LandmarkDensity,
                    GetFactionDefsFromNPCFaction(SessionManager.CurrentWorld.NPCFactions),
                    SessionManager.CurrentWorld.Pollution);

                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    Find.World.renderer.RegenerateAllLayersNow();
                    MemoryUtility.UnloadUnusedUnityAssets();
                    Current.CreatingWorld = null;
                    PostWorldGeneration();
                });
            }, "GeneratingWorld", doAsynchronously: true, null);

            Rand.EnsureStateStackEmpty();
            Rand.PushState(0);
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

            for (int i = 0; i < SessionManager.CurrentWorld.Features.Count; i++)
            {
                FeatureDetail planetFeature = SessionManager.CurrentWorld.Features[i];

                try
                {
                    WorldFeature worldFeature = new WorldFeature();
                    worldFeature.def = DefDatabase<FeatureDef>.AllDefs.First(fetch => fetch.defName == planetFeature.DefName);
                    worldFeature.uniqueID = i;
                    worldFeature.name = planetFeature.Label;
                    worldFeature.maxDrawSizeInTiles = planetFeature.MaxDrawSizeInTiles;
                    worldFeature.drawCenter = new Vector3(planetFeature.DrawCenter[0], planetFeature.DrawCenter[1], planetFeature.DrawCenter[2]);
                    worldFeature.layer = PlanetLayer.Selected;

                    Find.WorldFeatures.features.Add(worldFeature);
                }
                catch (Exception e) { Printer.Warning($"Failed set planet feature from def '{planetFeature.DefName}'. Reason: {e}"); }
            }

            Find.WorldFeatures.textsCreated = false;

            Find.WorldFeatures.UpdateFeatures();
        }

        public static void SetPlanetFactions()
        {
            Faction[] planetFactions = Find.World.factionManager.AllFactions.ToArray();

            for (int i = 0; i < SessionManager.CurrentWorld.NPCFactions.Count; i++)
            {
                try
                {
                    NPCFactionDetail faction = SessionManager.CurrentWorld.NPCFactions[i];

                    Faction toModify = planetFactions.First(fetch => fetch.def.defName == SessionManager.CurrentWorld.NPCFactions[i].DefName);

                    toModify.Name = faction.Name;

                    toModify.color = new Color(faction.Color[0],
                        faction.Color[1],
                        faction.Color[2],
                        faction.Color[3]);
                }
                catch (Exception e) { Printer.Warning($"Failed set planet faction from def '{SessionManager.CurrentWorld.NPCFactions[i].DefName}'. Reason: {e}"); }
            }
        }

        public static void PopulateWorldValues()
        {
            Printer.Warning("Populating world values", Verbosity.Verbose);
            SessionManager.CurrentWorld.Features = GetPlanetFeatures();
            SessionManager.CurrentWorld.Roads = PM_RoadsHelper.GetPlanetRoads();
            SessionManager.CurrentWorld.PollutedTiles = PM_Pollution.GetPlanetPollutedTiles();
            SessionManager.CurrentWorld.NPCFactions = GetPlanetNPCFactions();

            PM_WorldObject.SendAllWorldObjects();
        }

        public static List<NPCFactionDetail> GetNPCFactionsFromDef(FactionDef[] factionDefs)
        {
            List<NPCFactionDetail> npcFactions = new List<NPCFactionDetail>();
            foreach (FactionDef faction in factionDefs)
            {
                try
                {
                    NPCFactionDetail toCreate = new NPCFactionDetail();
                    toCreate.DefName = faction.defName;
                    npcFactions.Add(toCreate);
                }
                catch (Exception e) { Printer.Warning($"Failed to get faction '{faction.defName}' from game. Reason: {e}"); }
            }
            return npcFactions;
        }

        public static List<FactionDef> GetFactionDefsFromNPCFaction(List<NPCFactionDetail> factions)
        {
            List<FactionDef> defList = new List<FactionDef>();
            foreach (NPCFactionDetail faction in factions) defList.Add(DefDatabase<FactionDef>.GetNamed(faction.DefName));

            return defList;
        }

        public static List<NPCFactionDetail> GetPlanetNPCFactions()
        {
            List<NPCFactionDetail> planetFactions = new List<NPCFactionDetail>();
            Faction[] existingFactions = Find.World.factionManager.AllFactions.ToArray();

            foreach (Faction faction in existingFactions)
            {
                try
                {
                    if (faction == Faction.OfPlayer) continue;
                    else
                    {
                        NPCFactionDetail planetFaction = new NPCFactionDetail();
                        planetFaction.DefName = faction.def.defName;
                        planetFaction.Name = faction.Name;
                        planetFaction.Color = new float[] { faction.Color.r, faction.Color.g, faction.Color.b, faction.Color.a };

                        planetFactions.Add(planetFaction);
                    }
                }
                catch (Exception e) { Printer.Warning($"Failed to get NPC faction '{faction.def.defName}' to populate. Reason: {e}"); }
            }

            return planetFactions;
        }

        public static List<FeatureDetail> GetPlanetFeatures()
        {
            List<FeatureDetail> planetFeatures = new List<FeatureDetail>();
            WorldFeature[] worldFeatures = Find.World.features.features.ToArray();
            foreach (WorldFeature worldFeature in worldFeatures)
            {
                try
                {
                    FeatureDetail planetFeature = new FeatureDetail();
                    planetFeature.Label = worldFeature.name;
                    planetFeature.DefName = worldFeature.def.defName;
                    planetFeature.MaxDrawSizeInTiles = worldFeature.maxDrawSizeInTiles;
                    planetFeature.DrawCenter = new float[] { worldFeature.drawCenter.x, worldFeature.drawCenter.y, worldFeature.drawCenter.z };

                    planetFeatures.Add(planetFeature);
                }
                catch (Exception e) { Printer.Warning($"Failed to get feature '{worldFeature.def.defName}' to populate. Reason: {e}"); }
            }

            return planetFeatures;
        }
    }
}
