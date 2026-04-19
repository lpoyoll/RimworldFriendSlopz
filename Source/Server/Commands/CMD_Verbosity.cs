using GameServer.Core;
using GameServer.Files;
using Shared.Commands;
using Shared.Misc;

namespace RTServerBrowser.Commands
{
    public class CMD_Verbosity : CMD_Base
    {
        public CMD_Verbosity()
        {
            Prefix = "verbosity";
            Description = "Changes the verbosity to the desired value";
        }

        public override void Action()
        {
            Master.ServerConfig.Verbosity = int.Parse(CommandParameters[0]);
            ServerConfigFile.Save(ServerConfigFile.SavePath, Master.ServerConfig);

            Printer.Title($"Verbosity changed to {Master.ServerConfig.Verbosity}");
        }
    }
}
