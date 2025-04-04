using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameClient.Managers;
using GameClient.Misc;
using GameClient.TCP;
using Shared.MasterServer;
using UnityEngine;
using Verse;
using static Mono.Security.X509.X520;

namespace GameClient.Dialogs
{
    public class RT_Dialog_ServerListingInfo : Window
    {
        private Vector2 initialSize = new Vector2(600f, 250f);
        public override Vector2 InitialSize => initialSize;
        private ServerInfo info;
        public RT_Dialog_ServerListingInfo(ServerInfo info) 
        {
            this.info = info;
        }
        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Vector2 titleSize = Text.CalcSize(info._name);
            float centeredx = inRect.width / 2;
            Rect titleRect = new Rect(centeredx - titleSize.x / 2, inRect.y, titleSize.x, titleSize.y);
            Widgets.Label(titleRect, info._name);

            Widgets.DrawLineHorizontal(0, titleSize.y + 3f, inRect.width);

            Text.Font = GameFont.Small;
            Rect descriptionRect = new Rect(inRect.x, titleSize.y + 6f, inRect.width / 3 * 2, inRect.height - 55f);
            Widgets.Label(descriptionRect, info._description);

            Rect connectRect = new Rect(inRect.width - 110f, 250f, 100f, 45f);

            if(Widgets.ButtonText(connectRect, "Connect")) 
            {
                Network.ip = info._ip;
                Network.port = info._port.ToString();

                DialogManager.PushNewDialog(new RT_Dialog_Wait("Trying to connect to server"));
                Threader.GenerateThread(Threader.Mode.Start);
                Close();
            }
        }
    }
}
