using GameServer.Managers;
using Shared;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Commands
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
