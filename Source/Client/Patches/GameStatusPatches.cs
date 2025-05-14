using GameClient.Managers;
using GameClient.TCP;
using GameClient.Values;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Shared;
using Verse;
using static Shared.CommonEnumerators;

namespace GameClient.Patches
{
    public class GameStatusPatches
    {
        [HarmonyPatch(typeof(Game), nameof(Game.InitNewGame))]
        public static class InitModePatch
        {
            [HarmonyPostfix]
            public static void ModifyPost(Game __instance)
            {
                if (Network.State == ClientNetworkState.Connected)
                {
                    SettlementManager.SendNewPlayerSettlement(__instance.CurrentMap.Tile);

                    if (ClientValues.IsGeneratingFreshWorld) WorldManagerSender.SendWorld();
                    else SaveManager.ForceSave();

                    ClientValues.ToggleReadyToPlay(true);
                }
            }
        }

        [HarmonyPatch(typeof(Game), nameof(Game.LoadGame))]
        public static class LoadModePatch
        {
            [HarmonyPostfix]
            public static void GetIDFromExistingGame()
            {
                if (Network.State == ClientNetworkState.Connected)
                {
                    PlanetManager.BuildPlanet();

                    GameParameterManager.SetScenario(SessionValues.ScenarioFile);
                    GameParameterManager.SetStoryteller(SessionValues.StorytellerFile);
                    GameParameterManager.SetDifficulty(SessionValues.DifficultyFile);

                    ClientValues.ToggleReadyToPlay(true);
                }
            }
        }

        [HarmonyPatch(typeof(SettleInEmptyTileUtility), nameof(SettleInEmptyTileUtility.Settle))]
        public static class SettlePatch
        {
            [HarmonyPostfix]
            public static void ModifyPost(Caravan caravan)
            {
                if (Network.State == ClientNetworkState.Connected)
                {
                    SettlementManager.SendNewPlayerSettlement(caravan.Tile);

                    SaveManager.ForceSave();
                }
            }
        }

        [HarmonyPatch(typeof(SettleInExistingMapUtility), nameof(SettleInExistingMapUtility.Settle))]
        public static class SettleInMapPatch
        {
            [HarmonyPostfix]
            public static void ModifyPost(Map map)
            {
                if (Network.State == ClientNetworkState.Connected)
                {
                    SettlementManager.SendNewPlayerSettlement(map.Tile);

                    SaveManager.ForceSave();
                }
            }
        }

        [HarmonyPatch(typeof(SettlementAbandonUtility), "Abandon")]
        public static class AbandonPatch
        {
            [HarmonyPostfix]
            public static void ModifyPost(Settlement settlement)
            {
                if (Network.State == ClientNetworkState.Connected)
                {
                    PlayerSettlementData settlementData = new PlayerSettlementData();
                    settlementData._settlementFile.Tile = settlement.Tile;
                    settlementData._stepMode = SettlementStepMode.Remove;

                    Network.Listener.EnqueuePacket(PacketHeader.SettlementManager, settlementData);

                    SaveManager.ForceSave();
                }
            }
        }

        [HarmonyPatch(typeof(Settlement), nameof(Settlement.PostRemove))]
        public static class DestroyNPCSettlementPatch
        {
            [HarmonyPostfix]
            public static void ModifyPost(Settlement __instance)
            {
                if (Network.State == ClientNetworkState.Connected)
                {
                    if (!ClientValues.IsReadyToPlay) return;
                    if (!SessionValues.ActionValues.EnableNPCDestruction) return;

                    if (__instance.Faction == Faction.OfPlayer) return;
                    else if (ClientValues.PlayerFactions.Contains(__instance.Faction)) return;
                    else if (NPCManagerH.lastRemovedSettlement != __instance) NPCManager.RequestSettlementRemoval(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(Dialog_Options), nameof(Dialog_Options.DoWindowContents))]
        public static class PatchDevMode
        {
            [HarmonyPostfix]
            public static void DoPost()
            {
                if (Network.State == ClientNetworkState.Connected) ClientValues.ManageDevOptions();
                else return;
            }
        }
    }
}
