using GameServer.Core;
using GameServer.Files;
using Shared.Commands;
using Shared.Misc;

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
