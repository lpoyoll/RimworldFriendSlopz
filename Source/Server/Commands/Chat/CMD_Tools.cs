using GameServer.PacketManager;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Commands.Chat
{
    public class CMD_Tools : CMD_Base
    {
        public CMD_Tools()
        {
            Prefix = "/tools";
            Description = "Shows a list of all available chat tools";
            IsChatCommand = true;
        }

        public override void Action()
        {
            if (PM_Chat.TargetClient == null) return;
            else
            {
                foreach (string str in PM_Chat.DefaultTextTools)
                {
                    PM_Chat.SendConsoleMessage(PM_Chat.TargetClient, str);
                }
            }
        }
    }
}
