using GameClient.Misc;
using GameClient.PacketManagers;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GameClient.Patches
{
    [HarmonyPatch(typeof(SettleInEmptyTileUtility), nameof(SettleInEmptyTileUtility.Settle))]
    public static class Patch_SettleInEmptyTileUtility_Settle
    {
        [HarmonyPostfix]
        public static void ModifyPost(Caravan caravan)
        {
            PM_Settlements.SendNewPlayerSettlement(caravan.Tile);
        }
    }

    [HarmonyPatch(typeof(SettleInExistingMapUtility), nameof(SettleInExistingMapUtility.Settle))]
    public static class Patch_SettleInExistingMapUtility_Settle
    {
        [HarmonyPostfix]
        public static void ModifyPost(Map map)
        {
            PM_Settlements.SendNewPlayerSettlement(map.Tile);
        }
    }

    [HarmonyPatch(typeof(SettlementAbandonUtility), "Abandon")]
    public static class Patch_SettlementAbandonUtility_Abandon
    {
        [HarmonyPostfix]
        public static void ModifyPost(Settlement settlement)
        {
            PM_Settlements.AbandonSettlement(settlement.Tile);
        }
    }
}
