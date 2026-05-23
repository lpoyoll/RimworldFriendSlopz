using GameClient.Misc;
using GameClient.WorldObjects;
using HarmonyLib;
using RimWorld.Planet;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace GameClient.Patches
{
    [HarmonyPatch(typeof(WorldObjectsHolder), nameof(WorldObjectsHolder.Add))]
    public static class Patch_WorldObjectsHolder_Add
    {
        [HarmonyPostfix]
        public static void DoPost(WorldObject o)
        {
            if (!SessionHandler.IsReadyToPlay) return;
            else if (o.GetType() != typeof(Site)) return;
            else Printer.Warning($"Added '{o.GetType().Name}' - {o.Label}");
        }
    }

    [HarmonyPatch(typeof(WorldObjectsHolder), nameof(WorldObjectsHolder.Remove))]
    public static class Patch_WorldObjectsHolder_Remove
    {
        [HarmonyPostfix]
        public static void DoPost(WorldObject o)
        {
            if (!SessionHandler.IsReadyToPlay) return;
            else if (o.GetType() != typeof(Site)) return;
            else Printer.Warning($"Removed '{o.GetType().Name}' - {o.Label}");
        }
    }
}
