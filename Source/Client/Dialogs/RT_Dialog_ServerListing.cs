using System.Linq;
using GameClient.Managers;
using GameClient.Misc;
using Rimworld_Together_Master_Server.Data;
using TCPNetwork.Packets;
using Shared;
using UnityEngine;
using Verse;
using Reachability = Rimworld_Together_Master_Server.Data.Reachability;

namespace GameClient.Dialogs
{
    public class RT_Dialog_ServerListing : RT_Dialog_Base
    {
        public override Vector2 InitialSize => new Vector2(650f, 400f);

        public static RT_Dialog_Base Instance { get; private set; }

        private bool FailedToFetchServers { get; set; } = false;

        public RT_Dialog_ServerListing()
        {
            if (!GetServers()) FailedToFetchServers = true;

            Instance = this;
            this.Title = "Server Browser";
            this.Description = "This is a list of all publicly available servers!";

            closeOnAccept = false;
            closeOnCancel = true;
        }

        private bool GetServers() 
        {
            ServerBrowserManager.GetAllServersAvailable();

            var servers = ServerBrowserManager.AllServers;
            
            if (servers == null || servers.Length == 0) return false;
            else
            {
                Printer.Warning($"Found {servers.Count()} servers in the server browser", CommonEnumerators.LogImportanceMode.Verbose);

                return true;
            }
        }

        public override void DoWindowContents(Rect rect)
        {
            if (FailedToFetchServers) Close();

            float centeredX = rect.width / 2;
            float windowDescriptionDif = Text.CalcSize(base.Description).y + StandardMargin;
            float descriptionLineDif1 = windowDescriptionDif - Text.CalcSize(base.Description).y * 0.25f;
            float descriptionLineDif2 = windowDescriptionDif + Text.CalcSize(base.Description).y * 1.1f;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(centeredX - Text.CalcSize(base.Title).x / 2, rect.y, Text.CalcSize(base.Title).x, Text.CalcSize(base.Title).y), base.Title);

            Widgets.DrawLineHorizontal(rect.x, descriptionLineDif1, rect.width);

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(centeredX - Text.CalcSize(base.Description).x / 2, windowDescriptionDif, Text.CalcSize(base.Description).x, Text.CalcSize(base.Description).y), base.Description);

            Text.Font = GameFont.Medium;
            Widgets.DrawLineHorizontal(rect.x, descriptionLineDif2, rect.width);

            FillMainRect(new Rect(0f, descriptionLineDif2 + 10f, rect.width, rect.height - DefaultButtonSize.y - 85f));

            Text.Font = GameFont.Small;
            if (Widgets.ButtonText(new Rect(new Vector2(centeredX - DefaultButtonSize.x / 2, rect.yMax - DefaultButtonSize.y), DefaultButtonSize), "Close")) Close();
        }

        private void FillMainRect(Rect mainRect)
        {
            var servers = ServerBrowserManager.AllServers
                .ToList()
                .OrderByDescending(x => x._currentPlayerCount)
                .Where(x => x.Reachability == Reachability.Reachable && x._version == CommonValues.ExecutableVersion)
                .ToArray();
            
            float height = 6f + servers.Length * 30f;
            Rect viewRect = new Rect(0f, 0f, mainRect.width - 16f, height);
            Widgets.BeginScrollView(mainRect, ref base.ScrollPosition, viewRect);
            float num = 0;
            float num2 = base.ScrollPosition.y - 30f;
            float num3 = base.ScrollPosition.y + mainRect.height;
            int num4 = 0;

            for (int i = 0; i < servers.Length; i++)
            {
                var server = servers[i];
                if (num > num2 && num < num3)
                {
                    Rect rect = new Rect(0f, num, viewRect.width, 30f);
                    DrawCustomRow(rect, server , num4);
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

            Widgets.Label(fixedRect, $"{server._name} - {server._ip} - {server._currentPlayerCount} / {server._maximumPlayerCount}");
            if (Widgets.ButtonText(new Rect(new Vector2(rect.xMax - SmallerButtonSize.x - 5f, rect.yMax - TinyButtonSize.y), new Vector2(SmallerButtonSize.x, TinyButtonSize.y)), "Select"))
            {
                RT_Dialog_Base.PushNewDialog(new RT_Dialog_ServerListingInfo(server));
            }
        }

        private void ResetWindow() { RT_Dialog_Base.PushNewDialog(new RT_Dialog_ServerListing()); }
    }
}
