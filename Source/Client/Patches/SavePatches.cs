using System;
using GameClient.Managers;
using GameClient.Misc;
using GameClient.TCP;
using GameClient.Values;
using HarmonyLib;
using Verse;
using static Shared.CommonEnumerators;

namespace GameClient.Patches
{
    [HarmonyPatch(typeof(GameDataSaveLoader), "SaveGame", typeof(string))]
    public static class SaveOnlineGame
    {
        [HarmonyPrefix]
        public static bool DoPre(ref string fileName, ref int ___lastSaveTick)
        {
            try
            {
                if (Network.state == ClientNetworkState.Disconnected) return true;
                if (ClientValues.IsSavingGame) return false;

                ClientValues.ToggleSavingGame(true);
                ClientValues.ForcePermadeath();
                ClientValues.ManageDevOptions();

                GameParameterManager.SetScenario(SessionValues.ScenarioFile);
                GameParameterManager.SetStoryteller(SessionValues.StorytellerFile);
                GameParameterManager.SetDifficulty(SessionValues.DifficultyFile);

                string filePath = GenFilePaths.FilePathForSavedGame(fileName);

                try
                {
                    SafeSaver.Save(filePath, "savegame", delegate
                    {
                        ScribeMetaHeaderUtility.WriteMetaHeader();
                        Game target = Current.Game;
                        Scribe_Deep.Look(ref target, "game");
                    }, Find.GameInfo.permadeathMode);
                    ___lastSaveTick = Find.TickManager.TicksGame;
                }
                catch (Exception e) { Printer.Error("Exception while saving game: " + e); }

                if (Network.state.Equals(ClientNetworkState.Connected))
                {
                    Printer.Message("Sending maps to server");
                    MapManager.SendPlayerMapsToServer();

                    Printer.Message("Sending save to server");
                    SaveSenderManager.SendSaveToServer();
                }
            }
            catch (Exception e) { Printer.Error($"{e}"); }

            ClientValues.ToggleSavingGame(false);

            return false;
        }
    }
}
