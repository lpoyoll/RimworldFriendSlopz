using RTServer.Core;
using RTServer.Managers;
using RTServer.PacketManagers;
using RTShared.Commands;
using RTShared.Misc;
using RTShared.Files.Player;

namespace RTServer.Commands
{
    public class CMD_ClearPassword : CMD_Base
    {
        public CMD_ClearPassword()
        {
            Prefix = "clearpassword";
            Description = "Clears the password to join this server";
        }

        public override void Action()
        {
            PM_ServerPassword.ClearPassword();
            Printer.Title("Password has been cleared");
        }
    }
}
