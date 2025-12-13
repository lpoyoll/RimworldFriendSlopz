using GameServer.Core;
using GameServer.Misc;
using TCPNetwork.Packets;
using Shared;
using static Shared.CommonEnumerators;
using TCPNetwork.Files.Client;

namespace GameServer.Managers
{
    public static class LoginManager
    {
        [HandlesPacket(PacketHeader.LoginManager)]
        private static void ParsePacket(ServerClient client, byte[] bytes, PacketHeader header)
        {
            LoginData data = Serializer.ConvertBytesToObject<LoginData>(bytes);

            HandleUser(client, data);
        }

        public static void HandleUser(ServerClient client, LoginData data)
        {
            client.UserFile = new UserFile();
            client.UserFile.UpdateLoginDetails(data);

            if (UserManagerH.CheckIfUserExists(client, data)) LoginUser(client, data);
            else RegisterUser(client, data);
        }

        public static void LoginUser(ServerClient client, LoginData data)
        {
            if (!UserManagerH.CheckIfUserAuthCorrect(client, data)) return;
            else
            {
                client.LoadUserFromFile(client);

                if (UserManagerH.CheckIfUserBanned(client)) return;

                if (!UserManagerH.CheckWhitelist(client)) return;

                if (WorldManager.CheckIfWorldExists() && ModManager.CheckIfModConflict(client, data)) return;

                LoginManagerH.RemoveOldClientSessions(client);

                InformationDisplayer.DisplayLogin(client);

                PostLogin(client);
            }
        }

        public static void RegisterUser(ServerClient client, LoginData data)
        {
            client.UserFile.UpdateHash();

            InformationDisplayer.DisplayRegister(client);

            LoginUser(client, data);
        }

        private static void PostLogin(ServerClient client)
        {
            SiteManager.SetSiteInfoForClient(client);

            UserManager.SendPlayerRecount();

            GlobalDataManager.SendServerGlobalData(client);

            foreach (string str in ChatManager.DefaultJoinMessages) ChatManager.SendConsoleMessage(client, str);

            if (Master.ChatConfig.EnableMoTD) ChatManager.SendServerMessage(client, $"MoTD > {Master.ChatConfig.MessageOfTheDay}");

            if (Master.ChatConfig.LoginNotifications) ChatManager.BroadcastServerNotification($"{client.UserFile.Username} has joined the server!");

            if (WorldManager.CheckIfWorldExists())
            {
                if (SaveManager.CheckIfUserHasSave(client)) SaveSenderManager.SendSaveToClient(client);
                else WorldManager.SendWorld(client);
            }

            else
            {
                Printer.Warning($"Giving first join admin permission to {client.UserFile.Username}");

                client.UserFile.UpdateAdmin(true);
                CommandData commandData = new CommandData();
                commandData._commandMode = CommandMode.Op;
                client.Listener.EnqueuePacket(PacketHeader.ConsoleManager, commandData);

                WorldManager.RequireWorldFile(client);
            }
        }
    }

    public static class LoginManagerH
    {
        public static void RemoveOldClientSessions(ServerClient client)
        {
            foreach (ServerClient toFind in ServerNetwork.Instance.GetConnectedClientsSafe())
            {
                if (toFind == client) continue;
                else
                {
                    if (toFind.UserFile.Username == client.UserFile.Username)
                    {
                        DenyConnectionWithReason(toFind, LoginResponse.Duplicate);
                    }
                }
            }
        }

        public static void DenyConnectionWithReason(ServerClient client, LoginResponse response, object extraDetails = null)
        {
            LoginData loginData = new LoginData();
            loginData._tryResponse = response;

            if (response == LoginResponse.Mods) loginData._extraDetails = (List<string>)extraDetails;
            else if (response == LoginResponse.Version) loginData._extraDetails = new List<string>() { CommonValues.ExecutableVersion };

            client.Listener.EnqueuePacket(PacketHeader.LoginManager, loginData);
            client.Listener.DisconnectFlag = true;
        }
    }
}