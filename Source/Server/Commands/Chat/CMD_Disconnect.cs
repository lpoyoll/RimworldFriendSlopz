using GameServer.PacketManager;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Commands.Chat
{
    public class CMD_Disconnect : CMD_Base
    {
        public CMD_Disconnect()
        {
            Prefix = "/dc";
            Description = "Forcefully disconnects you from the server";
            IsChatCommand = true;
        }

        public override void Action()
        {
            if (PM_Chat.TargetClient == null) return;
            else PM_Chat.TargetClient.Listener.MarkForDisconnect();
        }
    }
}
