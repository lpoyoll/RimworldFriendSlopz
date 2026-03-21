using GameServer.Core;
using GameServer.Managers;
using Shared;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCPNetwork.Files.Client;

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
            UserFile userFile = UserManagerH.GetUserFileFromName(CMD_Base.CommandParameters[0]);
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
