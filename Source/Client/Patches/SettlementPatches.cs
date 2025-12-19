using GameClient.Managers;
using GameClient.Misc;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using static Shared.CommonEnumerators;

namespace GameClient.Patches;

[HarmonyPatch(typeof(SettleInEmptyTileUtility), nameof(SettleInEmptyTileUtility.Settle))]
public static class Patch_SettleInEmptyTileUtility_Settle
{
    [HarmonyPostfix]
    public static void ModifyPost(Caravan caravan)
    {
        if (SessionHandler.CurrentNetworkState == ClientNetworkState.Connected)
        {
            SettlementManager.SendNewPlayerSettlement(caravan.Tile);

            SaveManager.ForceSave();
        }
    }
}

[HarmonyPatch(typeof(SettleInExistingMapUtility), nameof(SettleInExistingMapUtility.Settle))]
public static class Patch_SettleInExistingMapUtility_Settle
{
    [HarmonyPostfix]
    public static void ModifyPost(Map map)
    {
        if (SessionHandler.CurrentNetworkState == ClientNetworkState.Connected)
        {
            SettlementManager.SendNewPlayerSettlement(map.Tile);

            SaveManager.ForceSave();
        }
    }
}

[HarmonyPatch(typeof(SettlementAbandonUtility), "Abandon")]
public static class Patch_SettlementAbandonUtility_Abandon
{
    [HarmonyPostfix]
    public static void ModifyPost(Settlement settlement)
    {
        if (SessionHandler.CurrentNetworkState != ClientNetworkState.Connected) return;
        else SettlementManager.AbandonSettlement(settlement.Tile);
    }
}

[HarmonyPatch(typeof(Settlement), nameof(Settlement.PostRemove))]
public static class Patch_Settlement_PostRemove
{
    [HarmonyPostfix]
    public static void ModifyPost(Settlement __instance)
    {
        if (SessionHandler.CurrentNetworkState == ClientNetworkState.Connected)
        {
            if (!SessionHandler.CurrentActionValues.EnableNPCDestruction) return;
            else
            {
                if (__instance.Faction == Faction.OfPlayer) return;
                else if (NPCManagerH.LastRemovedSettlement != __instance) NPCManager.RequestSettlementRemoval(__instance);
            }
        }
    }
}