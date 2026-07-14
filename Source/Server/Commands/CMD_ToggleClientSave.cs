using RTServer.Core;
using RTServer.Files;
using RTServer.PacketManagers;
using RTShared.Commands;
using RTShared.Files.Configs;
using RTShared.Misc;

namespace RTServer.Commands
{
    public class CMD_ToggleClientSave : CMD_Base
    {
        public CMD_ToggleClientSave()
        {
            Prefix = "toggleclientsave";
            Description = "Changes whether to use the client save or not";
        }

        public override void Action()
        {
            Master.ServerConfig.UseClientSave = !Master.ServerConfig.UseClientSave;
            FL_ServerConfig.Save(FL_ServerConfig.SavePath, Master.ServerConfig);
            Printer.Warning($"UseClientSave is now: {Master.ServerConfig.UseClientSave}");
        }
    }
}
