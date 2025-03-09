using System.Diagnostics;

namespace GameClient.Misc
{
    public static class CMDExecuter
    {
        public static void StartCMDWindow(string command)
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
