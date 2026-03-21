using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using Shared;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCPNetwork.Files.Client;

namespace GameServer.Commands
{
    public class CMD_Ban : CMD_Base
    {
        public CMD_Ban()
        {
            Prefix = "ban";
            Description = "Bans the selected player from the server";
            ParameterCount = 1;
        }

        public override void Action() { UserManager.BanPlayerFromName(CMD_Base.CommandParameters[0]); }
    }
}
