using GameServer.Core;
using GameServer.Hooks.TCPNetwork;
using GameServer.Misc;
using Shared;
using Shared.Commands;
using Shared.Misc;
using System.Text;
using TCPNetwork;
using Shared.Files.ServerClient;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using static TCPNetwork.Packets.PKT_Chat;

namespace GameServer.PacketManager
{
    public class PM_Chat : PM_Base
    {
        private static Semaphore LogSemaphore { get; set; } = new Semaphore(1, 1);

        private static Semaphore CommandSemaphore { get; set; } = new Semaphore(1, 1);

        private static string SystemName { get; set; } = "CONSOLE";

        private static string NotificationName { get; set; } = "SERVER";

        public static ServerClient TargetClient { get; set; } = null;

        public static string[] LatestCommand { get; set; } = null;

        public static string DefaultJoinMessage { get; set; } = "Use '/help' to check all the available commands.";

        [HandlesPacket(PacketHeader.Chat)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Chat data = Serializer.ConvertBytesToObject<PKT_Chat>(bytes);

            if (data.IsCommand) ExecuteChatCommand(client, data.Message.Split(' '));
            else BroadcastChatMessage(client, data.Message);
        }

        public static void SendConsoleMessage(ServerClient client, string message)
        {
            PKT_Chat chatData = new PKT_Chat();
            chatData.Username = SystemName;
            chatData.Message = message;
            chatData.UsernameColor = ChatColor.Console;
            chatData.MessageColor = ChatColor.Console;

            client.Listener.EnqueuePacket(PacketHeader.Chat, chatData);
        }

        public static void SendServerMessage(ServerClient client, string message)
        {
            PKT_Chat chatData = new PKT_Chat();
            chatData.Username = NotificationName;
            chatData.Message = message;
            chatData.UsernameColor = ChatColor.Server;
            chatData.MessageColor = ChatColor.Server;

            client.Listener.EnqueuePacket(PacketHeader.Chat, chatData);
        }

        private static void BroadcastChatMessage(ServerClient client, string message)
        {
            PKT_Chat chatData = new PKT_Chat();
            chatData.Username = client.GetData<UserFile>().Username;
            chatData.Message = message;
            chatData.UsernameColor = client.GetData<UserFile>().IsAdmin ? ChatColor.Admin : ChatColor.Normal;
            chatData.MessageColor = ChatColor.Normal;

            ServerNetwork.SendPacketToAllClients(PacketHeader.Chat, chatData);
            PM_Chat.WriteChatInConsole(client.GetData<UserFile>().Username, message);
            WriteToLogs(client.GetData<UserFile>().Username, message);
        }

        public static void BroadcastConsoleMessage(string message)
        {
            PKT_Chat chatData = new PKT_Chat();
            chatData.Username = SystemName;
            chatData.Message = message;
            chatData.UsernameColor = ChatColor.Console;
            chatData.MessageColor = ChatColor.Console;

            ServerNetwork.SendPacketToAllClients(PacketHeader.Chat, chatData);
            PM_Chat.WriteChatInConsole(chatData.Username, message);
            WriteToLogs(chatData.Username, message);
        }

        public static void BroadcastServerNotification(string message)
        {
            PKT_Chat chatData = new PKT_Chat();
            chatData.Username = NotificationName;
            chatData.Message = message;
            chatData.UsernameColor = ChatColor.Server;
            chatData.MessageColor = ChatColor.Server;

            ServerNetwork.SendPacketToAllClients(PacketHeader.Chat, chatData);
            PM_Chat.WriteChatInConsole(chatData.Username, message);
            WriteToLogs(chatData.Username, message);
        }

        private static void ExecuteChatCommand(ServerClient client, string[] command)
        {
            CommandSemaphore.WaitOne();

            try
            {
                CMD_Base toFind = CMD_Base.ChatCommands.FirstOrDefault(fetch => fetch.Prefix == command[0]);
                if (toFind == null) SendConsoleMessage(client, "Command was not found.");
                else
                {
                    TargetClient = client;
                    LatestCommand = command;
                    toFind.Action();
                }

                string chatCommand = "";
                for (int i = 0; i < command.Length; i++) chatCommand += command[i] + "";

                PM_Chat.WriteChatInConsole(client.GetData<UserFile>().Username, chatCommand);
            }
            catch (Exception ex) { Printer.Error(ex); }

            CommandSemaphore.Release();
        }

        private static void WriteToLogs(string username, string message)
        {
            LogSemaphore.WaitOne();

            try
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append($"[{DateTime.Now:HH:mm:ss}] | [" + username + "]: " + message);
                stringBuilder.Append(Environment.NewLine);

                DateTime dateTime = DateTime.Now.Date;
                string nowFileName = (dateTime.Year + "-" + dateTime.Month.ToString("D2") + "-" + dateTime.Day.ToString("D2")).ToString();
                string nowFullPath = Master.ChatLogsPath + Path.DirectorySeparatorChar + nowFileName + ".txt";

                File.AppendAllText(nowFullPath, stringBuilder.ToString());
                stringBuilder.Clear();
            }
            catch (Exception ex) { Printer.Error(ex); }

            LogSemaphore.Release();
        }

        public static void WriteChatInConsole(string username, string message, bool fromDiscord = false)
        {
            if (!Master.ServerConfig.DisplayChatInConsole) return;
            else
            {
                if (fromDiscord) Printer.Message($"[Discord] > {username} > {message}");
                else InformationDisplayer.DisplayChatMap(username, message);
            }
        }

        public static void SendLoginChatMessages(ServerClient client)
        {
            PM_Chat.SendConsoleMessage(client, PM_Chat.DefaultJoinMessage);

            if (Master.ChatConfig.EnableMoTD) PM_Chat.SendServerMessage(client, $"MoTD > {Master.ChatConfig.MessageOfTheDay}");

            if (Master.ChatConfig.LoginNotifications) PM_Chat.BroadcastServerNotification($"{client.GetData<UserFile>().Username} has joined the server!");
        }
    }
}

