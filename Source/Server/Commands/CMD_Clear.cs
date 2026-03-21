using Shared;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Commands
{
    public class CMD_Clear : CMD_Base
    {
        public CMD_Clear()
        {
            Prefix = "clear";
            Description = "Clears the console";
        }

        public override void Action()
        {
            Console.Clear();
            Printer.Title("[Cleared console]");
        }
    }
}
