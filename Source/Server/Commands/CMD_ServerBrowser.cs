using GameServer.Hooks.ServerBrowser;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Commands
{
    public class CMD_ServerBrowser : CMD_Base
    {
        public CMD_ServerBrowser()
        {
            Prefix = "serverbrowser";
            Description = "Attempts to connect to the server browser";
        }

        public override void Action() { ServerBrowserManager.StartFeature(); }
    }
}
