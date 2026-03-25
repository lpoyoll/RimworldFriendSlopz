using GameClient.Dialogs.Default;
using GameClient.Managers;
using GameClient.Misc;
using GameClient.PacketManagers;
using HarmonyLib;
using Shared.Misc;
using System;
using Verse;
using static Shared.CommonEnumerators;
using static Shared.Misc.Printer;

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
                if (SessionHandler.IsSavingGame) return false;
                if (SessionHandler.SynchronousMap != null) return false;

                SessionHandler.IsSavingGame = true;
                PM_Saves.LatestSavePath = GenFilePaths.FilePathForSavedGame(fileName);

                try
                {
                    SafeSaver.Save(PM_Saves.LatestSavePath, "savegame", delegate
                    {
                        ScribeMetaHeaderUtility.WriteMetaHeader();
                        Game target = Current.Game;
                        Scribe_Deep.Look(ref target, "game");
                    }, Find.GameInfo.permadeathMode);
                    ___lastSaveTick = Find.TickManager.TicksGame;
                }
                catch (Exception e) { Printer.Error("Exception while saving game: " + e); }

                PM_Saves.OnSave();

                DLG_Wait.Instance.Close();
            }
            catch (Exception e) { Printer.Error(e); }

            SessionHandler.IsSavingGame = false;

            return false;
        }
    }
}
