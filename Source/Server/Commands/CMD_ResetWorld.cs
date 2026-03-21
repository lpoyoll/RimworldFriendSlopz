using GameServer.Core;
using GameServer.Managers;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Commands
{
    public class CMD_ResetWorld : CMD_Base
    {
        public CMD_ResetWorld()
        {
            Prefix = "resetworld";
            Description = "Resets the server world";
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
