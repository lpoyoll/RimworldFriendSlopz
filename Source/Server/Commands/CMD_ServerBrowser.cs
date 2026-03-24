using GameServer.Core;
using GameServer.Files;
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
using TCPNetwork.Packets;
using static Shared.CommonEnumerators;

namespace GameServer.Commands
{
    public class CMD_ServerBrowser : CMD_Base
    {
        public CMD_ServerBrowser()
        {
            Prefix = "togglebrowser";
            Description = "Toggles the server browser discovery feature";
        }

        public override void Action()
        {
            Master.ServerConfig.EnableServerBrowser = !Master.ServerConfig.EnableServerBrowser;
            ServerConfigFile.Save(ServerConfigFile.SavePath, Master.ServerConfig);

            string display = Master.ServerConfig.EnableServerBrowser ? "ON" : "OFF";
            Printer.Warning($"Server browser discovery is now {display}");
        }
    }
}
