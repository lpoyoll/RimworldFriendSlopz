using RTServer.PacketManagers;
using RTShared.Commands;
using RTShared.Misc;

namespace RTServer.Commands
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
