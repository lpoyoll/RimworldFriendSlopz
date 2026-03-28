using Shared;
using Shared.Misc;

namespace GameServer.Commands
{
    public class CMD_GCClear : CMD_Base
    {
        public CMD_GCClear()
        {
            Prefix = "gcclear";
            Description = "Forces a GC clear";
        }

        public override void Action()
        {
            GC.Collect();
            Printer.Title($"Forced GC");
        }
    }
}
