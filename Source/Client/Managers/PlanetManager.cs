using GameClient.Defs;
using GameClient.PacketManagers;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static RTShared.CommonEnumerators;

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
            if (SessionManager.IsGeneratingFreshWorld) return;
            else
            {
                PM_WorldObject.ClearAllObjects();

                PM_WorldObject.AddWorldObjects(SessionManager.GlobalData.WorldObjects);
                PM_Settlements.AddSettlements(SessionManager.GlobalData.PlayerSettlements);
                PM_Roads.AddRoads(SessionManager.GlobalData.Roads, false);
                PM_Sites.AddSites(SessionManager.GlobalData.PlayerSites);
                PM_Caravan.AddCaravans();
                
                if (ModLister.BiotechInstalled) PM_Pollution.AddPollutedTiles(SessionManager.GlobalData.PollutedTiles, false);
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
                    factionToUse = SessionManager.EnemyFaction;
                    break;

                case Goodwill.Neutral:
                    factionToUse = SessionManager.NeutralFaction;
                    break;

                case Goodwill.Ally:
                    factionToUse = SessionManager.AllyFaction;
                    break;

                case Goodwill.Guild:
                    factionToUse = SessionManager.GuildFaction;
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

            SessionManager.EnemyFaction = factions.First(fetch => fetch.def.defName == RTFactionDefOf.RTEnemy.defName);
            SessionManager.AllyFaction = factions.First(fetch => fetch.def.defName == RTFactionDefOf.RTAlly.defName);
            SessionManager.NeutralFaction = factions.First(fetch => fetch.def.defName == RTFactionDefOf.RTNeutral.defName);
            SessionManager.GuildFaction = factions.First(fetch => fetch.def.defName == RTFactionDefOf.RTFaction.defName);

            SessionManager.PlayerFactions.Clear();
            SessionManager.PlayerFactions.Add(SessionManager.EnemyFaction);
            SessionManager.PlayerFactions.Add(SessionManager.AllyFaction);
            SessionManager.PlayerFactions.Add(SessionManager.NeutralFaction);
            SessionManager.PlayerFactions.Add(SessionManager.GuildFaction);

            SessionManager.PlayerFactionDefs.Clear();
            SessionManager.PlayerFactionDefs.Add(SessionManager.EnemyFaction.def);
            SessionManager.PlayerFactionDefs.Add(SessionManager.AllyFaction.def);
            SessionManager.PlayerFactionDefs.Add(SessionManager.NeutralFaction.def);
            SessionManager.PlayerFactionDefs.Add(SessionManager.GuildFaction.def);
        }
    }
}
