using GameClient.Managers;
using GameClient.Misc;
using GameClient.PacketManagers.Synchronous;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GameClient.Patches.Synchronous
{
    [HarmonyPatchCategory("Synchronous")]
    [HarmonyPatch(typeof(WeatherManager), nameof(WeatherManager.TransitionTo))]
    public static class P_WeatherManager_TransitionTo
    {
        [HarmonyPrefix]
        public static bool TransitionTo(WeatherDef newWeather, WeatherManager __instance)
        {
            if (PatchHandler.BypassFlag) return true;
            else
            {
                if (!SessionHandler.IsSynchronousHost) return false;
                else if (!SynchronousManager.CheckIfShouldPatch(__instance.map)) return true;
                else
                {
                    byte value = (byte)DefDatabase<WeatherDef>.AllDefs.FirstIndexOf(fetch => fetch == newWeather);
                    PM_SWeather.Ask(value);
                    return false;
                }
            }
        }
    }
}
