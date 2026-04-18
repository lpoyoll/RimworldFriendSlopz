using GameClient.Core;
using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GameClient.Managers
{
    public static class ModVersionManager
    {
        private static readonly string downloadURL = "https://github.com/RimWorld-Together/Rimworld-Together/releases/download";

        private static string DownloadPath { get; set; } = Path.Combine(Master.AppdataVersionPath, fileName);

        private static readonly string fileName = "3005289691.zip";

        public static void ChangeVersion(string version)
        {
            string url = $"{downloadURL}/{version}/{fileName}";

            if (!DownloadVersion(url, DownloadPath))
            {
                DLG_Base.PushNewDialog(new DLG_Message("ERROR",
                    new string[] { "Failed to download specified version! Please retry" }));
            }

            string parentFolder = Directory.GetParent(Master.ModMainPath).FullName;
            if (!ExtractVersion(DownloadPath, parentFolder))
            {
                DLG_Base.PushNewDialog(new DLG_Message("ERROR",
                    new string[] { "Failed to extract specified version! Please retry" }));
            }

            PrepareShellInstall();
        }

        private static bool DownloadVersion(string uri, string downloadPath)
        {
            try
            {
                if (File.Exists(downloadPath)) File.Delete(downloadPath);

                using WebClient webClient = new WebClient();
                webClient.DownloadFile(new Uri(uri), downloadPath);

                return true;
            }
            catch { return false; }
        }

        private static bool ExtractVersion(string filePath, string destination)
        {
            try
            {
                if (Directory.Exists(destination)) Directory.Delete(destination);

                Directory.CreateDirectory(destination);
                ZipFile.ExtractToDirectory(filePath, destination);

                return true;
            }
            catch { return false; }
        }

        private static void PrepareShellInstall()
        {
            string scriptPath = Path.Combine(Master.ModScriptsPath, "VersionUpdater.bat");
            string copyPath = Path.Combine(Master.AppdataTempPath, "VersionUpdater.bat");
            string modPath = Path.Combine(Master.AppdataTempPath, "ModPath.txt");

            File.Copy(scriptPath, copyPath);
            File.WriteAllText(modPath, Master.ModMainPath);

            Action toDo = delegate
            {
                ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", $"/c {$"\"\"{copyPath}\""}");
                processInfo.UseShellExecute = false;
                Process.Start(processInfo);

                Application.Quit();
            };

            DLG_Base.PushNewDialog(new DLG_Message("MESSAGE", new string[] { "The game will close to apply the new version" }, toDo));
        }
    }
}
