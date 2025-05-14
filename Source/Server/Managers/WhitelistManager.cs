using GameServer.Core;
using GameServer.Misc;
using static Shared.CommonEnumerators;

namespace GameServer.Managers
{

    public static class WhitelistManager
    {
        public static void AddUserToWhitelist(string username)
        {
            Master.Whitelist.WhitelistedUsers.Add(username);

            Main_.SaveValueFile(ServerFileMode.Whitelist);

            Printer.Warning($"User '{ConsoleManager.commandParameters[0]}' has been whitelisted");
        }

        public static void RemoveUserFromWhitelist(string username)
        {
            Master.Whitelist.WhitelistedUsers.Remove(username);

            Main_.SaveValueFile(ServerFileMode.Whitelist);

            Printer.Warning($"User '{ConsoleManager.commandParameters[0]}' is no longer whitelisted");
        }
    }
}
