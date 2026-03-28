using GameClient.PacketManagers;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GameClient.Patches
{
    [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.AbandonMap))]
    public static class Patch_GravshipUtility_AbandonMap
    {
        [HarmonyPostfix]
        public static void DoPost(Map map)
        {
            PM_Settlements.AbandonSettlement(map.Tile);
        }
    }

    [HarmonyPatch(typeof(GravshipUtility), nameof(GravshipUtility.ArriveNewMap))]
    public static class Patch_GravshipUtility_ArriveNewMap
    {
        [HarmonyPostfix]
        public static void DoPost(Gravship gravship)
        {
            PM_Settlements.SendNewPlayerSettlement(gravship.destinationTile);
        }
    }
}
