using GameClient.Dialogs.Default;
using GameClient.Hooks.TCPNetwork;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCPNetwork;
using UnityEngine;
using Verse;

namespace GameClient.Dialogs
{
    public class DLG_Login : DLG_Base
    {
        public override Vector2 InitialSize => new Vector2(350f, 240f);

        private string StringIP { get; set; } = "IP";

        private string StringPort { get; set; } = "Port";

        private string EndpointIP { get; set; } = string.Empty;

        private string EndpointPort { get; set; } = "25555";

        private int MaxChars { get; set; } = 30;

        public DLG_Login() { this.Title = "Direct Connect"; }

        public override void DoWindowContents(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(DLG_Base.GetRectMiddle(rect) - Text.CalcSize(Title).x / 2, rect.y, Text.CalcSize(Title).x, Text.CalcSize(Title).y), Title);
            Widgets.DrawLineHorizontal(rect.x, 30f, rect.width);
            Text.Font = GameFont.Small;

            Widgets.Label(new Rect((DLG_Base.GetRectMiddle(rect) - Text.CalcSize(StringIP).x / 2), 40f, Text.CalcSize(StringIP).x, 25f), StringIP);
            string input = Widgets.TextField(new Rect(DLG_Base.GetRectMiddle(rect) - rect.width / 2, 65f, rect.width, 25f), EndpointIP);
            if (AcceptsInput && input.Length <= MaxChars) EndpointIP = input;

            Widgets.Label(new Rect((DLG_Base.GetRectMiddle(rect) - Text.CalcSize(StringPort).x / 2), 100f, Text.CalcSize(StringPort).x, 25f), StringPort);
            input = Widgets.TextField(new Rect(DLG_Base.GetRectMiddle(rect) - rect.width / 2, 125f, rect.width, 25f), EndpointPort);
            if (AcceptsInput && input.Length <= MaxChars) EndpointPort = input;

            if (Widgets.ButtonText(GetRectForLocation(rect, SmallButtonSize, RectLocation.BottomLeft), "Confirm"))
            {
                Network.Ip = EndpointIP;
                Network.Port = int.Parse(EndpointPort);
                ClientNetwork _ = new ClientNetwork();

                Close();
            }

            if (Widgets.ButtonText(GetRectForLocation(rect, SmallButtonSize, RectLocation.BottomRight), "Cancel")) Close();
        }
    }
}
