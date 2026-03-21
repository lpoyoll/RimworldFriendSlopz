using GameServer.PacketManager;
using Shared;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Commands
{
    public class CMD_ForceSiteRewards : CMD_Base
    {
        public CMD_ForceSiteRewards()
        {
            Prefix = "forcesiterewards";
            Description = "Forces site rewards on the server";
        }

        public override void Action()
        {
            PM_Sites.SendRewardsToEveryPlayer();
            Printer.Title("[Forced rewards]");
        }
    }
}
