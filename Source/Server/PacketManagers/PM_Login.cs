using GameServer.Core;
using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using GameServer.Misc;
using Shared;
using Shared.Misc;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using static Shared.CommonEnumerators;
using static TCPNetwork.Packets.PKT_Login;

namespace GameServer.PacketManager
{
    public class PM_Login : PM_Base
    {
        [HandlesPacket(PacketHeader.Login)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Login data = Serializer.ConvertBytesToObject<PKT_Login>(bytes);

            if (UserManagerH.CheckIfUserExists(client, data)) LoginUser(client, data);
            else RegisterUser(client, data);
        }

        public static bool LoginUser(ServerClient client, PKT_Login data)
        {
            if (!UserManagerH.CheckIfUserAuthCorrect(client, data)) return false;

            client.GetData<UserFile>(UserFile.LoadOrCreateUserFile(client, data));

            if (UserManagerH.CheckIfUserBanned(client)) return false;

            if (!UserManagerH.CheckWhitelist(client)) return false;

            if (PM_World.CheckIfWorldExists() && PM_Mods.CheckIfModConflict(client, data)) return false;

            RemoveOldClientSessions(client);

            InformationDisplayer.DisplayLogin(client);

            PostLogin(client);

            return true;
        }

        public static void RegisterUser(ServerClient client, PKT_Login data)
        {
            client.GetData<UserFile>(UserFile.LoadOrCreateUserFile(client, data));

            InformationDisplayer.DisplayRegister(client);

            LoginUser(client, data);
        }

        private static void PostLogin(ServerClient client)
        {
            UserManager.SendPlayerRecount();

            GlobalDataManager.SendServerGlobalData(client);

            PM_Chat.SendLoginChatMessages(client);

            if (PM_World.CheckIfWorldExists())
            {
                if (PM_Saves.CheckIfUserHasSave(client)) PM_Saves.SendSaveToClient(client);
                else PM_World.SendWorld(client);
            }

            else
            {
                PM_World.RequireWorldFile(client);

                client.GetData<UserFile>().UpdateAdmin(true);
                PKT_Command commandData = new PKT_Command();
                commandData._commandMode = CommandMode.Op;
                client.Listener.EnqueuePacket(PacketHeader.Console, commandData);
                Printer.Warning($"Giving first join admin permission to {client.GetData<UserFile>().Username}");
            }
        }

        public static void RemoveOldClientSessions(ServerClient client)
        {
            ServerClient[] oldClients = ServerNetwork.GetConnectedClients().Where(fetch => fetch.GetData<UserFile>().Username 
                == client.GetData<UserFile>().Username && fetch != client).ToArray();

            foreach (ServerClient sc in oldClients) sc.Listener.MarkForDisconnect();
        }

        public static void DenyConnectionWithReason(ServerClient client, LoginResponse response, object extraDetails = null)
        {
            PKT_Login loginData = new PKT_Login();
            loginData._tryResponse = response;

            if (response == LoginResponse.Mods) loginData._extraDetails = (List<string>)extraDetails;
            else if (response == LoginResponse.Version) loginData._extraDetails = new List<string>() { CommonValues.ExecutableVersion };

            client.Listener.EnqueuePacket(PacketHeader.Login, loginData);
            client.Listener.MarkForDisconnect();
        }
    }
}