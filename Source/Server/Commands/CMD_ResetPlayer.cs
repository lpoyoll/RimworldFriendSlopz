using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using GameServer.PacketManager;
using Shared.Commands;
using Shared.Misc;
using TCPNetwork.Files.Client;

namespace GameServer.Commands
{
    public class CMD_ResetPlayer : CMD_Base
    {
        public CMD_ResetPlayer()
        {
            Prefix = "resetplayer";
            Description = "Resets the selected player";
            ParameterCount = 1;
        }

        public override void Action()
        {
            UserFile userFile = UserManagerH.GetUserFileFromName(CMD_Base.CommandParameters[0]);
            if (userFile == null) Printer.Warning($"User '{CMD_Base.CommandParameters[0]}' was not found");
            else
            {
                ServerClient toFind = ServerNetwork.GetConnectedClientFromUsername(userFile.Username);
                PM_Saves.ResetPlayerData(toFind, userFile.Username);
            }
        }
    }
}
