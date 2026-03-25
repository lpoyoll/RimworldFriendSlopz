using GameClient.Core.Configs;
using GameClient.Defs;
using GameClient.Dialogs;
using GameClient.Misc;
using GameClient.PacketManagers;
using GameClient.Tabs;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            Rect chatRect = new Rect(new Vector2(UI.screenWidth - ButtonSize.x - 2f, 2f), ButtonSize);
            if (Widgets.ButtonImageWithBG(chatRect, RTChatDefs.Chat))
            {
                if (!TAB_Chat.IsTabOpen) Find.WindowStack.Add(new TAB_Chat());
                else TAB_Chat.Instance.Close();
            }

            Rect optionsRect = new Rect(new Vector2(chatRect.x - ButtonSize.x - 2f, 2f), ButtonSize);
            if (Widgets.ButtonImageWithBG(optionsRect, RTChatDefs.Options))
            {
                if (!TAB_Options.IsTabOpen) Find.WindowStack.Add(new TAB_Options());
                else TAB_Options.Instance.Close();
            }

            string text = $"{Math.Abs(PM_KeepAlive.CurrentPing)} ms";
            Vector2 size = Text.CalcSize(text);
            Vector2 position = new Vector2(UI.screenWidth - size.x - DLG_Base.DefaultMargin, LatencyHeight);
            Widgets.Label(new Rect(position, size), text);

            return true;
        }
    }
}
