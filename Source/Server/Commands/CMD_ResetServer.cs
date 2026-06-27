using RTServer.Core;
using RTServer.Managers;
using RTShared.Commands;

namespace RTServer.Commands
{
    public class CMD_ResetServer : CMD_Base
    {
        public CMD_ResetServer()
        {
            Prefix = "resetserver";
            Description = "Resets the server completely";
        }

        public override void Action()
        {
            BackupManager.BackupServer();

            Directory.Delete(Master.AssetsPath, true);
            Directory.Delete(Master.ConfigsPath, true);

            Environment.Exit(0);
        }
    }
}
