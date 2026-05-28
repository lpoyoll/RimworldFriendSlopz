using RTShared.Misc;
using System;
using UnityEngine;
using Verse;

namespace GameClient.Dialogs.Default
{
    public class DLG_Wait : DLG_Base
    {
        public override Vector2 InitialSize => new Vector2(300f, 95f);

        public static DLG_Base Instance { get; private set; } = null;

        private DateTime PreviousTime { get; set; } = DateTime.Now;

        public DLG_Wait()
        {
            Instance = this;
            this.Title = "WAIT";
            this.Description = "...";
        }

        public override void DoWindowContents(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(DLG_Base.GetRectMiddle(rect) - Text.CalcSize(Title).x / 2, rect.y, Text.CalcSize(Title).x, Text.CalcSize(Title).y), Title);

            Widgets.DrawLineHorizontal(rect.x, 37, rect.width);

            Animate();
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(DLG_Base.GetRectMiddle(rect) - Text.CalcSize(Description).x / 2, Text.CalcSize(Description).y + 18f, Text.CalcSize(Description).x, Text.CalcSize(Description).y), Description);
        }

        private void Animate()
        {
            DateTime currentTime = DateTime.Now;

            if (currentTime - PreviousTime > TimeSpan.FromSeconds(0.5f))
            {
                if (Description.Length < 3) Description += ".";
                else Description = ".";

                PreviousTime = currentTime;
            }
        }
    }
}
