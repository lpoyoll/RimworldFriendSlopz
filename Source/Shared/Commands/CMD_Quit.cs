using Shared.Commands;
using System;

namespace Shared.Commands
{
    public class CMD_Quit : CMD_Base
    {
        public CMD_Quit()
        {
            Prefix = "quit";
            Description = "Quits the server";
        }

        public override void Action() { Environment.Exit(0); }
    }
}
