using GameServer.Managers;
using RTShared.Commands;

namespace GameServer.Commands
{
    public class CMD_Pardon : CMD_Base
    {
        public CMD_Pardon()
        {
            Prefix = "pardon";
            Description = "Pardon the selected player from the server";
            ParameterCount = 1;
        }

        public override void Action() { UserManager.PardonPlayerFromName(CMD_Base.CommandParameters[0]); }
    }
}
