using Shared;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Commands
{
    public class CMD_Help : CMD_Base
    {
        public CMD_Help()
        {
            Prefix = "help";
            Description = "Shows a list of all available commands to use";
        }

        public override void Action()
        {
            Printer.Title($"List of available commands: [{Commands.Count()}]");

            Printer.Title("----------------------------------------");
            foreach (CMD_Base command in Commands.ToList().OrderBy(fetch => fetch.Prefix)) Printer.Warning($"{command.Prefix} - {command.Description}");
            Printer.Title("----------------------------------------");
        }
    }
}
