using GameClient.Managers;
using GameClient.PacketManagers;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GameClient.Patches
{
    [HarmonyPatch(typeof(MainMenuDrawer), "DoMainMenuControls")]
    public static class Patch_MainMenuDrawer_DoMainMenuControls
    {
        [HarmonyPrefix]
        public static bool DoPre()
        {
            if (Current.ProgramState == ProgramState.Playing)
            {
                Vector2 buttonSize = new Vector2(170f, 45f);

                if (Widgets.ButtonText(new Rect(0, (buttonSize.y + 7) * 2, buttonSize.x, buttonSize.y), ""))
                {
                    Find.MainTabsRoot.EscapeCurrentTab(playSound: false);
                    SessionManager.IsExiting = true;
                    PM_Saves.ForceSave();
                }

                if (Widgets.ButtonText(new Rect(0, (buttonSize.y + 7) * 3, buttonSize.x, buttonSize.y), ""))
                {
                    Find.MainTabsRoot.EscapeCurrentTab(playSound: false);
                    SessionManager.IsExiting = true;
                    PM_Saves.ForceSave();
                }
            }

            return true;
        }
    }
}
