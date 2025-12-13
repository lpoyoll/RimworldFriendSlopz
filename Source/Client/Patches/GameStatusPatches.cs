using GameClient.Managers;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Shared;
using Verse;
using static Shared.CommonEnumerators;
using GameClient.Misc;
using GameClient.Dialogs;
using System;

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
                if (SessionHandler.CurrentNetworkState == ClientNetworkState.Connected)
                {
                    SettlementManager.SendNewPlayerSettlement(__instance.CurrentMap.Tile);

                    if (!SessionHandler.IsGeneratingFreshWorld) SaveManager.ForceSave();
                    else ModManager.OpenModManagerMenu(true);

                    SessionHandler.ForcePermadeath();
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

                    GameParameterManager.SetScenario(SessionHandler.ScenarioFile);
                    GameParameterManager.SetStoryteller(SessionHandler.StorytellerFile);
                    GameParameterManager.SetDifficulty(SessionHandler.DifficultyFile);

                    SessionHandler.ForcePermadeath();
                    SessionHandler.IsReadyToPlay = true;

                    MainThreadHandler.Instance.DoOnStartMethods();
                }
            }
        }

        [HarmonyPatch(typeof(Dialog_Options), nameof(Dialog_Options.DoWindowContents))]
        public static class PatchDevMode
        {
            [HarmonyPostfix]
            public static void DoPost()
            {
                if (SessionHandler.CurrentNetworkState == ClientNetworkState.Connected) SessionHandler.ManageDevOptions();
                else return;
            }
        }
    }
}
