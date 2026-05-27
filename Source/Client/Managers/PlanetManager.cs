using GameClient.Defs;
using GameClient.Misc;
using GameClient.PacketManagers;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static Shared.CommonEnumerators;

namespace GameClient.Managers
{
    //Class that handles all the planet functions for the mod
    public static class PlanetManager
    {
        //Regenerates the planet of player objects

        public static void BuildPlanet()
        {
            PlanetManagerHelper.GetPlayerFactionsInWorld();

            //This step gets skiped if it's the first time building the planet
            if (SessionHandler.IsGeneratingFreshWorld) return;
            else
            {
                PM_WorldObject.ClearAllObjects();

                PM_WorldObject.AddWorldObjects(SessionHandler.GlobalData.WorldObjects);
                PM_Settlements.AddSettlements(SessionHandler.GlobalData.PlayerSettlements);
                PM_Roads.AddRoads(SessionHandler.GlobalData.Roads, false);
                PM_Sites.AddSites(SessionHandler.GlobalData.PlayerSites);
                PM_Caravan.AddCaravans();
                
                if (ModLister.BiotechInstalled) PM_Pollution.AddPollutedTiles(SessionHandler.GlobalData.PollutedTiles, false);
            }
        }
    }

    public static class PlanetManagerHelper
    {
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
            }

            return factionToUse;
        }

        //Returns an npc faction depending on the value

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

            return factions;
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

            SessionHandler.PlayerFactionDefs.Clear();
            SessionHandler.PlayerFactionDefs.Add(SessionHandler.EnemyFaction.def);
            SessionHandler.PlayerFactionDefs.Add(SessionHandler.AllyFaction.def);
            SessionHandler.PlayerFactionDefs.Add(SessionHandler.NeutralFaction.def);
            SessionHandler.PlayerFactionDefs.Add(SessionHandler.GuildFaction.def);
        }
    }
}
