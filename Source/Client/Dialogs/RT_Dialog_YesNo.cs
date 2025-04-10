using System;
using GameClient.Managers;
using RimWorld;
using UnityEngine;
using Verse;

namespace GameClient.Dialogs
{
    public class RT_Dialog_YesNo : RT_Dialog_Base
    {
        public override Vector2 InitialSize => new Vector2(400f, 150f);

        public RT_Dialog_YesNo(string description, Action actionYes, Action actionNo = null)
        {
            this.Title = "OPTION";
            this.Description = description;
            this.OnAccept = actionYes;
            this.OnCancel = actionNo;

            closeOnAccept = false;
            closeOnCancel = false;
        }

        public override void DoWindowContents(Rect rect)
        {
            float centeredX = rect.width / 2;
            float horizontalLineDif = Text.CalcSize(Description).y + StandardMargin / 2;
            float windowDescriptionDif = Text.CalcSize(Description).y + StandardMargin;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(centeredX - Text.CalcSize(Title).x / 2, rect.y, Text.CalcSize(Title).x, Text.CalcSize(Title).y), Title);

            Widgets.DrawLineHorizontal(rect.x, horizontalLineDif, rect.width);

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(centeredX - Text.CalcSize(Description).x / 2, windowDescriptionDif, Text.CalcSize(Description).x, Text.CalcSize(Description).y), Description);

            if (Widgets.ButtonText(GetRectForLocation(rect, SmallerButtonSize, RectLocation.BottomLeft), "Yes"))
            {
                OnAccept?.Invoke();
                Close();
            }

            if (Widgets.ButtonText(GetRectForLocation(rect, SmallerButtonSize, RectLocation.BottomRight), "No"))
            {
                OnCancel?.Invoke();
                Close();
            }
        }
    }
}
