using System;
using GameClient.Dialogs;
using GameClient.Managers;
using Shared.Network.Client;
using GameClient.Values;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using static Shared.CommonEnumerators;

namespace GameClient.Patches.Pages
{
    [HarmonyPatch(typeof(Page_SelectStoryteller), "PreOpen")]
    public static class PatchDifficultyOverride
    {
        [HarmonyPrefix]
        public static bool DoPre(ref DifficultyDef ___difficulty, ref Difficulty ___difficultyValues)
        {
            if (Network.State == ClientNetworkState.Disconnected) return true;
            else
            {
                Find.GameInitData.permadeathChosen = true;
                Find.GameInitData.permadeath = true;

                if (!ClientValues.IsGeneratingFreshWorld)
                {
                    ___difficulty = DifficultyDefOf.Rough;
                    ___difficultyValues = new Difficulty(___difficulty);
                }

                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Page_SelectStoryteller), "DoWindowContents")]
    public static class PatchSelectStorytellerPage
    {
        public static bool executedMessage;

        [HarmonyPrefix]
        public static bool DoPre(Rect rect, Page_SelectStoryteller __instance)
        {
            if (Network.State == ClientNetworkState.Disconnected) return true;

            if (ClientValues.IsGeneratingFreshWorld)
            {
                if (Widgets.ButtonText(RT_Dialog_Base.GetRectForLocation(rect, RT_Dialog_Base.SmallButtonSize, RT_Dialog_Base.RectLocation.BottomRight), ""))
                {
                    Current.Game.storyteller = GameParameterManagerH.GetStorytellerReference(__instance);

                    Action difficultyYes = delegate
                    {
                        DifficultyManager.SetDifficulty(DifficultyManager.GetDifficulty(__instance), true);
                        DifficultyManager.SendDifficulty(DifficultyManager.GetDifficulty(__instance), true);
                        RT_Dialog_Base.PushNewDialog(__instance.next);
                        __instance.Close();
                    };

                    Action difficultyNo = delegate
                    {
                        DifficultyManager.SetDifficulty(DifficultyManager.GetDifficulty(__instance), true);
                        DifficultyManager.SendDifficulty(DifficultyManager.GetDifficulty(__instance), false);
                        RT_Dialog_Base.PushNewDialog(__instance.next);
                        __instance.Close();
                    };

                    RT_Dialog_YesNo d2 = new RT_Dialog_YesNo("Do you want to ENFORCE the selected DIFFICULTY?", difficultyYes, difficultyNo);

                    Action storytellerYes = delegate
                    {
                        GameParameterManager.SetStoryteller(GameParameterManager.GetStoryteller(__instance), true);
                        GameParameterManager.SendStoryteller(GameParameterManager.GetStoryteller(__instance), true);
                        RT_Dialog_Base.PushNewDialog(d2);
                    };

                    Action storytellerNo = delegate
                    {
                        GameParameterManager.SetStoryteller(GameParameterManager.GetStoryteller(__instance), true);
                        GameParameterManager.SendStoryteller(GameParameterManager.GetStoryteller(__instance), false);
                        RT_Dialog_Base.PushNewDialog(d2);
                    };

                    RT_Dialog_YesNo d1 = new RT_Dialog_YesNo("Do you want to ENFORCE the selected STORYTELLER?", storytellerYes, storytellerNo);

                    RT_Dialog_Base.PushNewDialog(d1);
                };
            }

            else
            {
                if (SessionValues.StorytellerFile.EnforceStoryteller)
                {
                    if (executedMessage) return true;
                    else
                    {
                        Action toDo = delegate
                        {
                            GameParameterManager.SetStoryteller(SessionValues.StorytellerFile);
                            DifficultyManager.SetDifficulty(SessionValues.DifficultyFile, true);
                            RT_Dialog_Base.PushNewDialog(__instance.next);
                            __instance.Close();

                            executedMessage = false;
                        };
                        RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("MESSAGE", new string[] { "Storyteller will be forced by the server" }, toDo));

                        executedMessage = true;
                    }
                }
            }

            return true;
        }

        [HarmonyPostfix]
        public static void DoPost(Rect rect)
        {
            if (Network.State == ClientNetworkState.Disconnected) return;
            if (ClientValues.IsGeneratingFreshWorld) return;

            Text.Font = GameFont.Small;
            Vector2 buttonSize = new Vector2(150f, 38f);
            Vector2 buttonLocation = new Vector2(rect.xMax - buttonSize.x, rect.yMax - buttonSize.y);
            if (Widgets.ButtonText(new Rect(buttonLocation.x, buttonLocation.y, buttonSize.x, buttonSize.y), "Join")) { }
        }
    }

    [HarmonyPatch(typeof(Page_SelectStorytellerInGame), "PreClose")]
    public static class PatchSelectStorytellerInGamePageClose
    {
        [HarmonyPrefix]
        public static bool DoPre()
        {
            if (Network.State == ClientNetworkState.Disconnected) return true;

            if (ClientValues.IsAdmin)
            {
                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("MESSAGE", new string[] { "Difficulty settings overriden due to being an admin" }));
                return true;
            }

            if (SessionValues.DifficultyFile.EnforceDifficulty || SessionValues.StorytellerFile.EnforceStoryteller)
            {
                Action toDo = delegate
                {
                    GameParameterManager.SetStoryteller(SessionValues.StorytellerFile);
                    DifficultyManager.SetDifficulty(SessionValues.DifficultyFile);
                };

                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("MESSAGE", new string[] { "Settings might change to reflect server enforcements" }, toDo));

                return false;
            }

            return true;
        }
    }
}
