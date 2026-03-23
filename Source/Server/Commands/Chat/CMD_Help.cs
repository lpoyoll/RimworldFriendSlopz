using GameServer.PacketManager;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Commands.Chat
{
    public class CMD_Help : CMD_Base
    {
        public CMD_Help()
        {
            Prefix = "/help";
            Description = "Shows a list of all available commands";
            IsChatCommand = true;
        }

        public override void Action()
        {
            if (PM_Chat.TargetClient == null) return;
            else
            {
                List<string> messagesToSend = new List<string> { "List of available commands:" };
                foreach (CMD_Base command in CMD_Base.ChatCommands) messagesToSend.Add($"{command.Prefix} - {command.Description}");
                foreach (string str in messagesToSend) PM_Chat.SendConsoleMessage(PM_Chat.TargetClient, str);
            }
        }
    }
}
