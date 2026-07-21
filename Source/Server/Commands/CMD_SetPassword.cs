using RTServer.Core;
using RTServer.Managers;
using RTServer.PacketManagers;
using RTShared.Commands;
using RTShared.Misc;
using RTShared.Files.Player;

namespace RTServer.Commands
{
    public class CMD_SetPassword : CMD_Base
    {
        public CMD_SetPassword()
        {
            Prefix = "setpassword";
            Description = "Sets the password to join this server";
            ParameterCount = 1;
        }

        public override void Action()
        {
            PM_ServerPassword.SetPassword(CommandParameters[0]);
            Printer.Title("Password has been changed");
        }
    }
}
