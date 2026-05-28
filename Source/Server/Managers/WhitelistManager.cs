using GameServer.Core;
using RTShared.Commands;
using RTShared.Files.Configs;
using RTShared.Misc;

namespace GameServer.Managers
{
    public static class WhitelistManager
    {
        public static void AddUserToWhitelist(string username)
        {
            Master.Whitelist.WhitelistedUsers.Add(username);

            FL_WhitelistConfig.Save(FL_WhitelistConfig.SavePath, Master.Whitelist);

            Printer.Warning($"User '{CMD_Base.CommandParameters[0]}' has been whitelisted");
        }

        public static void RemoveUserFromWhitelist(string username)
        {
            Master.Whitelist.WhitelistedUsers.Remove(username);

            FL_WhitelistConfig.Save(FL_WhitelistConfig.SavePath, Master.Whitelist);

            Printer.Warning($"User '{CMD_Base.CommandParameters[0]}' is no longer whitelisted");
        }
    }
}
