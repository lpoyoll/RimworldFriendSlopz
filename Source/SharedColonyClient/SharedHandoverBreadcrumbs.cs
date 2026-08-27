using HarmonyLib;
using RTNetwork.Packets;

namespace RWTSharedColony
{
    [HarmonyPatch(typeof(SharedTileLiveSync), nameof(SharedTileLiveSync.TryAutoAccept))]
    public static class RimjobHostHandoverBreadcrumbPatch
    {
        [HarmonyPrefix]
        public static void Prefix(PKT_Synchronous request)
        {
            RimjobCrashCapture.Mark($"Host TryAutoAccept begin; from={request?.Username ?? "<unknown>"}; fromTile={request?.FromTile ?? -1}; toTile={request?.ToTile ?? -1}");
        }

        [HarmonyPostfix]
        public static void Postfix(bool __result)
        {
            RimjobCrashCapture.Mark("Host TryAutoAccept end; accepted=" + __result);
        }
    }

    [HarmonyPatch(typeof(SharedTileLiveSync), nameof(SharedTileLiveSync.TryApplyAccept))]
    public static class RimjobGuestHandoverBreadcrumbPatch
    {
        [HarmonyPrefix]
        public static void Prefix(PKT_Synchronous packet)
        {
            RimjobCrashCapture.Mark($"Guest TryApplyAccept begin; from={packet?.Username ?? "<unknown>"}; dataBytes={packet?.Data?.Length ?? 0}");
        }

        [HarmonyPostfix]
        public static void Postfix(bool __result)
        {
            RimjobCrashCapture.Mark("Guest TryApplyAccept end; applied=" + __result);
        }
    }
}
