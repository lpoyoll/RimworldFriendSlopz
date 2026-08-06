using System.Collections.Generic;
using System.Linq;
using RTNetwork.Components;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using RTServer.Core;
using RTServer.Hooks.TCPNetwork;
using RTServer.Managers;
using RTServer.Misc;
using RTShared.Files.Player;
using RTShared.Misc;
using static RTShared.Misc.CommonEnumerators;
using static RTNetwork.Packets.PKT_Login;

namespace RTServer.PacketManagers
{
    public class PM_Login : PM_Base
    {
        [HandlesPacket(PacketHeader.Login)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Login packet = Serializer.ConvertBytesToObject<PKT_Login>(bytes);

            if (PM_ServerPassword.CheckIfPasswordIsSet()) PM_ServerPassword.AskForPassword(client, packet);
            else TryLogin(client, packet);
        }

        public static void TryLogin(ServerClient client, PKT_Login packet)
        {
            if (UserManagerH.CheckIfUserExists(client, packet)) LoginUser(client, packet);
            else RegisterUser(client, packet);
        }

        private static bool LoginUser(ServerClient client, PKT_Login data)
        {
            if (!UserManagerH.CheckIfUserAuthCorrect(client, data)) return false;

            client.GetData<FL_Player>(FL_Player.LoadOrCreateUserFile(data.Username, data.Password));

            client.GetData<FL_Player>().UpdateIP(client.IP);
            
            if (UserManagerH.CheckIfUserBanned(client)) return false;

            if (PM_World.CheckIfWorldExists() && PM_Mods.CheckIfModConflict(client, data)) return false;

            RemoveOldClientSessions(client);

            InformationDisplayer.DisplayLogin(client);

            PostLogin(client);

            return true;
        }

        private static void RegisterUser(ServerClient client, PKT_Login data)
        {
            client.GetData<FL_Player>(FL_Player.LoadOrCreateUserFile(data.Username, data.Password));

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

                client.GetData<FL_Player>().UpdateAdmin(true);
                PKT_Command commandData = new PKT_Command();
                commandData._commandMode = CommandMode.Op;
                client.Listener.EnqueuePacket(PacketHeader.Console, commandData);
                Printer.Warning($"Giving first join admin permission to {client.GetData<FL_Player>().Username}");
            }
        }

        private static void RemoveOldClientSessions(ServerClient client)
        {
            ServerClient[] oldClients = ServerNetwork.GetConnectedClients().Where(fetch => fetch.GetData<FL_Player>().Username 
                == client.GetData<FL_Player>().Username && fetch != client).ToArray();

            foreach (ServerClient sc in oldClients) sc.Listener.MarkForDisconnect();
        }

        public static void DenyConnectionWithReason(ServerClient client, LoginResponse response, object extraDetails = null)
        {
            PKT_Login loginData = new PKT_Login();
            loginData.Response = response;

            if (response == LoginResponse.Mods) loginData.ServerMods = Master.ModConfig;
            else if (response == LoginResponse.Version) loginData.ExtraDetails = [CommonValues.ExecutableVersion];

            client.Listener.EnqueuePacket(PacketHeader.Login, loginData);
            client.Listener.MarkForDisconnect();
        }
    }
}