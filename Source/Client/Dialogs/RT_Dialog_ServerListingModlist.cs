using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using GameClient.Core.Configs;
using GameClient.Managers;
using GameClient.Misc;
using Shared;
using Steamworks;
using UnityEngine;
using Verse;

namespace GameClient.Dialogs
{
    public class RT_Dialog_ServerListingModlist : RT_Dialog_Base
    {
        private ServerInfo ServerInfo { get; set; }

        private List<string> DownloadedMods = new List<string>();
        public override Vector2 InitialSize => new Vector2(550f, 700f);
        public RT_Dialog_ServerListingModlist(string title, string description, ServerInfo serverInfo) 
        {
            this.Title = title;
            this.Description = description;
            this.ServerInfo = serverInfo;
        }

        public override void DoWindowContents(Rect rect)
        {
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
            float height = 6f + ServerInfo._config.UnsortedMods.Length * 30f;
            Rect viewRect = new Rect(0f, 0f, mainRect.width - 16f, height);
            Widgets.BeginScrollView(mainRect, ref base.ScrollPosition, viewRect);
            float num = 0;
            float num2 = base.ScrollPosition.y - 30f;
            float num3 = base.ScrollPosition.y + mainRect.height;
            int num4 = 0;

            for (int i = 0; i < ServerInfo._config.UnsortedMods.Length; i++)
            {
                if (num > num2 && num < num3)
                {
                    Rect rect = new Rect(0f, num, viewRect.width, 30f);
                    DrawCustomRow(rect, ServerInfo._config.UnsortedMods[i], num4, i);
                }

                num += 30f;
                num4++;
            }

            Widgets.EndScrollView();
        }

        private void DrawCustomRow(Rect rect, string modID, int rowCount, int index)
        {
            ModMetaData foundMod = ModLister.AllInstalledMods.FirstOrDefault(x => x.PackageId == modID);
            Text.Font = GameFont.Small;
            Rect fixedRect = new Rect(new Vector2(rect.x, rect.y + 5f), new Vector2(rect.width - 16f, rect.height - 5f));
            if (rowCount % 2 == 0) Widgets.DrawHighlight(fixedRect);
            Widgets.Label(fixedRect, $"{GetModType(modID)}{foundMod?.Name ?? modID}");
            if (foundMod == null || DownloadedMods.Contains(modID))
            {
                HandleSteamLinks(fixedRect, modID, index);
            }
            else 
            {
                HandleExistingMod(fixedRect, foundMod);
            }
        }

        private void HandleSteamLinks(Rect rect, string modID, int index) 
        {
            Rect linkButtonRect = new Rect(new Vector2(rect.xMax - SlimButtonSize.x - TinyButtonSize.x - 5f, rect.yMax - TinyButtonSize.y), TinyButtonSize);
            ulong steamId = ServerInfo._config.AllModIds[index];
            if (steamId != 0)
            {
                if (!modID.Contains("ludeon.rimworld"))
                {
                    if (Widgets.ButtonText(linkButtonRect, "Link"))
                    {
                        string url;
                        if (IsSteamRunning())
                        {
                            url = $"steam://url/CommunityFilePage/{steamId}";
                        }
                        else
                        {
                            url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={steamId}";
                        }
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                        DownloadedMods.Add(modID);
                    }
                    Rect downloadButtonRect = new Rect(linkButtonRect);
                    downloadButtonRect.x = rect.xMax - SlimButtonSize.x;
                    downloadButtonRect.width = SlimButtonSize.x;
                    if (Widgets.ButtonText(downloadButtonRect, "Download"))
                    {
                        ServerBrowserManager.DownloadMod(ServerInfo._config.AllModIds[index]);
                    }
                }
            }
        }

        private void HandleExistingMod(Rect rect, ModMetaData mod) 
        {
            Vector2 textSize = Text.CalcSize("Downloaded!");
            Widgets.Label(new Rect(new Vector2(rect.xMax - textSize.x + 5f, rect.yMax - textSize.y), textSize), "Downloaded!");
        }

        private static bool IsSteamRunning()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return IsProcessRunning("steam");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return IsProcessRunning("steam_osx");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return IsProcessRunning("steam");
            }
            return false;
        }

        private static bool IsProcessRunning(string processName)
        {
            try
            {
                foreach (var process in Process.GetProcesses())
                {
                    if (process.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            catch
            {
                
            }
            return false;
        }

        private string GetModType(string id) 
        {
            if (ServerInfo._config.RequiredMods.Contains(id))
                return ("[Required]>");
            else
                return ("[Optional]>");
        }
    }
}
