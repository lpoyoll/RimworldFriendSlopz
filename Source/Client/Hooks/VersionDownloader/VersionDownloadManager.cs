using GameClient.Core;
using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using GameClient.Misc;
using RTShared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RTNetwork;
using RTNetwork.PacketManagers;
using RTNetwork.Packets.ServerBrowser;
using RTNetwork.Packets.VersionDownloader;
using UnityEngine;
using RTShared.Misc;

namespace GameClient.Hooks.VersionDownloader
{
    public static class VersionDownloadManager
    {
        private static readonly string GitHubURL = "https://github.com/RimWorld-Together/Rimworld-Together/releases/download";

        private static readonly string FileName = "3005289691.zip";

        private static WebClient Client { get; set; } = new WebClient();

        public static void ChangeVersion(string version)
        {
            if (!DownloadFile(Directory.GetParent(Master.ModMainPath).FullName))
            {
                DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "Error occurred while downloading the file!" }));
                return;
            }

            if (!ExtractVersion(Directory.GetParent(Master.ModMainPath).FullName))
            {
                DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "Error occurred while downloading the file!" }));
                return;
            }

            PrepareShellInstall(Directory.GetParent(Master.ModMainPath).FullName);
        }

        private static bool DownloadFile(string startingPath)
        {
            try
            {
                string completeURL = GitHubURL + "/" + DLG_Inputs.DialogInputResults[0] + "/" + FileName;
                Client.DownloadFile(completeURL, Path.Combine(startingPath, "3005289691.zip"));
                return true;
            }

            catch (Exception ex)
            {
                Printer.Error(ex);
                return false;
            }
        }

        private static bool ExtractVersion(string startingPath)
        {
            string zipPath = Path.Combine(startingPath, "3005289691.zip");
            string destination = Path.Combine(startingPath, "3005289691-Temp");

            if (!File.Exists(zipPath)) return false;
            else
            {
                if (Directory.Exists(destination)) Directory.Delete(destination);
                Directory.CreateDirectory(destination);

                try
                {
                    ZipFile.ExtractToDirectory(zipPath, destination);
                    File.Delete(zipPath);
                    return true;
                }
                catch { return false; }
            }
        }

        private static void PrepareShellInstall(string startingPath)
        {
            string scriptStartingLocation = Path.Combine(Master.ModScriptsPath, "VersionUpdater.bat");
            string scriptCopyPath = Path.Combine(startingPath, "VersionUpdater.bat");
            if (File.Exists(scriptCopyPath)) File.Delete(scriptCopyPath);
            File.Copy(scriptStartingLocation, scriptCopyPath);

            string txtPath = Path.Combine(Directory.GetParent(startingPath).FullName, "ModPath.txt");
            if (File.Exists(txtPath)) File.Delete(txtPath);
            File.WriteAllText(txtPath, Directory.GetParent(Master.ModMainPath).FullName);

            Action toDo = delegate
            {
                ProcessStartInfo processInfo = new ProcessStartInfo(scriptCopyPath);
                processInfo.UseShellExecute = false;
                Process.Start(processInfo);
                Application.Quit();
            };

            DLG_Base.PushNewDialog(new DLG_Message("MESSAGE", new string[] { "The game will close to apply the new version" }, toDo));
        }
    }
}
