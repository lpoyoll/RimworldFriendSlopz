using GameClient.Defs;
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
using GameClient.Managers;

namespace GameClient.Patches
{
    public static class Patch_WorldObjectsHolder
    {
        public static List<Def> RestrictedDefs = new List<Def>()
        {
            WorldObjectDefOf.AbandonedSettlement
        };

        public static bool CheckIfShouldPatch(WorldObject wo)
        {
            if (PM_WorldObject.IsBypass) return false;
            else if (!SessionManager.IsReadyToPlay) return false;
            else if (Patch_WorldObjectsHolder.RestrictedDefs.Contains(wo.def)) return false;
            else if (wo.Faction == Faction.OfPlayer || SessionManager.PlayerFactions.Contains(wo.Faction)) return false;
            else return true;
        }
    }

    [HarmonyPatch(typeof(WorldObjectsHolder), nameof(WorldObjectsHolder.Add))]
    public static class Patch_WorldObjectsHolder_Add
    {
        [HarmonyPrefix]
        public static bool DoPre(WorldObject o)
        {
            if (!Patch_WorldObjectsHolder.CheckIfShouldPatch(o)) return true;
            else
            {
                if (o.def == WorldObjectDefOf.Settlement)
                {
                    PM_WorldObject.Send(o, PKT_WorldObject.StepMode.Add, PKT_WorldObject.WorldObjectMode.Settlement);
                    return false;
                }

                else if (o.def == WorldObjectDefOf.Site)
                {
                    PM_WorldObject.Send(o, PKT_WorldObject.StepMode.Add, PKT_WorldObject.WorldObjectMode.Site);
                    return false;
                }

                else return true;
            }
        }
    }

    [HarmonyPatch(typeof(WorldObjectsHolder), nameof(WorldObjectsHolder.Remove))]
    public static class Patch_WorldObjectsHolder_Remove
    {
        [HarmonyPrefix]
        public static bool DoPre(WorldObject o)
        {
            if (!Patch_WorldObjectsHolder.CheckIfShouldPatch(o)) return true;
            else
            {
                if (o.def == WorldObjectDefOf.Settlement)
                {
                    PM_WorldObject.Send(o, PKT_WorldObject.StepMode.Remove, PKT_WorldObject.WorldObjectMode.Settlement);
                    return false;
                }

                else if (o.def == WorldObjectDefOf.Site)
                {
                    PM_WorldObject.Send(o, PKT_WorldObject.StepMode.Remove, PKT_WorldObject.WorldObjectMode.Site);
                    return false;
                }

                else return true;
            }
        }
    }
}
