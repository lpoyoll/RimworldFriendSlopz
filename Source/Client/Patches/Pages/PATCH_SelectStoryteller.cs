using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Managers;
using HarmonyLib;
using RimWorld;
using System;
using RTNetwork;
using UnityEngine;
using Verse;

namespace GameClient.Patches.Pages
{
    [HarmonyPatch(typeof(Page_SelectStoryteller), nameof(Page_SelectStoryteller.PreOpen))]
    public static class Patch_Page_SelectStoryteller_PreOpen
    {
        [HarmonyPrefix]
        public static bool DoPre(ref DifficultyDef ___difficulty, ref Difficulty ___difficultyValues)
        {
            Find.GameInitData.permadeathChosen = true;
            Find.GameInitData.permadeath = true;

            if (!SessionManager.IsGeneratingFreshWorld)
            {
                ___difficulty = DifficultyDefOf.Rough;
                ___difficultyValues = new Difficulty(___difficulty);
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Page_SelectStoryteller), nameof(Page_SelectStoryteller.DoWindowContents))]
    public static class Patch_Page_SelectStoryteller_DoWindowContents
    {
        public static bool executedMessage;

        [HarmonyPrefix]
        public static bool DoPre(Rect rect, Page_SelectStoryteller __instance)
        {
            if (!SessionManager.IsGeneratingFreshWorld && SessionManager.CurrentStoryteller.IsEnforced)
            {
                if (executedMessage) return true;
                else
                {
                    Action toDo = delegate
                    {
                        PM_GameParameter.SetStoryteller(SessionManager.CurrentStoryteller);
                        PM_GameParameter.SetDifficulty(SessionManager.CurrentDifficulty, true);
                        DLG_Base.PushNewDialog(__instance.next);
                        __instance.Close();

                        executedMessage = false;
                    };
                    DLG_Base.PushNewDialog(new DLG_Message("MESSAGE", new string[] { "Storyteller will be forced by the server" }, toDo));

                    executedMessage = true;
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Page_SelectStorytellerInGame), nameof(Page_SelectStorytellerInGame.PreClose))]
    public static class Patch_Page_SelectStorytellerInGame_PreClose
    {
        [HarmonyPrefix]
        public static bool DoPre()
        {
            if (SessionManager.IsAdmin)
            {
                DLG_Base.PushNewDialog(new DLG_Message("MESSAGE", new string[] { "Difficulty settings overriden due to being an admin" }));
                return true;
            }

            if (SessionManager.CurrentDifficulty.IsEnforced || SessionManager.CurrentStoryteller.IsEnforced)
            {
                Action toDo = delegate
                {
                    PM_GameParameter.SetStoryteller(SessionManager.CurrentStoryteller);
                    PM_GameParameter.SetDifficulty(SessionManager.CurrentDifficulty);
                };

                DLG_Base.PushNewDialog(new DLG_Message("MESSAGE", new string[] { "Settings might change to reflect server enforcements" }, toDo));

                return false;
            }

            return true;
        }
    }
}
