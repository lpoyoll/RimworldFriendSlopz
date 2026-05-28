using GameServer.Core;
using GameServer.Files;
using RTShared.Commands;
using RTShared.Misc;

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
            FL_ServerConfig.Save(FL_ServerConfig.SavePath, Master.ServerConfig);

            string display = Master.ServerConfig.EnableServerTelemetry ? "ON" : "OFF";
            Printer.Warning($"Server browser telemetry is now {display}");
        }
    }
}
