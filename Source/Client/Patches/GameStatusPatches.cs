using GameClient.Managers;
using HarmonyLib;
using Verse;
using static Shared.CommonEnumerators;
using GameClient.Misc;

namespace GameClient.Patches;

public class GameStatusPatches
{
    [HarmonyPatch(typeof(Game), nameof(Game.InitNewGame))]
    public static class InitModePatch
    {
        [HarmonyPostfix]
        public static void ModifyPost(Game __instance)
        {
            if (SessionHandler.CurrentNetworkState == ClientNetworkState.Connected)
            {
                SettlementManager.SendNewPlayerSettlement(__instance.CurrentMap.Tile);

                if (!SessionHandler.IsGeneratingFreshWorld) SaveManager.ForceSave();
                else ModManager.OpenModManagerMenu(true);

                SessionHandler.IsReadyToPlay = true;

                MainThreadHandler.Instance.DoOnStartMethods();
            }
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.LoadGame))]
    public static class LoadModePatch
    {
        [HarmonyPostfix]
        public static void GetIDFromExistingGame()
        {
            if (SessionHandler.CurrentNetworkState == ClientNetworkState.Connected)
            {
                PlanetManager.BuildPlanet();

                GameParameterManager.SetScenario(SessionHandler.CurrentScenario);
                GameParameterManager.SetStoryteller(SessionHandler.CurrentStoryteller);
                GameParameterManager.SetDifficulty(SessionHandler.CurrentDifficulty);

                SessionHandler.IsReadyToPlay = true;

                MainThreadHandler.Instance.DoOnStartMethods();
            }
        }
    }
}