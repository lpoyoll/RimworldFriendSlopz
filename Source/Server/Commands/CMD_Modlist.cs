using RTServer.Core;
using RTShared.Commands;
using RTShared.Files.Configs;
using RTShared.Files.Mods;
using RTShared.Misc;

namespace RTServer.Commands
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
            FL_ModData[] required = Master.ModConfig.ModConfigs.Where(fetch => fetch.Type == FL_ModConfig.ModType.Required).ToArray();
            Printer.Title($"Required Mods: {required.Length}");
            Printer.Title("----------------------------------------");
            foreach (FL_ModData config in Master.ModConfig.ModConfigs.Where(fetch => fetch.Type == FL_ModConfig.ModType.Required))
            {
                Printer.Warning(config.ModName);
            }

            FL_ModData[] optional = Master.ModConfig.ModConfigs.Where(fetch => fetch.Type == FL_ModConfig.ModType.Optional).ToArray();
            Printer.Title($"Optional Mods: {optional.Length}");
            Printer.Title("----------------------------------------");
            foreach (FL_ModData config in Master.ModConfig.ModConfigs.Where(fetch => fetch.Type == FL_ModConfig.ModType.Optional))
            {
                Printer.Warning(config.ModName);
            }
        }
    }
}
