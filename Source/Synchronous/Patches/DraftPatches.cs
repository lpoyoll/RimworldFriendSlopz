using GameClient.Managers;
using GameClient.Misc;
using GameClient.PacketManagers.Synchronous;
using HarmonyLib;
using RimWorld;

namespace Synchronous.Patches
{
    [HarmonyPatchCategory("Synchronous")]
    [HarmonyPatch(typeof(Pawn_DraftController), nameof(Pawn_DraftController.Drafted), MethodType.Setter)]
    public static class P_Pawn_DraftController_Drafted
    {
        [HarmonyPrefix]
        public static bool Drafted(Pawn_DraftController __instance, bool value)
        {
            if (PatchHandler.BypassFlag) return true;
            else if (!SynchronousManager.CheckIfShouldPatch(__instance.pawn.MapHeld)) return true;
            else
            {
                PM_SDraft.Ask(__instance.pawn, value);
                return false;
            }
        }
    }
}
