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
using GameClient.PacketManagers;
using GameClient.Dialogs.Default;

namespace GameClient.Patches
{
    [HarmonyPatch(typeof(Game), nameof(Game.InitNewGame))]
    public static class InitModePatch
    {
        [HarmonyPostfix]
        public static void ModifyPost(Game __instance)
        {
            PM_Settlements.SendNewPlayerSettlement(__instance.CurrentMap.Tile);
            MainThreadHandler.Instance.DoOnStartMethods();
            SessionHandler.IsReadyToPlay = true;

            DLG_Base.PushNewDialog(new DLG_Message("Save", new string[] { "Game will save now" }, PM_Saves.ForceSave));

            if (SessionHandler.IsGeneratingFreshWorld)
            {
                GameParameterManager.SetFirstTimeSetup();
                PM_Mods.OpenModManagerMenu();
            }
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.LoadGame))]
    public static class LoadModePatch
    {
        [HarmonyPostfix]
        public static void GetIDFromExistingGame()
        {
            PlanetManager.BuildPlanet();
            SessionHandler.IsReadyToPlay = true;
            MainThreadHandler.Instance.DoOnStartMethods();
        }
    }
}
