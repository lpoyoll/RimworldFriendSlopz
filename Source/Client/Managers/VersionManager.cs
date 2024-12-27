using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using GameClient.Core;
using GameClient.Dialogs;
using GameClient.Misc;
using Verse;

namespace GameClient.Managers
{
    public static class VersionManager
    {
        public static void PromptChangeVersion()
        {
            RT_Dialog_OK dialog2 = new RT_Dialog_OK("The game will restart to apply the new version", GenCommandLine.Restart);
            DialogManager.PushNewDialog(new RT_Dialog_1Input("Version selection", "Please type the version number you want to switch to",
                delegate
                {
                    string downloadPath = Path.Combine(Master.tempFolderPath, "Download.zip");
                    string extractPath = Path.Combine(Master.tempFolderPath, "Output");
                    string uri = $"https://github.com/Byte-Nova/Rimworld-Together/releases/download/{DialogManager.dialog1ResultOne}/3005289691.zip";
                    Printer.Warning(uri);

                    if (!DownloadVersion(uri, downloadPath))
                    {
                        DialogManager.PushNewDialog(new RT_Dialog_OK("Version failed to download, check and try again"));
                        return;
                    }

                    else if (!InstallVersion(downloadPath, extractPath))
                    {
                        DialogManager.PushNewDialog(new RT_Dialog_OK("Version failed to install, check logs for more information"));
                    }

                    DialogManager.PushNewDialog(dialog2);
                }
            ));
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

        private static bool InstallVersion(string downloadPath, string extractPath)
        {
            try
            {
                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);

                string installDirectory = Directory.GetParent(Master.modAssemblyFolderPath).Parent.Parent.ToString() + 
                    Path.DirectorySeparatorChar + "3005289691";

                StartCMDWindow($"powershell -command Expand-Archive -Force '{downloadPath}' '{extractPath}'");
                StartCMDWindow($"del \"{downloadPath}\"");

                StartCMDWindow($"rmdir \"{installDirectory}\" /s /q");
                StartCMDWindow($"move \"{extractPath}\" \"{installDirectory}\"");

                return true;
            }
            catch { return false; }
        }

        private static void StartCMDWindow(string command)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", $"/c {command}");
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardError = true;

            Process process = Process.Start(processInfo);
            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) => Printer.Error(e.Data);
            process.BeginErrorReadLine();

            process.WaitForExit();
            process.Close();
        }
    }
}
