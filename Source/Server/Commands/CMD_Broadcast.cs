using RTServer.Hooks.TCPNetwork;
using RTShared.Commands;
using RTShared.Misc;
using RTNetwork.Packets;

namespace RTServer.Commands
{
    public class CMD_Broadcast : CMD_Base
    {
        public CMD_Broadcast()
        {
            Prefix = "broadcast";
            Description = "Broadcast a message to every player in letter format";
            ParameterCount = -1;
        }

        public override void Action()
        {
            string fullText = "";
            foreach (string str in CMD_Base.CommandParameters) fullText += $"{str} ";
            fullText = fullText.Remove(fullText.Length - 1, 1);

            PKT_Command commandData = new PKT_Command();
            commandData.Mode = PKT_Command.CommandMode.Broadcast;
            commandData.Details = fullText;

            ServerNetwork.SendPacketToAllClients(PacketHeader.Console, commandData);

            Printer.Title($"Sent broadcast: '{fullText}'");
        }
    }
}
