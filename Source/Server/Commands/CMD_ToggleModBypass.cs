using GameServer.Core;
using RTShared.Commands;
using RTShared.Files.Configs;
using RTShared.Misc;

namespace GameServer.Commands
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
            Master.ModConfig.BypassMods = !Master.ModConfig.BypassMods;
            FL_ModConfig.Save(FL_ModConfig.SavePath, Master.ModConfig);
            Printer.Warning($"Mod bypass is now: {Master.ModConfig.BypassMods}");
        }
    }
}
