using Shared;
using Shared.Misc;

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
