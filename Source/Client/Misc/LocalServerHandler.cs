using GameClient.Core;
using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Managers;
using HarmonyLib;
using Shared;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace GameClient.Misc
{
    public static class LocalServerHandler
    {
        private static readonly string DownloadURL = $"https://github.com/RimWorld-Together/Rimworld-Together/releases/download/" +
            $"{CommonValues.ExecutableVersion}/win-x64.zip";

        public static void ManageLocalServer()
        {
            CreateFolderIfMissing();
            if (Directory.GetFiles(Master.AppdataLocalServerPath).Length > 0) OpenExplorer();
            else AskForDownloadPermission();
        }

        private static void AskForDownloadPermission()
        {
            DLG_Base.PushNewDialog(new DLG_YesNo("The server will be downloaded. Continue?", 
                delegate { SetupServer(); OpenExplorer(); }, null));
        }

        private static void SetupServer()
        {
            DownloadServer();
            UnzipServer();
            Cleanup();
        }

        private static void CreateFolderIfMissing() 
        {
            if (!Directory.Exists(Master.AppdataLocalServerPath))
            {
                Directory.CreateDirectory(Master.AppdataLocalServerPath);
            }
        }

        private static void DownloadServer()
        {
            using WebClient webClient = new WebClient();
            webClient.DownloadFile(DownloadURL, Path.Combine(Master.AppdataLocalServerPath, "win-x64.zip"));
        }

        private static void UnzipServer()
        {
            string filePath = Path.Combine(Master.AppdataLocalServerPath, "win-x64.zip");
            ZipFile.ExtractToDirectory(filePath, Master.AppdataLocalServerPath);
        }

        private static void Cleanup()
        {
            File.Delete(Path.Combine(Master.AppdataLocalServerPath, "win-x64.zip"));
        }

        private static void OpenExplorer()
        {
            Process.Start(Master.AppdataLocalServerPath);

            DLG_Base.PushNewDialog(new DLG_Message("MESSAGE", new string[] { "The server folder is open in your explorer",
                "Please double click the server executable to boot" }));
        }
    }
}
