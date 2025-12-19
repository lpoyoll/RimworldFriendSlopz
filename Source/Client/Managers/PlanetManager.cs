using GameClient.Defs;
using GameClient.Misc;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Shared;
using Verse;
using static Shared.CommonEnumerators;

namespace GameClient.Managers;

public static class PlanetManager
{
    //Regenerates the planet of player objects

    public static void BuildPlanet()
    {
        PlanetManagerHelper.GetPlayerFactionsInWorld();
        PlanetManagerHelper.GetMapGenerators();

        //This step gets skipped if it's the first time building the planet
        if (SessionHandler.IsGeneratingFreshWorld) return;
        else
        {
            SettlementManager.ClearAllSettlements();
            SettlementManager.AddSettlements(PlayerSettlementManagerHelper.TempSettlements);
                
            SiteManager.ClearAllSites();
            SiteManager.AddSites(SiteManagerH.tempSites);
                
            NPCManager.ClearAllSettlements();
            NPCManagerH.SaveAllQuests();
                
            NPCManager.AddSettlements(NPCManagerH.TempNPCSettlements);
            NPCManagerH.CleanupQuests();
                
            RoadManager.ClearAllRoads();
            RoadManager.AddRoads(RoadManagerHelper.tempRoadDetails, false);
                
            if (ModLister.BiotechInstalled)
            {
                PollutionManager.ClearAllPollution();
                PollutionManager.AddPollutedTiles(PollutionManagerHelper.TempPollutionDetails, false);
            }
                
            CaravanManager.ClearAllCaravans();
        }
    }
}

public static class PlanetManagerHelper
{
    public static MapGeneratorDef emptyGenerator;
    public static MapGeneratorDef defaultSettlementGenerator;
    public static MapGeneratorDef defaultSiteGenerator;

    public static Faction GetPlayerFactionFromGoodwill(Goodwill goodwill)
    {
        Faction factionToUse = null;
        switch (goodwill)
        {
            case Goodwill.Enemy:
                factionToUse = SessionHandler.EnemyFaction;
                break;

            case Goodwill.Neutral:
                factionToUse = SessionHandler.NeutralFaction;
                break;

            case Goodwill.Ally:
                factionToUse = SessionHandler.AllyFaction;
                break;

            case Goodwill.Guild:
                factionToUse = SessionHandler.GuildFaction;
                break;

            case Goodwill.Personal:
                factionToUse = Faction.OfPlayer;
                break;
            
            default:
                Printer.Error($"Received invalid goodwill {goodwill}");
                return null;
        }

        return factionToUse;
    }

    public static List<Faction> GetNPCFactionFromDefName(string defName)
    {
        List<Faction> factions = new List<Faction>();
        foreach (Faction faction in Find.World.factionManager.AllFactions)
        {
            if (faction.def.defName == defName)
            {
                factions.Add(faction);
            }
        }

        if (factions.Count >= 1) return factions;
        else
        {
            switch (defName) // If missing factions from missing dlcs.
            {
                case "OutlanderRoughPig":
                    factions.AddRange(GetNPCFactionFromDefName(FactionDefOf.OutlanderRough.defName));
                    break;

                case "PirateYttakin":
                    factions.AddRange(GetNPCFactionFromDefName(FactionDefOf.Pirate.defName));
                    break;

                case "PirateWaster":
                    factions.AddRange(GetNPCFactionFromDefName(FactionDefOf.Pirate.defName));
                    break;

                case "TribeRoughNeanderthal":
                    factions.AddRange(GetNPCFactionFromDefName(FactionDefOf.TribeRough.defName));
                    break;

                case "TribeSavageImpid":
                    factions.AddRange(GetNPCFactionFromDefName(FactionDefOf.TribeRough.defName));
                    break;

                case "TribeCannibal":
                    factions.AddRange(GetNPCFactionFromDefName(FactionDefOf.TribeRough.defName));
                    break;

                case "Empire":
                    factions.AddRange(GetNPCFactionFromDefName(FactionDefOf.OutlanderCivil.defName));
                    break;

                default:
                    break;
            }

            return factions;
        }
    }

    //Gets the default generator for the map builder

    public static void GetMapGenerators()
    {
        emptyGenerator = DefDatabase<MapGeneratorDef>.AllDefs.First(fetch => fetch.defName == "Empty");

        WorldObjectDef settlement = WorldObjectDefOf.Settlement;
        defaultSettlementGenerator = settlement.mapGenerator;

        WorldObjectDef site = WorldObjectDefOf.Site;
        defaultSiteGenerator = site.mapGenerator;
    }

    //Sets the default generator for the map builder

    public static void SetDefaultGenerators()
    {
        WorldObjectDef settlement = WorldObjectDefOf.Settlement;
        settlement.mapGenerator = defaultSettlementGenerator;

        WorldObjectDef site = WorldObjectDefOf.Site;
        site.mapGenerator = defaultSiteGenerator;
    }

    public static void SetOverrideGenerators()
    {
        WorldObjectDef settlement = WorldObjectDefOf.Settlement;
        settlement.mapGenerator = emptyGenerator;

        WorldObjectDef site = WorldObjectDefOf.Site;
        site.mapGenerator = emptyGenerator;
    }

    public static void GetPlayerFactionsInWorld()
    {
        Faction[] factions = Find.FactionManager.AllFactions.ToArray();

        SessionHandler.EnemyFaction = factions.First(fetch => fetch.def.defName == RTFactionDefOf.RTEnemy.defName);
        SessionHandler.AllyFaction = factions.First(fetch => fetch.def.defName == RTFactionDefOf.RTAlly.defName);
        SessionHandler.NeutralFaction = factions.First(fetch => fetch.def.defName == RTFactionDefOf.RTNeutral.defName);
        SessionHandler.GuildFaction = factions.First(fetch => fetch.def.defName == RTFactionDefOf.RTFaction.defName);

        SessionHandler.PlayerFactions.Clear();
        SessionHandler.PlayerFactions.Add(SessionHandler.EnemyFaction);
        SessionHandler.PlayerFactions.Add(SessionHandler.AllyFaction);
        SessionHandler.PlayerFactions.Add(SessionHandler.NeutralFaction);
        SessionHandler.PlayerFactions.Add(SessionHandler.GuildFaction);
    }
}