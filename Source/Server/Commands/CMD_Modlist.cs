using GameServer.Core;
using RTShared.Commands;
using RTShared.Files.Configs;
using RTShared.Files.Mods;
using RTShared.Misc;

namespace GameServer.Commands
{
    public class CMD_Modlist : CMD_Base
    {
        public CMD_Modlist()
        {
            Prefix = "modlist";
            Description = "Displays a list of all server mods";
            ParameterCount = 0;
        }

        public override void Action() 
        {
            ModConfig[] required = Master.ModConfig.ModConfigs.Where(fetch => fetch.Type == FL_ModConfig.ModType.Required).ToArray();
            Printer.Title($"Required Mods: {required.Length}");
            Printer.Title("----------------------------------------");
            foreach (ModConfig config in Master.ModConfig.ModConfigs.Where(fetch => fetch.Type == FL_ModConfig.ModType.Required))
            {
                Printer.Warning(config.FileName);
            }

            ModConfig[] optional = Master.ModConfig.ModConfigs.Where(fetch => fetch.Type == FL_ModConfig.ModType.Optional).ToArray();
            Printer.Title($"Optional Mods: {optional.Length}");
            Printer.Title("----------------------------------------");
            foreach (ModConfig config in Master.ModConfig.ModConfigs.Where(fetch => fetch.Type == FL_ModConfig.ModType.Optional))
            {
                Printer.Warning(config.FileName);
            }

            ModConfig[] forbidden = Master.ModConfig.ModConfigs.Where(fetch => fetch.Type == FL_ModConfig.ModType.Forbidden).ToArray();
            Printer.Title($"Forbidden Mods: {forbidden.Length}");
            Printer.Title("----------------------------------------");
            foreach (ModConfig config in Master.ModConfig.ModConfigs.Where(fetch => fetch.Type == FL_ModConfig.ModType.Forbidden))
            {
                Printer.Warning(config.FileName);
            }
        }
    }
}
