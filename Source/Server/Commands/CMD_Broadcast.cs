using GameServer.Hooks.TCPNetwork;
using RTShared.Commands;
using RTShared.Misc;
using RTNetwork.Packets;
using static RTShared.Misc.CommonEnumerators;

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

            ServerNetwork.SendPacketToAllClients(PacketHeader.Console, commandData);

            Printer.Title($"Sent broadcast: '{fullText}'");
        }
    }
}
