using GameClient.Hooks.Synchronous;
using GameClient.Managers;
using GameClient.Misc;
using GameClient.PacketManagers.Synchronous;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace GameClient.Patches.Synchronous
{
    [HarmonyPatchCategory("Synchronous")]
    [HarmonyPatch(typeof(MentalStateHandler), nameof(MentalStateHandler.TryStartMentalState))]
    public static class P_MentalStateHandler_TryStartMentalState
    {
        [HarmonyPrefix]
        public static bool TryStartMentalState(MentalStateDef stateDef, Pawn ___pawn)
        {
            if (PatchHandler.BypassFlag) return true;
            else if (!SessionManager.IsSynchronousHost) return false;
            else if (!SynchronousManager.CheckIfShouldPatch(___pawn.MapHeld)) return true;
            else
            {
                byte value = (byte)DefDatabase<MentalStateDef>.AllDefs.FirstIndexOf(fetch => fetch == stateDef);
                PM_SMentalState.Ask(___pawn, value, PlayerMentalState.MentalMode.Add);
                return false;
            }
        }
    }

    [HarmonyPatchCategory("Synchronous")]
    [HarmonyPatch(typeof(MentalState), nameof(MentalState.RecoverFromState))]
    public static class P_MentalState_RecoverFromState
    {
        [HarmonyPrefix]
        public static bool RecoverFromState(MentalState __instance)
        {
            if (PatchHandler.BypassFlag) return true;
            else if (!SessionManager.IsSynchronousHost) return false;
            else if (!SynchronousManager.CheckIfShouldPatch(__instance.pawn.MapHeld)) return true;
            else if (PM_SMentalState.LatestMentalState == __instance) return false;
            else
            {
                byte value = (byte)DefDatabase<MentalStateDef>.AllDefs.FirstIndexOf(fetch => fetch == __instance.def);
                PM_SMentalState.Ask(__instance.pawn, value, PlayerMentalState.MentalMode.Remove);
                PM_SMentalState.LatestMentalState = __instance;
                return false;
            }
        }
    }

    [HarmonyPatchCategory("Synchronous")]
    [HarmonyPatch(typeof(MentalStateHandler), nameof(MentalStateHandler.MentalStateHandlerTickInterval))]
    public static class P_MentalStateHandler_MentalStateHandlerTickInterval
    {
        [HarmonyPrefix]
        public static bool MentalStateHandlerTickInterval(MentalStateHandler __instance)
        {
            if (__instance.CurState == PM_SMentalState.LatestMentalState) return false;
            else return true;
        }
    }
}
