using GameServer.Core;
using Shared;
using Shared.Files.Configs.Mods;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            ModConfig[] required = Master.ModConfig.ModConfigs.Where(fetch => fetch.Type == ModConfigFile.ModType.Required).ToArray();
            Printer.Title($"Required Mods: {required.Length}");
            Printer.Title("----------------------------------------");
            foreach (ModConfig config in Master.ModConfig.ModConfigs.Where(fetch => fetch.Type == ModConfigFile.ModType.Required))
            {
                Printer.Warning(config.FileName);
            }

            ModConfig[] optional = Master.ModConfig.ModConfigs.Where(fetch => fetch.Type == ModConfigFile.ModType.Optional).ToArray();
            Printer.Title($"Optional Mods: {optional.Length}");
            Printer.Title("----------------------------------------");
            foreach (ModConfig config in Master.ModConfig.ModConfigs.Where(fetch => fetch.Type == ModConfigFile.ModType.Optional))
            {
                Printer.Warning(config.FileName);
            }

            ModConfig[] forbidden = Master.ModConfig.ModConfigs.Where(fetch => fetch.Type == ModConfigFile.ModType.Forbidden).ToArray();
            Printer.Title($"Forbidden Mods: {forbidden.Length}");
            Printer.Title("----------------------------------------");
            foreach (ModConfig config in Master.ModConfig.ModConfigs.Where(fetch => fetch.Type == ModConfigFile.ModType.Forbidden))
            {
                Printer.Warning(config.FileName);
            }
        }
    }
}
