using System.Diagnostics;

namespace GameClient.Misc
{
    public static class CMDExecuter
    {
        public static void StartCMDWindow(string command)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", $"/c {command}");
            processInfo.UseShellExecute = false;
            Process.Start(processInfo);
        }
    }
}
