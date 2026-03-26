using GameClient.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace GameClient.Tabs
{
    public class TAB_Minichat : DLG_Base
    {
        public static Vector2 Position = new Vector2((UI.screenWidth / 2) - 150f, UI.screenHeight - 100f - 25f);

        public override Vector2 InitialSize => new Vector2(300f, 100f);

        public TAB_Minichat() 
        {
            layer = WindowLayer.Super;

            forcePause = false;
            drawShadow = false;
            doWindowBackground = false;
            preventCameraMotion = false;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = false;
        }

        public override void DoWindowContents(Rect rect)
        {
            windowRect.x = Position.x;
            windowRect.y = Position.y;

            GUI.color = new Color(0f, 0f, 0f, 0.15f);
            Widgets.DrawBoxSolid(rect, GUI.color);
            GUI.color = Color.white;

            Widgets.Label(rect, "Transparent dialog");
        }
    }
}
