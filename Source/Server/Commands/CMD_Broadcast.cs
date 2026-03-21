using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using GameServer.PacketManager;
using Shared;
using Shared.Files;
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
    public class CMD_Broadcast : CMD_Base
    {
        public CMD_Broadcast()
        {
            Prefix = "broadcast";
            Description = "";
            ParameterCount = -1;
        }

        public override void Action()
        {
            string fullText = "";
            foreach (string str in CMD_Base.CommandParameters) fullText += $"{str} ";
            fullText = fullText.Remove(fullText.Length - 1, 1);

            PKT_Command commandData = new PKT_Command();
            commandData._commandMode = CommandMode.Broadcast;
            commandData._details = fullText;

            ServerNetwork.SendPacketToAllClients(PacketHeader.ConsoleManager, commandData);

            Printer.Title($"Sent broadcast: '{fullText}'");
        }
    }
}
