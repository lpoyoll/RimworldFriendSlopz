using RTServer.Core;
using RTServer.Files;
using RTShared.Commands;
using RTShared.Misc;

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
            FL_ServerConfig.Save(FL_ServerConfig.SavePath, Master.ServerConfig);

            Printer.Title($"Verbosity changed to {Master.ServerConfig.Verbosity}");
        }
    }
}
