using GameServer.Core;
using GameServer.Managers;
using Shared.Commands;
using Shared.Misc;
using Shared.Files.ServerClient;

namespace GameServer.Commands
{
    public class CMD_WhitelistAdd : CMD_Base
    {
        public CMD_WhitelistAdd()
        {
            Prefix = "whitelistadd";
            Description = "Whitelists the selected player";
            ParameterCount = 1;
        }

        public override void Action()
        {
            PlayerFile userFile = UserManagerH.GetUserFileFromName(CMD_Base.CommandParameters[0]);
            if (userFile == null) Printer.Warning($"User '{CMD_Base.CommandParameters[0]}' was not found");
            else
            {
                if (Master.Whitelist.WhitelistedUsers.Contains(userFile.Username))
                {
                    Printer.Warning($"User '{CMD_Base.CommandParameters[0]}' was already whitelisted");
                }
                else WhitelistManager.AddUserToWhitelist(CMD_Base.CommandParameters[0]);
            }
        }
    }
}
