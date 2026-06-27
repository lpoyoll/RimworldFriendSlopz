using RTServer.Managers;
using RTShared.Commands;

namespace RTServer.Commands
{
    public class CMD_Backup : CMD_Base
    {
        public CMD_Backup()
        {
            Prefix = "backup";
            Description = "Backs up the server";
        }

        public override void Action() { BackupManager.BackupServer(); }
    }
}
