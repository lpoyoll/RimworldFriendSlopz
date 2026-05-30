using GameClient.Defs;
using GameClient.Dialogs;
using GameClient.Managers;
using HarmonyLib;
using RimWorld;
using RTNetwork.PacketManagers;
using UnityEngine;
using Verse;

namespace GameClient.Patches
{
    [HarmonyPatch(typeof(GlobalControls), nameof(GlobalControls.GlobalControlsOnGUI))]
    public static class Patch_GlobalControls_GlobalControlsOnGUI
    {
        private static Vector2 ButtonSize { get; set; } = new Vector2(35f, 35f);

        private static float LatencyHeight { get; set; } = 40f;

        [HarmonyPrefix]
        public static bool DoPre()
        {
            DrawChatButton(new Rect(new Vector2(UI.screenWidth - ButtonSize.x - 2f, 2f), ButtonSize));

            DrawOptionsButton(new Rect(new Vector2(UI.screenWidth - (ButtonSize.x * 2) - 4f, 2f), ButtonSize));

            DrawAdminButton(new Rect(new Vector2(UI.screenWidth - (ButtonSize.x * 3) - 6f, 2f), ButtonSize));

            DrawLatencyText();

            return true;
        }

        private static void DrawChatButton(Rect rect)
        {
            if (!DLG_Chat.IsDialogOpen)
            {
                if (Widgets.ButtonImageWithBG(rect, RTTextureDefs.ChatOn)) Find.WindowStack.Add(new DLG_Chat());
            }

            else
            {
                if (Widgets.ButtonImageWithBG(rect, RTTextureDefs.ChatOff)) DLG_Chat.Instance.Close();
            }
        }

        private static void DrawOptionsButton(Rect rect)
        {
            if (!DLG_Options.IsDialogOpen)
            {
                if (Widgets.ButtonImageWithBG(rect, RTTextureDefs.OptionsOn)) Find.WindowStack.Add(new DLG_Options());
            }

            else
            {
                if (Widgets.ButtonImageWithBG(rect, RTTextureDefs.OptionsOff)) DLG_Options.Instance.Close();
            }
        }

        private static void DrawAdminButton(Rect rect)
        {
            if (!SessionManager.IsAdmin) return;

            if (!DLG_Admin.IsDialogOpen)
            {
                if (Widgets.ButtonImageWithBG(rect, RTTextureDefs.AdminOn)) Find.WindowStack.Add(new DLG_Admin());
            }

            else
            {
                if (Widgets.ButtonImageWithBG(rect, RTTextureDefs.AdminOff)) DLG_Admin.Instance.Close();
            }
        }

        private static void DrawLatencyText()
        {
            string text = $"{PM_KeepAlive.CurrentPing} ms";
            Vector2 size = Text.CalcSize(text);
            Vector2 position = new Vector2(UI.screenWidth - size.x - DLG_Base.DefaultMargin, LatencyHeight);
            Widgets.Label(new Rect(position, size), text);
        }
    }
}
