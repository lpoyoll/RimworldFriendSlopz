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

        private readonly string title = "OPTION";

        private readonly string description;

        private readonly Action actionYes;

        private readonly Action actionNo;

        private readonly string nText;

        private readonly string yText;
        public RT_Dialog_YesNo(string description, Action actionYes, Action actionNo = null, string yText = "Yes", string nText = "No")
        {
            DialogManager.dialogYesNo = this;
            this.description = description;
            this.actionYes = actionYes;
            this.actionNo = actionNo;
            this.yText = yText;
            this.nText = nText;
            forcePause = true;
            absorbInputAroundWindow = true;
            soundAppear = SoundDefOf.CommsWindow_Open;

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

            if (Widgets.ButtonText(GetRectForLocation(rect, defaultButtonSize, RectLocation.BottomLeft), yText))
            {
                OnAccept?.Invoke();
                Close();
            }

            if (Widgets.ButtonText(GetRectForLocation(rect, defaultButtonSize, RectLocation.BottomRight), nText))
            {
                OnCancel?.Invoke();
                Close();
            }
        }
    }
}
