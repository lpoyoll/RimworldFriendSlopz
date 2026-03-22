using GameServer.Hooks.ServerBrowser;
using Shared;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Commands
{
    public class CMD_Ip : CMD_Base
    {
        public CMD_Ip()
        {
            Prefix = "getip";
            Description = "Returns the public IPV4 of this server";
        }

        public override void Action() { Printer.Title($"Your server IPV4 is {ServerBrowserManager.GetPublicIP().Result}"); }
    }
}
