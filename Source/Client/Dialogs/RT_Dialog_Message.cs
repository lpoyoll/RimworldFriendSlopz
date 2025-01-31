using System;
using GameClient.Managers;
using RimWorld;
using UnityEngine;
using Verse;
using static GameClient.Managers.DialogManagerH;

namespace GameClient.Dialogs
{
    public class RT_Dialog_Message : Window
    {
        public override Vector2 InitialSize => new Vector2(500f, 150f);

        private readonly string title = "MESSAGE";

        private string currentMessage;

        private readonly string[] messages;

        private int index = 0;

        private readonly Action onConfirm;

        public RT_Dialog_Message(string title, string[] messages, Action onConfirm = null)
        {
            DialogManager.dialogMessage = this;

            this.title = title;
            this.messages = messages;
            this.onConfirm = onConfirm;
            currentMessage = messages[index];

            forcePause = true;
            absorbInputAroundWindow = true;
            soundAppear = SoundDefOf.CommsWindow_Open;

            closeOnAccept = false;
            closeOnCancel = false;
        }

        public override void DoWindowContents(Rect rect)
        {
            float centeredX = rect.width / 2;
            float horizontalLineDif = Text.CalcSize(currentMessage).y + StandardMargin / 2;
            float windowDescriptionDif = Text.CalcSize(currentMessage).y + StandardMargin;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(centeredX - Text.CalcSize(title).x / 2, rect.y, Text.CalcSize(title).x, Text.CalcSize(title).y), title);
            Widgets.DrawLineHorizontal(rect.x, horizontalLineDif, rect.width);
            Text.Font = GameFont.Small;

            Widgets.Label(new Rect(centeredX - Text.CalcSize(currentMessage).x / 2, windowDescriptionDif, 
                Text.CalcSize(currentMessage).x, Text.CalcSize(currentMessage).y), currentMessage);

            if (Widgets.ButtonText(GetRectForLocation(rect, defaultButtonSize, RectLocation.BottomCenter), "OK"))
            {
                if (index < messages.Length - 1)
                {
                    index++;
                    currentMessage = messages[index];
                }

                else
                {
                    onConfirm?.Invoke();
                    Close();
                }
            }
        }
    }
}
