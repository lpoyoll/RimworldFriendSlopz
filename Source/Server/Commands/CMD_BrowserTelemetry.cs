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
    public class CMD_BrowserTelemetry : CMD_Base
    {
        public CMD_BrowserTelemetry()
        {
            Prefix = "toggletelemetry";
            Description = "Toggles the server browser telemetry feature";
        }

        public override void Action()
        {
            Master.ServerConfig.EnableServerTelemetry = !Master.ServerConfig.EnableServerTelemetry;
            ServerConfigFile.Save(ServerConfigFile.SavePath, Master.ServerConfig);

            string display = Master.ServerConfig.EnableServerTelemetry ? "ON" : "OFF";
            Printer.Warning($"Server browser telemetry is now {display}");
        }
    }
}
