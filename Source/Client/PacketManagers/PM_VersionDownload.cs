using GameClient.Core;
using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using TCPNetwork.Files.Client;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets.VersionDownloader;
using UnityEngine;

namespace GameClient.PacketManagers
{
    public class PM_VersionDownload : PM_Base
    {
        [HandlesPacket(PacketHeader.VersionDownload)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_VersionDownload data = Serializer.ConvertBytesToObject<PKT_VersionDownload>(bytes);

            switch (data.CurrentStepMode)
            {
                case PKT_VersionDownload.StepMode.Receive:
                    ReceiveVersion(data);
                    break;

                case PKT_VersionDownload.StepMode.Deny:
                    GetVersionDenied();
                    break;
            }
        }

        private static void ReceiveVersion(PKT_VersionDownload data)
        {
            DownloadVersion(data, Directory.GetParent(Master.ModMainPath).FullName);

            ExtractVersion(Directory.GetParent(Master.ModMainPath).FullName);

            PrepareShellInstall();
        }

        private static void GetVersionDenied()
        {
            DLG_Base.PushNewDialog(new DLG_Message("ERROR", new string[] { "The specified version is not available!" }));
        }

        private static void DownloadVersion(PKT_VersionDownload data, string startingPath) 
        {
            string filePath = Path.Combine(startingPath, "3005289691.zip");
            File.WriteAllBytes(filePath, data.VersionContents); 
        }

        private static bool ExtractVersion(string startingPath)
        {
            string zipPath = Path.Combine(startingPath, "3005289691.zip");
            string destination = Path.Combine(startingPath, "3005289691-Temp");

            try
            {
                if (Directory.Exists(destination)) Directory.Delete(destination);

                Directory.CreateDirectory(destination);
                ZipFile.ExtractToDirectory(zipPath, destination);
                File.Delete(zipPath);

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
