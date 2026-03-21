using GameServer.Core;
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
    public class CMD_Reload : CMD_Base
    {
        public CMD_Reload()
        {
            Prefix = "reload";
            Description = "Reloads all server configs";
            ParameterCount = 0;
        }

        public override void Action() { Main_.LoadResources(); }
    }
}
