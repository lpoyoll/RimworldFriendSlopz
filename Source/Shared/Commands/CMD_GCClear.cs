using Shared.Commands;
using Shared.Misc;
using System;

namespace Shared.Commands
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
