using RTServer.Managers;
using RTShared.Commands;

namespace RTServer.Commands
{
    public class CMD_Ban : CMD_Base
    {
        public CMD_Ban()
        {
            Prefix = "ban";
            Description = "Bans the selected player from the server";
            ParameterCount = 1;
        }

        public override void Action() { UserManager.BanPlayerFromName(CMD_Base.CommandParameters[0]); }
    }
}
