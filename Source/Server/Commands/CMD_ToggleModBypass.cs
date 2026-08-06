using RTServer.Core;
using RTShared.Commands;
using RTShared.Files.Configs;
using RTShared.Misc;

namespace RTServer.Commands
{
    public class CMD_ToggleModBypass : CMD_Base
    {
        public CMD_ToggleModBypass()
        {
            Prefix = "togglemodbypass";
            Description = "Toggles mod bypass on/off";
        }

        public override void Action()
        {
            Master.ModConfig.AllowAllMods = !Master.ModConfig.AllowAllMods;
            FL_ModConfig.Save(FL_ModConfig.SavePath, Master.ModConfig);
            Printer.Warning($"Mod bypass is now: {Master.ModConfig.AllowAllMods}");
        }
    }
}
