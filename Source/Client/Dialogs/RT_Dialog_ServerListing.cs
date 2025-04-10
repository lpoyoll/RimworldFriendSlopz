using System.Linq;
using GameClient.Core.Preferences;
using GameClient.Files;
using GameClient.Managers;
using GameClient.Misc;
using GameClient.TCP;
using RimWorld;
using UnityEngine;
using Verse;

namespace GameClient.Dialogs
{
    public class RT_Dialog_ServerListing : RT_Dialog_Base
    {
        public override Vector2 InitialSize => new Vector2(650f, 400f);

        public RecentServersFile recentServers => RecentServersHandler.LoadRecentServers();

        public static int dialogServerListingIndex;

        public RT_Dialog_ServerListing()
        {
            this.Title = "Recent servers";
            this.Description = "This list shows the servers you last joined";

            closeOnAccept = false;
            closeOnCancel = false;
        }

        public override void DoWindowContents(Rect rect)
        {
            float centeredX = rect.width / 2;

            float windowDescriptionDif = Text.CalcSize(Description).y + StandardMargin;
            float descriptionLineDif1 = windowDescriptionDif - Text.CalcSize(Description).y * 0.25f;
            float descriptionLineDif2 = windowDescriptionDif + Text.CalcSize(Description).y * 1.1f;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(centeredX - Text.CalcSize(Title).x / 2, rect.y, Text.CalcSize(Title).x, Text.CalcSize(Title).y), Title);

            Widgets.DrawLineHorizontal(rect.x, descriptionLineDif1, rect.width);
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(centeredX - Text.CalcSize(Description).x / 2, windowDescriptionDif, Text.CalcSize(Description).x, Text.CalcSize(Description).y), Description);
            Text.Font = GameFont.Medium;
            Widgets.DrawLineHorizontal(rect.x, descriptionLineDif2, rect.width);

            FillMainRect(new Rect(0f, descriptionLineDif2 + 10f, rect.width, rect.height - DefaultButtonSize.y - 85f));

            Text.Font = GameFont.Small;
            if (Widgets.ButtonText(new Rect(new Vector2(centeredX - DefaultButtonSize.x / 2, rect.yMax - DefaultButtonSize.y), DefaultButtonSize), "Close")) Close();
        }

        private void FillMainRect(Rect mainRect)
        {
            float height = 6f + recentServers.ServerAddresses.Count() * 30f;
            Rect viewRect = new Rect(0f, 0f, mainRect.width - 16f, height);
            Widgets.BeginScrollView(mainRect, ref ScrollPosition, viewRect);
            float num = 0;
            float num2 = ScrollPosition.y - 30f;
            float num3 = ScrollPosition.y + mainRect.height;
            int num4 = 0;

            for (int i = 0; i < recentServers.ServerAddresses.Count(); i++)
            {
                if (num > num2 && num < num3)
                {
                    Rect rect = new Rect(0f, num, viewRect.width, 30f);
                    DrawCustomRow(rect, recentServers.ServerNames[i], recentServers.ServerAddresses[i], num4);
                }

                num += 30f;
                num4++;
            }

            Widgets.EndScrollView();
        }

        private void DrawCustomRow(Rect rect, string serverName, string serverAddress, int index)
        {
            Text.Font = GameFont.Small;
            Rect fixedRect = new Rect(new Vector2(rect.x, rect.y + 5f), new Vector2(rect.width - 16f, rect.height - 5f));
            if (index % 2 == 0) Widgets.DrawHighlight(fixedRect);

            Widgets.Label(fixedRect, $"{serverName} - {serverAddress}");
            if (Widgets.ButtonText(new Rect(new Vector2(rect.xMax - TinyButtonSize.x - TinyButtonSize.x - 5f, rect.yMax - TinyButtonSize.y), TinyButtonSize), "Select"))
            {
                Network.ip = serverAddress.Split(':')[0];
                Network.port = serverAddress.Split(':')[1];

                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Wait("Trying to connect to server"));
                Threader.GenerateThread(Threader.Mode.Start);
                Close();
            }

            if (Widgets.ButtonText(new Rect(new Vector2(rect.xMax - TinyButtonSize.x, rect.yMax - TinyButtonSize.y), TinyButtonSize), "Delete"))
            {
                dialogServerListingIndex = index;

                RecentServersHandler.RemoveServerFromList(serverName, serverAddress);

                ResetWindow();
            }
        }

        private void ResetWindow() { RT_Dialog_Base.PushNewDialog(new RT_Dialog_ServerListing()); }
    }
}
