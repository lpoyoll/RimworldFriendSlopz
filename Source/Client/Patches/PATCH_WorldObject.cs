using GameClient.Defs;
using GameClient.Misc;
using GameClient.PacketManagers;
using GameClient.WorldObjects;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RTShared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RTNetwork.Packets;
using Verse;

namespace GameClient.Patches
{
    public static class Patch_WorldObjectsHolder
    {
        public static List<Def> RestrictedDefs = new List<Def>()
        {
            WorldObjectDefOf.AbandonedSettlement,
            WorldObjectDefOf.TravelingShuttle,
            WorldObjectDefOf.TravellingTransporters,
            WorldObjectDefOf.GravshipLaunch,
            WorldObjectDefOf.Gravship,
            WorldObjectDefOf.RoutePlannerWaypoint,
            WorldObjectDefOf.Caravan,
            RTSitePartDefOf.RTBase
        };
    }

    [HarmonyPatch(typeof(WorldObjectsHolder), nameof(WorldObjectsHolder.Add))]
    public static class Patch_WorldObjectsHolder_Add
    {
        [HarmonyPrefix]
        public static bool DoPre(WorldObject o)
        {
            if (!SessionHandler.IsReadyToPlay) return true;

            if (PM_WorldObject.IsBypass) return true;
            else
            {
                if (o.def == WorldObjectDefOf.Settlement)
                {
                    if (o.Faction == Faction.OfPlayer) return true;
                    else
                    {
                        PM_WorldObject.Send(o, PKT_WorldObject.StepMode.Add, PKT_WorldObject.WorldObjectMode.Settlement);
                        return false;
                    }
                }

                else
                {
                    if (o.Faction == Faction.OfPlayer) return true;
                    else if (Patch_WorldObjectsHolder.RestrictedDefs.Contains(o.def)) return true;
                    else
                    {
                        PM_WorldObject.Send(o, PKT_WorldObject.StepMode.Add, PKT_WorldObject.WorldObjectMode.Site);
                        return false;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(WorldObjectsHolder), nameof(WorldObjectsHolder.Remove))]
    public static class Patch_WorldObjectsHolder_Remove
    {
        [HarmonyPrefix]
        public static bool DoPre(WorldObject o)
        {
            if (!SessionHandler.IsReadyToPlay) return true;

            if (PM_WorldObject.IsBypass) return true;
            else
            {
                if (o.def == WorldObjectDefOf.Settlement)
                {
                    if (o.Faction == Faction.OfPlayer) return true;
                    else
                    {
                        PM_WorldObject.Send(o, PKT_WorldObject.StepMode.Remove, PKT_WorldObject.WorldObjectMode.Settlement);
                        return false;
                    }
                }

                else
                {
                    if (o.Faction == Faction.OfPlayer) return true;
                    else if (Patch_WorldObjectsHolder.RestrictedDefs.Contains(o.def)) return true;
                    else
                    {
                        PM_WorldObject.Send(o, PKT_WorldObject.StepMode.Remove, PKT_WorldObject.WorldObjectMode.Site);
                        return false;
                    }
                }
            }
        }
    }
}
