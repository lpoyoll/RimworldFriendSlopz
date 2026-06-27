using RTServer.Core;
using RTServer.Files;
using RTShared.Commands;
using RTShared.Misc;

namespace RTServer.Commands
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
            FL_ServerConfig.Save(FL_ServerConfig.SavePath, Master.ServerConfig);

            string display = Master.ServerConfig.EnableServerBrowser ? "ON" : "OFF";
            Printer.Warning($"Server browser discovery is now {display}");
        }
    }
}
