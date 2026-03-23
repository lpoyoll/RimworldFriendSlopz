using GameServer.PacketManager;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCPNetwork.Files.Client;
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
                    ServerClient toFind = ChatManagerHelper.GetUserFromName(ChatManagerHelper.GetUsernameFromMention(PM_Chat.LatestCommand[1]));
                    if (toFind == null) PM_Chat.SendConsoleMessage(PM_Chat.TargetClient, "User was not found.");
                    else
                    {
                        PKT_Chat chatData = new PKT_Chat();
                        chatData._message = message;
                        chatData._usernameColor = ChatColor.Private;
                        chatData._messageColor = ChatColor.Private;

                        //Send to sender
                        chatData._username = $"Whisper to: {toFind.UserFile.Username}";
                        PM_Chat.TargetClient.Listener.EnqueuePacket(PacketHeader.ChatManager, chatData);

                        //Send to recipient
                        chatData._username = $"Whisper from: {PM_Chat.TargetClient.UserFile.Username}";
                        toFind.Listener.EnqueuePacket(PacketHeader.ChatManager, chatData);

                        ChatManagerHelper.ShowChatInConsole(chatData._username, message);
                    }
                }
            }
        }
    }
}
