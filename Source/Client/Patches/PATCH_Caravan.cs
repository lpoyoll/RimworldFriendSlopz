using GameClient.PacketManagers;
using HarmonyLib;
using RimWorld.Planet;

namespace GameClient.Patches
{
    [HarmonyPatch(typeof(Caravan), nameof(Caravan.PostAdd))]
    public static class PatchAddCaravan
    {
        [HarmonyPostfix]
        public static void DoPost(Caravan __instance)
        {
            PM_Caravan.RequestCaravanAdd(__instance);
        }
    }

    [HarmonyPatch(typeof(Caravan), nameof(Caravan.PostRemove))]
    public static class PatchRemoveCaravan
    {
        [HarmonyPostfix]
        public static void DoPost(Caravan __instance)
        {
            PM_Caravan.RequestCaravanRemove(__instance);
        }
    }

    [HarmonyPatch(typeof(Caravan_PathFollower), "TryEnterNextPathTile")]
    public static class PatchMoveCaravan
    {
        [HarmonyPrefix]
        public static bool DoPre(Caravan ___caravan)
        {
            PM_Caravan.RequestCaravanUpdate(___caravan);
            return true;
        }
    }
}
