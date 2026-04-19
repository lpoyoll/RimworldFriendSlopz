using Shared.Commands;
using Shared.Misc;
using System;

namespace Shared.Commands
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
