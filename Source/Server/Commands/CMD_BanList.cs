using GameServer.Managers;
using Shared.Commands;
using Shared.Misc;
using Shared.Files.ServerClient;

namespace GameServer.Commands
{
    public class CMD_BanList : CMD_Base
    {
        public CMD_BanList()
        {
            Prefix = "banlist";
            Description = "Shows a list of all banned server players";
            ParameterCount = 0;
        }

        public override void Action() 
        {
            FL_Player[] userFiles = UserManagerH.GetAllUserFiles().Where(x => x.IsBanned).ToArray();

            Printer.Title($"Banned players: [{userFiles.Count()}]");
            Printer.Title("----------------------------------------");
            foreach (FL_Player user in userFiles) Printer.Warning($"{user.Username} - {user.LatestIP}");
            Printer.Title("----------------------------------------");
        }
    }
}
