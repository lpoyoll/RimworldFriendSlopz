using System;
using GameClient.Managers;
using RimWorld;
using UnityEngine;
using Verse;
using static GameClient.Managers.DialogManagerH;

namespace GameClient.Dialogs
{
    public class RT_Dialog_YesNo : Window
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
            float horizontalLineDif = Text.CalcSize(description).y + StandardMargin / 2;
            float windowDescriptionDif = Text.CalcSize(description).y + StandardMargin;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(centeredX - Text.CalcSize(title).x / 2, rect.y, Text.CalcSize(title).x, Text.CalcSize(title).y), title);

            Widgets.DrawLineHorizontal(rect.x, horizontalLineDif, rect.width);

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(centeredX - Text.CalcSize(description).x / 2, windowDescriptionDif, Text.CalcSize(description).x, Text.CalcSize(description).y), description);

            if (Widgets.ButtonText(GetRectForLocation(rect, defaultButtonSize, RectLocation.BottomLeft), yText))
            {
                actionYes?.Invoke();
                Close();
            }

            if (Widgets.ButtonText(GetRectForLocation(rect, defaultButtonSize, RectLocation.BottomRight), nText))
            {
                actionNo?.Invoke();
                Close();
            }
        }
    }
}
