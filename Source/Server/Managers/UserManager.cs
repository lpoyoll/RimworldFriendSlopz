using GameServer.Core;
using RTNetwork.Packets;
using RTShared;
using RTShared.Files;
using RTShared.Files.ServerClient;
using RTShared.Misc;
using GameServer.Hooks.TCPNetwork;
using GameServer.PacketManager;
using static RTNetwork.Packets.PKT_Login;
using RTShared.Commands;
using RTNetwork;

namespace GameServer.Managers
{

    public static class UserManager
    {
        public static void SendPlayerRecount()
        {
            PKT_PlayerRecount playerRecountData = new PKT_PlayerRecount();
            playerRecountData.CurrentPlayerCount = ServerNetwork.GetConnectedClients().Count();
            ServerNetwork.SendPacketToAllClients(PacketHeader.PlayerRecount, playerRecountData);
        }

        public static void BanPlayerFromName(string username)
        {
            FL_Player userFile = UserManagerH.GetUserFileFromName(username);
            ServerClient client = ServerNetwork.GetConnectedClientFromUsername(username);
            if (userFile == null || client == null) Printer.Warning($"User '{CMD_Base.CommandParameters[0]}' was not found");
            else
            {
                if (userFile.IsBanned) Printer.Warning($"User '{userFile.Username}' is already banned from the server");
                else
                {
                    userFile.UpdateBan(true);
                    client.Listener.MarkForDisconnect();
                    Printer.Warning($"User '{userFile.Username}' has been banned from the server");
                }
            }
        }

        public static void PardonPlayerFromName(string username)
        {
            FL_Player userFile = UserManagerH.GetUserFileFromName(username);
            if (userFile == null) Printer.Warning($"User '{CMD_Base.CommandParameters[0]}' was not found");
            else
            {
                if (!userFile.IsBanned) Printer.Warning($"User '{userFile.Username}' is not banned from the server");
                else
                {
                    userFile.UpdateBan(false);
                    Printer.Warning($"User '{userFile.Username}' has been pardoned from the server");
                }
            }
        }
    }

    public static class UserManagerH
    {
        public static FL_Player GetUserFile(ServerClient client)
        {
            string[] userFiles = Directory.GetFiles(Master.UsersPath);

            foreach (string userFile in userFiles)
            {
                FL_Player file = Serializer.SerializeFromFile<FL_Player>(userFile);
                if (file.Username == client.GetData<FL_Player>().Username) return file;
            }

            return null;
        }

        public static FL_Player GetUserFileFromName(string username)
        {
            string[] userFiles = Directory.GetFiles(Master.UsersPath);

            foreach (string userFile in userFiles)
            {
                FL_Player file = Serializer.SerializeFromFile<FL_Player>(userFile);
                if (file.Username == username) return file;
            }

            return null;
        }

        public static FL_Player[] GetAllUserFiles()
        {
            List<FL_Player> userFiles = new List<FL_Player>();

            string[] existingUsers = Directory.GetFiles(Master.UsersPath);
            foreach (string user in existingUsers) userFiles.Add(Serializer.SerializeFromFile<FL_Player>(user));
            return userFiles.ToArray();
        }

        public static bool CheckIfUserIsConnected(string username)
        {
            ServerClient toGet = ServerNetwork.GetConnectedClientFromUsername(username);
            if (toGet != null) return true;
            else return false;
        }

        public static bool CheckIfUserExists(ServerClient client, PKT_Login data)
        {
            FL_Player toFind = GetAllUserFiles().FirstOrDefault(fetch => fetch.Username.ToLower() == data._username.ToLower());
            if (toFind != null) return true;
            else return false;
        }

        public static bool CheckIfUserAuthCorrect(ServerClient client, PKT_Login data)
        {
            FL_Player toFind = GetAllUserFiles().FirstOrDefault(fetch => fetch.Username == data._username && fetch.Password == data._password);
            if (toFind != null) return true;
            else
            {
                PM_Login.DenyConnectionWithReason(client, LoginResponse.Invalid);
                return false;

            }
        }

        public static bool CheckIfUserBanned(ServerClient client)
        {
            if (!client.GetData<FL_Player>().IsBanned) return false;
            else
            {
                Printer.Message($"Banned user '{client.GetData<FL_Player>().Username}' tried to join the server");
                PM_Login.DenyConnectionWithReason(client, LoginResponse.Ban);
                return true;
            }
        }

        public static bool CheckWhitelist(ServerClient client)
        {
            if (!Master.Whitelist.UseWhitelist) return true;
            else if (Master.Whitelist.WhitelistedUsers.ToArray().First(fetch => fetch == client.GetData<FL_Player>().Username) != null) return true;
            else
            {
                PM_Login.DenyConnectionWithReason(client, LoginResponse.Whitelist);
                return false;
            }
        }

        public static int[] GetUserStructuresTilesFromUsername(string username)
        {
            FL_Settlement[] settlements = PM_Settlements.GetAllSettlements().ToList().FindAll(x => x.Username == username).ToArray();
            FL_Site[] sites = PM_Sites.GetAllSites().ToList().FindAll(x => x.Username == username).ToArray();

            List<int> tilesToExclude = new List<int>();
            foreach (FL_Settlement settlement in settlements) tilesToExclude.Add(settlement.Tile);
            foreach (FL_Site site in sites) tilesToExclude.Add(site.Tile);

            return tilesToExclude.ToArray();
        }
    }
}
