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
            RT_Dialog_Message dialog2 = new RT_Dialog_Message("MESSAGE", new string[] { "The game will restart to apply the new version" }, GenCommandLine.Restart);

            DialogManager.PushNewDialog(new RT_Dialog_Inputs("Version selection", 
                new string[] { "Release number", "Password (optional)" }, 
                new bool[] { false, true }, 
                delegate
                {
                    string downloadPath = Path.Combine(Master.appdataTempVersionPath, "3005289691.zip");
                    string extractPath = Path.Combine(Master.appdataTempVersionPath, "3005289691");
                    string uri = $"https://github.com/Byte-Nova/Rimworld-Together/releases/download/{DialogManager.dialogInputResults[0]}/3005289691.zip";

                    if (!DownloadVersion(uri, downloadPath))
                    {
                        DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "Version failed to download, check and try again" }));
                        return;
                    }

                    else if (!UnzipVersion(downloadPath, extractPath))
                    {
                        DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "Version failed to decompress, check logs for more information" }));
                        return;
                    }

                    else if (!InstallVersion(extractPath))
                    {
                        DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "Version failed to install, check logs for more information" }));
                        return;
                    }

                    else if (!Cleanup(downloadPath))
                    {
                        DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "Installer failed to cleanup, check logs for more information" }));
                        return;
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

        private static bool UnzipVersion(string downloadPath, string extractPath)
        {
            try
            {
                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);

                string appPath = Path.Combine(Master.modAddonsPath, "7z", "7z.exe");
                CMDExecuter.StartCMDWindow($"\"\"{appPath}\" x \"{downloadPath}\" -p\"{DialogManager.dialogInputResults[1]}\" -o\"{extractPath}\"");

                return true;
            }
            catch { return false; }
        }

        private static bool InstallVersion(string extractPath)
        {
            try
            {
                string modsDirectory = Directory.GetParent(Master.modMainPath).ToString();
                string installDirectory = Path.Combine(modsDirectory, "3005289691");

                CMDExecuter.StartCMDWindow($"rmdir \"{installDirectory}\" /s /q");

                CMDExecuter.StartCMDWindow($"move \"{extractPath}\" \"{modsDirectory}\"");

                return true;
            }
            catch { return false; }
        }

        private static bool Cleanup(string toClean)
        {
            try { CMDExecuter.StartCMDWindow($"del \"{toClean}\""); return true; }
            catch { return false; }
        }
    }
}
