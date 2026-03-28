using Shared;

namespace GameServer.Commands
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
