using GameClient.Managers;
using GameClient.Misc;
using GameClient.PacketManagers.Synchronous;
using HarmonyLib;
using Synchronous.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace GameClient.Patches.Synchronous
{
    [HarmonyPatchCategory("Synchronous")]
    [HarmonyPatch(typeof(Thing), nameof(Thing.Destroy))]
    public static class P_Thing_Destroy
    {
        [HarmonyPrefix]
        public static bool Destroy(Thing __instance)
        {
            if (PatchHandler.BypassFlag) return true;
            else if (!SynchronousManager.CheckIfShouldPatch(__instance.MapHeld)) return true;
            else if (!SessionHandler.IsSynchronousHost) return false;
            else
            {
                PM_SDestroy.Ask(__instance);
                return false;
            }
        }
    }
}
