using GameClient.Core.Configs;
using GameClient.Defs;
using GameClient.Hooks.TCPNetwork;
using GameClient.Misc;
using GameClient.Tabs;
using HarmonyLib;
using RimWorld;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;
using UnityEngine;
using Verse;
using Verse.Sound;
using static Shared.CommonEnumerators;
using static TCPNetwork.Packets.PKT_Chat;

namespace GameClient.PacketManagers
{
    [StaticConstructorOnStartup]
    public class PM_Chat : PM_Base
    {
        [HandlesPacket(PacketHeader.ChatManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Chat data = Serializer.ConvertBytesToObject<PKT_Chat>(bytes);

            AddMessageToChat(data._username, data._message, data._usernameColor, data._messageColor);
        }

        public static void SendMessage(string messageToSend)
        {
            RTSoundDefs.ChatSend.PlayOneShotOnCamera();

            PKT_Chat chatData = new PKT_Chat();
            chatData._username = SessionHandler.Username;
            chatData._message = messageToSend;

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.ChatManager, chatData);
        }

        public static void AddMessageToChat(string username, string message, ChatColor userColor, ChatColor messageColor)
        {
            if (TAB_Chat.ChatMessages.Count() > 100) TAB_Chat.ChatMessages.RemoveAt(0);

            if (ChatManagerH.CheckIfHasBeenTagged(message)) message = message.Replace($"@{SessionHandler.Username}", $"<color=red>@{SessionHandler.Username}</color>");

            TAB_Chat.ChatMessages.Add($"<color=grey>{DateTime.Now.ToString("HH:mm")}</color> " + $"{ChatManagerH.messageColorDictionary[userColor]}{username}</color>: " +
                $"{ChatManagerH.messageColorDictionary[messageColor]}{ChatManagerH.ParseMessage(message)}</color>");

            if (!TAB_Chat.IsTabOpen & !TAB_Chat.ShouldPlaySounds) RTSoundDefs.ChatReceive.PlayOneShotOnCamera();
        }

        [OnSessionEnd]
        private static void CleanChat()
        {
            TAB_Chat.CurrentChatInput = string.Empty;
            TAB_Chat.ChatMessages = new List<string>();
        }
    }

    public class ChatManagerH
    {
        public static Dictionary<ChatColor, string> messageColorDictionary = new Dictionary<ChatColor, string>()
        {
            { ChatColor.Normal, "<color=white>" },
            { ChatColor.Admin, "<color=red>" },
            { ChatColor.Console, "<color=yellow>" },
            { ChatColor.Private, "<color=#3ae0dd>" },
            { ChatColor.Discord, "<color=white>" },
            { ChatColor.Server, " <color=white>" }
        };

        public static string[] GetMessageWords(string message) { return message.Split(' '); }

        public static bool CheckIfHasBeenTagged(string message) { return GetMessageWords(message).Contains($"@{SessionHandler.Username}"); }

        public static string ParseMessage(string message, bool fromBroadcast = false)
        {
            bool verifying = false;
            string verification = "";
            Stack<string> codeType = new Stack<string>();

            message = Regex.Replace(message, @"\*\*\*(.+?)\*\*\*", "[b][i]$1[/][/]");
            message = Regex.Replace(message, @"\*\*(.+?)\*\*", "[b]$1[/]");
            message = Regex.Replace(message, @"\*(.+?)\*", "[i]$1[/]");
            message = Regex.Replace(message, @"\&([a-fA-F0-9]{6})(.+?)\&\&", "[$1]$2[/]");

            foreach (char c in message)
            {
                if (c == '[') verifying = true;

                if (verifying)
                {
                    verification += c;
                    if (c == ']') verifying = false;
                }

                if (verification != "" && !verifying)
                {
                    switch (verification.ToLower())
                    {
                        //Check for TAG CLOSING

                        case "[/]":
                            if (codeType.Count > 0) message = message.ReplaceFirst(verification, $"</{codeType.Pop()}>");
                            verification = "";
                            break;

                        //Check for BOLD

                        case "[b]":
                            message = message.Replace(verification, "<b>");
                            codeType.Push("b");
                            verification = "";
                            break;

                        //Check for CURSIVE

                        case "[i]":
                            message = message.Replace(verification, "<i>");
                            codeType.Push("i");
                            verification = "";
                            break;

                        //Check for NEW LINE (broadcasts only)

                        case "[n]":
                            if (fromBroadcast)
                            {
                                message = message.Replace(verification, "\n");
                                verification = "";
                            }
                            break;

                        //Check for CUSTOM COLOR

                        default:
                            if (Regex.IsMatch(verification, @"\[[a-fA-F0-9]{6}\]"))
                            {
                                string verificationReplacement = verification.Replace("[", "<color=#").Replace("]", ">");
                                message = message.Replace(verification, verificationReplacement);
                                codeType.Push("color");
                                verification = "";
                            }
                            break;
                    }
                }
            }

            while (codeType.Count > 0) message += $"</{codeType.Pop()}>";

            return message;
        }
    }
}
