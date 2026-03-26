using GameClient.Core.Configs;
using GameClient.Defs;
using GameClient.Dialogs;
using GameClient.Managers;
using GameClient.PacketManagers;
using RimWorld;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace GameClient.Tabs
{
    public class TAB_Chat : DLG_Base
    {
        public static Vector2 ChatBoxPosition = new Vector2(0, UI.screenHeight - 35f - 400f);

        public override Vector2 InitialSize => new Vector2(600f, 400f);

        private Vector2 scrollPositionChat = Vector2.zero;

        public static TAB_Chat Instance { get; private set; } = null;

        public static bool IsTabOpen { get; set; } = false;

        public static bool ShouldScrollChat { get; set; } = true;

        public static bool ShouldPlaySounds { get; set; } = false;

        public static string CurrentChatInput { get; set; } = string.Empty;

        public static List<string> ChatMessages { get; set; } = new List<string>();

        public TAB_Chat()
        {
            layer = WindowLayer.GameUI;
            Instance = this;

            draggable = true;
            forcePause = false;
            preventCameraMotion = false;
            absorbInputAroundWindow = false;
        }

        public override void PreOpen()
        {
            base.PreOpen();

            windowRect.x = ChatBoxPosition.x;
            windowRect.y = ChatBoxPosition.y;
        }

        public override void PostOpen()
        {
            base.PostOpen();
            IsTabOpen = true;
        }

        public override void PostClose()
        {
            base.PostClose();

            ChatBoxPosition.x = windowRect.x;
            ChatBoxPosition.y = windowRect.y;

            IsTabOpen = false;
        }

        public override void DoWindowContents(Rect rect)
        {
            DrawPlayerCount(rect);
            if (Widgets.ButtonText(new Rect(new Vector2(rect.width - LongButtonSize.x, rect.y), LongButtonSize), "Tools")) DrawToolsButton();
            DrawPinCheckbox(new Rect(rect.width - LongButtonSize.x - 30f, rect.y, 25f, 25f));
            DrawMuteCheckbox(new Rect(rect.width - LongButtonSize.x - (30f * 2), rect.y, 25f, 25f));

            Widgets.DrawLineHorizontal(rect.x, rect.y + LongButtonSize.y + 5f, rect.width);
            DrawMessageList(new Rect(rect.x, rect.y + 32f, rect.width, rect.height - 60f));

            DrawInput(new Rect(rect.xMin, rect.yMax - 25f, rect.width, 25f));
            CheckForEnterKey();

            if (TAB_Chat.ShouldScrollChat) ScrollToLastMessage();
        }

        private void DrawToolsButton()
        {
            List<Tuple<string, string>> modes = new List<Tuple<string, string>>()
            {
                Tuple.Create("Whisper", "/w "),
                Tuple.Create("Bold", "[b]"),
                Tuple.Create("Italic", "[i]"),
            };

            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (Tuple<string, string> tuple in modes)
            {
                FloatMenuOption item = new FloatMenuOption(tuple.Item1, delegate
                {
                    CurrentChatInput += tuple.Item2;
                });

                list.Add(item);
            }

            Find.WindowStack.Add(new FloatMenu(list));
        }

        private void DrawPlayerCount(Rect rect)
        {
            string toShow = PM_Recount.CurrentPlayers > 1 ? $"{PM_Recount.CurrentPlayers} Players" : $"{PM_Recount.CurrentPlayers} Player";

            Text.Font = GameFont.Small;
            Widgets.Label(new(rect.x, rect.y, Text.CalcSize(toShow).x, Text.CalcSize(toShow).y), $"<color=grey>{toShow}</color>");
        }

        private void DrawMessageList(Rect mainRect)
        {
            float height = 6f;
            float heightCalcWidthOffset = 160f;
            float chatScrollbarSafezone = 30f;

            foreach (string str in ChatMessages.ToArray()) height += Text.CalcHeight(str, mainRect.width - chatScrollbarSafezone);

            Rect viewRect = new(mainRect.x, mainRect.y, mainRect.width - chatScrollbarSafezone, height);

            Widgets.BeginScrollView(mainRect, ref scrollPositionChat, viewRect);

            float num = 0;
            float num2 = scrollPositionChat.y - chatScrollbarSafezone;
            float num3 = scrollPositionChat.y + mainRect.height;

            foreach (string str in ChatMessages.ToArray())
            {
                if (num > num2 && num < num3)
                {
                    Rect rect2 = new Rect(mainRect.x, mainRect.y + num, viewRect.width, 
                        Text.CalcHeight(str, mainRect.width - heightCalcWidthOffset - chatScrollbarSafezone));

                    DrawCustomRow(rect2, str);
                }

                num += Text.CalcHeight(str, mainRect.width - chatScrollbarSafezone);
            }

            Widgets.EndScrollView();
        }

        private void DrawInput(Rect rect)
        {
            Text.Font = GameFont.Small;
            string inputOne = Widgets.TextField(rect, CurrentChatInput);
            if (inputOne.Length <= 512) CurrentChatInput = inputOne;
        }

        private void DrawPinCheckbox(Rect rect)
        {
            Action toDo = delegate 
            { 
                ShouldScrollChat = !ShouldScrollChat;
                SoundDefOf.Click.PlayOneShotOnCamera();
            };

            if (ShouldScrollChat)
            {
                if (Widgets.ButtonImage(rect, RTTextureDefs.PinOn, true, "Pin chat")) toDo();
            }

            else
            {
                if (Widgets.ButtonImage(rect, RTTextureDefs.PinOff, true, "Unpin chat")) toDo();
            }
        }

        private void DrawMuteCheckbox(Rect rect)
        {
            Action toDo = delegate
            {
                TAB_Chat.ShouldPlaySounds = !TAB_Chat.ShouldPlaySounds;
                SoundDefOf.Click.PlayOneShotOnCamera();
            };

            if (TAB_Chat.ShouldPlaySounds)
            {
                if (Widgets.ButtonImage(rect, RTTextureDefs.SoundOn, true, "Unmute sounds")) toDo();
            }

            else
            {
                if (Widgets.ButtonImage(rect, RTTextureDefs.SoundOff, true, "Mute sounds")) toDo();
            }
        }

        private void CheckForEnterKey()
        {
            bool keyPressed = !string.IsNullOrWhiteSpace(CurrentChatInput) && (Event.current.keyCode == KeyCode.Return ||
                Event.current.keyCode == KeyCode.KeypadEnter);

            if (keyPressed)
            {
                PM_Chat.SendMessage(CurrentChatInput);
                CurrentChatInput = "";
            }
        }

        private void ScrollToLastMessage() { scrollPositionChat.Set(scrollPositionChat.x, scrollPositionChat.y + Mathf.Infinity); }

        private void DrawCustomRow(Rect rect, string message)
        {
            Text.Font = GameFont.Small;
            Rect fixedRect = new(rect.x + 10f, rect.y + 5f, rect.width, rect.height);
            Widgets.Label(fixedRect, message);
        }
    }
}
