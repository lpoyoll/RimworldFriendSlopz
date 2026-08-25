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
using static RTNetwork.Packets.PKT_Login;

namespace RTServer.PacketManagers
{
    public class PM_Login : PM_Base
    {
        [HandlesPacket(PacketHeader.Login)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Login packet = Serializer.ConvertBytesToObject<PKT_Login>(bytes);

            Printer.Message($"[AUTH] Login request | IP={client.IP} | User={SafeUsername(packet?.Username)} | ServerPasswordRequired={PM_ServerPassword.CheckIfPasswordIsSet()}", Printer.Verbosity.Verbose);

            if (PM_ServerPassword.CheckIfPasswordIsSet()) PM_ServerPassword.AskForPassword(client, packet);
            else TryLogin(client, packet);
        }

        public static void TryLogin(ServerClient client, PKT_Login packet)
        {
            bool userExists = UserManagerH.CheckIfUserExists(client, packet);
            Printer.Message($"[AUTH] Account lookup | IP={client.IP} | User={SafeUsername(packet?.Username)} | ExistingAccount={userExists}", Printer.Verbosity.Verbose);

            if (userExists) LoginUser(client, packet);
            else RegisterUser(client, packet);
        }

        private static bool LoginUser(ServerClient client, PKT_Login data)
        {
            if (!UserManagerH.CheckIfUserAuthCorrect(client, data)) return false;

            client.GetData<FL_Player>(FL_Player.LoadOrCreateUserFile(data.Username, data.Password));

            client.GetData<FL_Player>().UpdateIP(client.IP);
            
            if (UserManagerH.CheckIfUserBanned(client)) return false;

            if (PM_Mods.CheckIfModConflict(client, data.RunningMods)) return false;
            
            if (PM_Mods.CheckForModConfigs(client, data.RunningMods)) return false;
            
            if (PM_Mods.CheckForModOrder(client, data.RunningMods)) return false;

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

            if (!PM_World.CheckIfWorldExists())
            {
                Printer.Warning($"[WORLD] No server world exists yet; requesting world from first authenticated client '{client.GetData<FL_Player>().Username}' ({client.IP})");
                PM_World.RequireWorldFile(client);
            }
            else
            {
                if (PM_Saves.CheckIfUserHasSave(client)) PM_Saves.SendSaveToClient(client);
                else PM_World.SendWorld(client);
            }
        }

        private static void RemoveOldClientSessions(ServerClient client)
        {
            ServerClient[] oldClients = ServerNetwork.GetConnectedClients().Where(fetch => fetch.GetData<FL_Player>().Username 
                == client.GetData<FL_Player>().Username && fetch != client).ToArray();

            foreach (ServerClient sc in oldClients)
            {
                Printer.Warning($"[SESSION] Replacing previous session | User={client.GetData<FL_Player>().Username} | OldIP={sc.IP} | NewIP={client.IP}");
                sc.Listener.MarkForDisconnect();
            }
        }

        public static void DenyConnectionWithReason(
            ServerClient client,
            LoginResponse response,
            object extraDetails = null,
            string username = null,
            string diagnosticDetails = null)
        {
            PKT_Login loginData = new PKT_Login();
            loginData.Response = response;

            if (response == LoginResponse.Mods || response == LoginResponse.ModConfigs || response == LoginResponse.ModOrder) loginData.ServerMods = Master.ModConfig;
            else if (response == LoginResponse.Version) loginData.ExtraDetails = [CommonValues.ExecutableVersion];

            string resolvedUsername = username;
            try
            {
                if (string.IsNullOrWhiteSpace(resolvedUsername)) resolvedUsername = client.GetData<FL_Player>()?.Username;
            }
            catch { }

            int connected = 0;
            try { connected = ServerNetwork.GetConnectedClients().Length; }
            catch { }

            bool worldLoaded = Master.WorldValues != null;
            bool worldFileExists = false;
            try { worldFileExists = PM_World.CheckIfWorldExists(); }
            catch { }

            string details = string.IsNullOrWhiteSpace(diagnosticDetails) ? string.Empty : $" | Details={diagnosticDetails}";
            Printer.Warning($"[DENY] IP={client.IP} | User={SafeUsername(resolvedUsername)} | Reason={response} | Connected={connected}/{Master.ServerConfig.MaxPlayers} | WorldLoaded={worldLoaded} | WorldFile={worldFileExists}{details}");

            client.Listener.EnqueuePacket(PacketHeader.Login, loginData);
            client.Listener.MarkForDisconnect();
        }

        private static string SafeUsername(string username)
        {
            return string.IsNullOrWhiteSpace(username) ? "<unknown>" : username;
        }
    }
}