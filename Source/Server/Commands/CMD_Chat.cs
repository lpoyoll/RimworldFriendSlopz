using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using GameServer.PacketManager;
using Shared;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCPNetwork.Packets;
using static Shared.CommonEnumerators;

namespace GameServer.Commands
{
    public class CMD_Chat : CMD_Base
    {
        public CMD_Chat()
        {
            Prefix = "chat";
            Description = "Send a message in chat from the Server";
            ParameterCount = -1;
        }

        public override void Action()
        {
            string fullText = "";
            foreach (string str in CMD_Base.CommandParameters) fullText += $"{str} ";
            fullText = fullText.Remove(fullText.Length - 1, 1);

            PM_Chat.BroadcastConsoleMessage(fullText);

            Printer.Title($"Sent chat: '{fullText}'");
        }
    }
}
