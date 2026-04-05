using GameServer.Core;
using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using GameServer.Misc;
using Shared;
using Shared.Misc;
using TCPNetwork.Files.Client;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using static Shared.CommonEnumerators;
using static TCPNetwork.Packets.PKT_Login;

namespace GameServer.PacketManager
{
    public class PM_Logins : PM_Base
    {
        [HandlesPacket(PacketHeader.LoginManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Login data = Serializer.ConvertBytesToObject<PKT_Login>(bytes);

            HandleUser(client, data);
        }

        public static void HandleUser(ServerClient client, PKT_Login data)
        {
            client.UserFile = new UserFile();
            client.UserFile.UpdateLoginDetails(data);

            if (UserManagerH.CheckIfUserExists(client, data))
            {
                var result = TryLoginUser(client, data);
                if (!string.IsNullOrEmpty(result))
                {
                    Printer.Error($"Error during login: {result}");
                }
            }
            else RegisterUser(client, data);
        }

        public static string TryLoginUser(ServerClient client, PKT_Login data)
        {
            if (!UserManagerH.CheckIfUserAuthCorrect(client, data)) return $"Login details to not match, either the password or username is wrong for {data._username}";
            
            client.LoadUserFromFile(client);

            if (UserManagerH.CheckIfUserBanned(client)) return $"{data._username} is banned";

            if (!UserManagerH.CheckWhitelist(client)) return $"{data._username} is not whitelisted";

            if (PM_World.CheckIfWorldExists() && PM_Mods.CheckIfModConflict(client, data)) return $"{data._username} has a mod conflict";

            LoginManagerH.RemoveOldClientSessions(client);

            InformationDisplayer.DisplayLogin(client);

            PostLogin(client);
            return "";
        }

        public static void RegisterUser(ServerClient client, PKT_Login data)
        {
            client.UserFile.UpdateHash();

            InformationDisplayer.DisplayRegister(client);

            var error = TryLoginUser(client, data);
            if (!string.IsNullOrEmpty(error))
            {
                Printer.Error($"Error during login: {error}");
            }
        }

        private static void PostLogin(ServerClient client)
        {
            PM_Sites.SetSiteInfoForClient(client);

            UserManager.SendPlayerRecount();

            GlobalDataManager.SendServerGlobalData(client);

            PM_Chat.DoOnConnectActions(client);

            if (PM_World.CheckIfWorldExists())
            {
                if (PM_Saves.CheckIfUserHasSave(client)) PM_Saves.SendSaveToClient(client);
                else PM_World.SendWorld(client);
            }

            else
            {
                PM_World.RequireWorldFile(client);

                client.UserFile.UpdateAdmin(true);
                PKT_Command commandData = new PKT_Command();
                commandData._commandMode = CommandMode.Op;
                client.Listener.EnqueuePacket(PacketHeader.ConsoleManager, commandData);
                Printer.Warning($"Giving first join admin permission to {client.UserFile.Username}");
            }
        }
    }

    public class LoginManagerH
    {
        public static void RemoveOldClientSessions(ServerClient client)
        {
            foreach (ServerClient toFind in ServerNetwork.GetConnectedClients())
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
            PKT_Login loginData = new PKT_Login();
            loginData._tryResponse = response;

            if (response == LoginResponse.Mods) loginData._extraDetails = (List<string>)extraDetails;
            else if (response == LoginResponse.Version) loginData._extraDetails = new List<string>() { CommonValues.ExecutableVersion };

            client.Listener.EnqueuePacket(PacketHeader.LoginManager, loginData);
            client.Listener.MarkForDisconnect();
        }
    }
}