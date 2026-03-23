using System;
using UnityEngine;
using Verse;

namespace GameClient.Dialogs.Default
{
    public class DLG_YesNo : DLG_Base
    {
        public override Vector2 InitialSize => new Vector2(400f, 150f);

        private string YesText { get; set; }

        private string NoText { get; set; }
        
        private Color YesColor { get; set; }
        
        private Color NoColor { get; set; }

        public DLG_YesNo(string description, Action actionYes, Action actionNo = null, 
            string yText = "Yes", string nText = "No", Color? yesColor = null, Color? noColor = null)
        {
            this.Title = "OPTION";
            this.Description = description;
            this.OnAccept = actionYes;
            this.OnCancel = actionNo;
            this.YesText = yText;
            this.NoText = nText;

            if(yesColor == null)
                yesColor = Color.white;
            if(noColor == null)
                noColor = Color.white;
            
            YesColor = yesColor.Value;
            NoColor = noColor.Value;
            
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

            GUI.color = YesColor;
            if (Widgets.ButtonText(GetRectForLocation(rect, SmallButtonSize, RectLocation.BottomLeft), YesText))
            {
                OnAccept?.Invoke();
                Close();
            }
            GUI.color = NoColor;
            if (Widgets.ButtonText(GetRectForLocation(rect, SmallButtonSize, RectLocation.BottomRight), NoText))
            {
                OnCancel?.Invoke();
                Close();
            }
            GUI.color = Color.white;
        }
    }
}
