using GameServer.Core;
using GameServer.Managers;
using RTShared.Commands;
using RTShared.Misc;
using RTShared.Files.ServerClient;

namespace GameServer.Commands
{
    public class CMD_WhitelistRemove : CMD_Base
    {
        public CMD_WhitelistRemove()
        {
            Prefix = "whitelistremove";
            Description = "Removes the selected player from the whitelist";
            ParameterCount = 1;
        }

        public override void Action()
        {
            FL_Player userFile = UserManagerH.GetUserFileFromName(CMD_Base.CommandParameters[0]);
            if (userFile == null) Printer.Warning($"User '{CMD_Base.CommandParameters[0]}' was not found");
            else
            {
                if (!Master.Whitelist.WhitelistedUsers.Contains(userFile.Username))
                {
                    Printer.Warning($"User '{CMD_Base.CommandParameters[0]}' was not whitelisted");
                }
                else WhitelistManager.RemoveUserFromWhitelist(CMD_Base.CommandParameters[0]);
            }
        }
    }
}
