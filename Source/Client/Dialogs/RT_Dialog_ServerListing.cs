using System.Linq;
using GameClient.Core.Preferences;
using GameClient.Files;
using GameClient.Managers;
using GameClient.Misc;
using GameClient.TCP;
using RimWorld;
using Shared;
using UnityEngine;
using Verse;

namespace GameClient.Dialogs
{
    public class RT_Dialog_ServerListing : RT_Dialog_Base
    {
        public override Vector2 InitialSize => new Vector2(650f, 400f);

        public readonly string title = "Server Browser";

        public readonly string description = "This is a list of all publicly available servers!";

        private Vector2 scrollPosition = Vector2.zero;

        private readonly Vector2 button = new Vector2(150f, 38f);

        private readonly Vector2 selectButton = new Vector2(47f, 25f);

        private readonly Vector2 deleteButton = new Vector2(47f, 25f);

        public RecentServersFile recentServers => RecentServersHandler.LoadRecentServers();

        public ServerInfo[] AllServers = new ServerInfo[0];

        public static RT_Dialog_Base Instance { get; private set; }

        private bool failedToFetchServers = false;
        public RT_Dialog_ServerListing()
        {
            if (!GetServers()) failedToFetchServers = true;

            Instance = this;
            this.Title = title;
            this.Description = description;
            closeOnAccept = false;
            closeOnCancel = false;
        }

        private bool GetServers() 
        {
            ServerInfo[] servers = ServerBrowserManager.GetAllServersAvailable();
            if (servers == null)
                return false;
            AllServers = servers;
            return true;
        }

        public override void DoWindowContents(Rect rect)
        {
            if (failedToFetchServers)
                Close();
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

            for (int i = 0; i < AllServers.Length; i++)
            {
                if (num > num2 && num < num3)
                {
                    Rect rect = new Rect(0f, num, viewRect.width, 30f);
                    DrawCustomRow(rect, AllServers[i], num4);
                }

                num += 30f;
                num4++;
            }

            Widgets.EndScrollView();
        }

        private void DrawCustomRow(Rect rect, ServerInfo server, int index)
        {
            Text.Font = GameFont.Small;
            Rect fixedRect = new Rect(new Vector2(rect.x, rect.y + 5f), new Vector2(rect.width - 16f, rect.height - 5f));
            if (index % 2 == 0) Widgets.DrawHighlight(fixedRect);

            Widgets.Label(fixedRect, $"{server._name} - {server._ip}");
            if (Widgets.ButtonText(new Rect(new Vector2(rect.xMax - selectButton.x - deleteButton.x - 5f, rect.yMax - selectButton.y), selectButton), "Select"))
            {
                RT_Dialog_Base.PushNewDialog(new RT_Dialog_ServerListingInfo(server));
            }
        }

        private void ResetWindow() { RT_Dialog_Base.PushNewDialog(new RT_Dialog_ServerListing()); }
    }
}
