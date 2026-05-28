using GameServer.Hooks.TCPNetwork;
using GameServer.PacketManager;
using Shared;
using Shared.Commands;
using TCPNetwork;
using Shared.Files.ServerClient;
using TCPNetwork.Packets;
using static TCPNetwork.Packets.PKT_Chat;

namespace GameServer.Commands.Chat
{
    public class CMD_Whisper : CMD_Base
    {
        public CMD_Whisper()
        {
            Prefix = "/w";
            Description = "Sends a private message to a specific user";
            IsChatCommand = true;
        }

        public override void Action()
        {
            if (PM_Chat.TargetClient == null) return;
            else
            {
                string message = "";
                for (int i = 2; i < PM_Chat.LatestCommand.Length; i++) message += PM_Chat.LatestCommand[i] + " ";

                if (string.IsNullOrWhiteSpace(message)) PM_Chat.SendConsoleMessage(PM_Chat.TargetClient, "Message was empty.");
                else
                {
                    ServerClient toFind = ServerNetwork.GetConnectedClientFromUsername(PM_Chat.LatestCommand[1].Replace("@", ""));
                    if (toFind == null) PM_Chat.SendConsoleMessage(PM_Chat.TargetClient, "User was not found.");
                    else
                    {
                        PKT_Chat chatData = new PKT_Chat();
                        chatData.Message = message;
                        chatData.UsernameColor = ChatColor.Private;
                        chatData.MessageColor = ChatColor.Private;

                        //Send to sender
                        chatData.Username = $"Whisper to '{toFind.GetData<FL_Player>().Username}'";
                        PM_Chat.TargetClient.Listener.EnqueuePacket(PacketHeader.Chat, chatData);

                        //Send to recipient
                        chatData.Username = $"Whisper from '{PM_Chat.TargetClient.GetData<FL_Player>().Username}'";
                        toFind.Listener.EnqueuePacket(PacketHeader.Chat, chatData);
                        PM_Chat.WriteChatInConsole(chatData.Username, message);
                    }
                }
            }
        }
    }
}
