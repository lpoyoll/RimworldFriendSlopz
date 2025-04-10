using System;
using GameClient.Managers;
using RimWorld;
using UnityEngine;
using Verse;

namespace GameClient.Dialogs
{
    public class RT_Dialog_Message : RT_Dialog_Base
    {
        public override Vector2 InitialSize => new Vector2(500f, 150f);

        private string currentMessage;

        private readonly string[] messages;

        private int index = 0;

        public RT_Dialog_Message(string title, string[] messages, Action onConfirm = null)
        {
            this.Title = title;
            this.messages = messages;
            this.OnAccept = onConfirm;
            currentMessage = messages[index];

            closeOnAccept = false;
            closeOnCancel = false;
        }

        public override void DoWindowContents(Rect rect)
        {
            float centeredX = rect.width / 2;
            float horizontalLineDif = Text.CalcSize(currentMessage).y + StandardMargin / 2;
            float windowDescriptionDif = Text.CalcSize(currentMessage).y + StandardMargin;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(centeredX - Text.CalcSize(Title).x / 2, rect.y, Text.CalcSize(Title).x, Text.CalcSize(Title).y), Title);
            Widgets.DrawLineHorizontal(rect.x, horizontalLineDif, rect.width);
            Text.Font = GameFont.Small;

            Widgets.Label(new Rect(centeredX - Text.CalcSize(currentMessage).x / 2, windowDescriptionDif, 
                Text.CalcSize(currentMessage).x, Text.CalcSize(currentMessage).y), currentMessage);

            if (Widgets.ButtonText(GetRectForLocation(rect, DefaultButtonSize, RectLocation.BottomCenter), "OK"))
            {
                if (index < messages.Length - 1)
                {
                    index++;
                    currentMessage = messages[index];
                }

                else
                {
                    OnAccept?.Invoke();
                    Close();
                }
            }
        }
    }
}
