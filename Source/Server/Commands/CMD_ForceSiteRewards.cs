using RTServer.PacketManager;
using RTShared.Commands;
using RTShared.Misc;

namespace RTServer.Commands
{
    public class CMD_ForceSiteRewards : CMD_Base
    {
        public CMD_ForceSiteRewards()
        {
            Prefix = "forcesiterewards";
            Description = "Forces site rewards on the server";
        }

        public override void Action()
        {
            PM_Sites.SendRewardsToEveryPlayer();
            Printer.Title("[Forced rewards]");
        }
    }
}
