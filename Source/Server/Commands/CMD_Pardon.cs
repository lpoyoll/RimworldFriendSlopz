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
    public class CMD_Pardon : CMD_Base
    {
        public CMD_Pardon()
        {
            Prefix = "pardon";
            Description = "Pardon the selected player from the server";
            ParameterCount = 1;
        }

        public override void Action() { UserManager.PardonPlayerFromName(CMD_Base.CommandParameters[0]); }
    }
}
