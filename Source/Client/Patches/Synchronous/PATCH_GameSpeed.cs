using GameClient.Managers;
using GameClient.Misc;
using GameClient.PacketManagers.Synchronous;
using HarmonyLib;
using Verse;

namespace GameClient.Patches.Synchronous
{
    [HarmonyPatchCategory("Synchronous")]
    [HarmonyPatch(typeof(TickManager), nameof(TickManager.CurTimeSpeed), MethodType.Setter)]
    public static class P_TickManager_CurTimeSpeed
    {
        [HarmonyPrefix]
        public static bool CurTimeSpeed(TimeSpeed value)
        {
            if (PatchHandler.BypassFlag) return true;
            else if (!SessionManager.IsSynchronousHost) return false;
            else if (value > TimeSpeed.Normal) return false;
            else
            {
                PM_SGameSpeed.Ask(value);
                return false;
            }
        }
    }

    [HarmonyPatchCategory("Synchronous")]
    [HarmonyPatch(typeof(TickManager), nameof(TickManager.TogglePaused))]
    public static class P_TickManager_TogglePaused
    {
        [HarmonyPrefix]
        public static bool TogglePaused()
        {
            if (PatchHandler.BypassFlag) return false;
            else if (!SessionManager.IsSynchronousHost) return false;
            else
            {
                PM_SGameSpeed.Ask(TimeSpeed.Paused);
                return false;
            }
        }
    }

    [HarmonyPatchCategory("Synchronous")]
    [HarmonyPatch(typeof(TickManager), nameof(TickManager.Paused), MethodType.Getter)]
    public static class P_TickManager_Paused
    {
        [HarmonyPrefix]
        public static bool Paused(ref bool __result)
        {
            if (LongEventHandler.ForcePause) __result = true;
            else if (Find.TickManager.CurTimeSpeed != 0) __result = Find.TilePicker.Active;
            else __result = false;

            return false;
        }
    }
}
