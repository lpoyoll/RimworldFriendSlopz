using GameServer.Files;
using GameServer.Misc;
using Shared;
using Shared.Files;
using Shared.Files.Guild;
using System.Linq;
using TCPNetwork.Packets;
using TCPNetwork.Server;
using static Shared.CommonEnumerators;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GameServer.Managers
{

    public static class GoodwillManager
    {
        [HandlesPacket(PacketHeader.GoodWillManager)]
        private static void ParsePacket(ServerClient client, byte[] bytes)
        {
            FactionGoodwillData data = Serializer.ConvertBytesToObject<FactionGoodwillData>(bytes);

            Printer.Warning(data, LogImportanceMode.Extreme);

            ChangeUserGoodwills(client, data);
        }

        public static void ChangeUserGoodwills(ServerClient client, FactionGoodwillData data)
        {
            SettlementFile settlementFile = SettlementManager.GetSettlementFileFromTile(data._tile);
            SiteFile siteFile = SiteManagerHelper.GetSiteFileFromTile(data._tile);

            if (settlementFile != null) data._username = settlementFile.Username;
            else data._username = siteFile.Username;

            GuildFile guild = GuildManagerH.GetFactionFromFactionName(client.UserFile.GuildName);
            if (GuildManagerH.CheckIfUserIsInFaction(guild, data._username))
            {
                ResponseShortcutManager.SendBreakPacket(client);
                return;
            }

            client.UserFile.EnemyPlayers.Remove(data._username);
            client.UserFile.AllyPlayers.Remove(data._username);

            if (data._goodwill == Goodwill.Enemy)
            {
                if (!client.UserFile.EnemyPlayers.Contains(data._username))
                {
                    client.UserFile.EnemyPlayers.Add(data._username);
                }
            }

            else if (data._goodwill == Goodwill.Ally)
            {
                if (!client.UserFile.AllyPlayers.Contains(data._username))
                {
                    client.UserFile.AllyPlayers.Add(data._username);
                }
            }

            List<Goodwill> tempSettlementList = new List<Goodwill>();
            SettlementFile[] settlements = SettlementManager.GetAllSettlements();
            foreach (SettlementFile settlement in settlements)
            {
                //Check if settlement owner is the one we are looking for

                if (settlement.Username == data._username)
                {
                    data._settlementTiles.Add(settlement.Tile);
                    tempSettlementList.Add(GetSettlementGoodwill(client, settlement));
                }
            }
            data._settlementGoodwills = tempSettlementList.ToArray();

            List<Goodwill> tempSiteList = new List<Goodwill>();
            SiteFile[] sites = SiteManagerHelper.GetAllSites();
            foreach (SiteFile site in sites)
            {
                //Check if site owner is the one we are looking for

                if (site.Username == data._username)
                {
                    data._siteTiles.Add(site.Tile);
                    tempSiteList.Add(GetSiteGoodwill(client, site));
                }
            }
            data._siteGoodwills = tempSiteList.ToArray();

            UserManagerH.SaveUserFile(client.UserFile);

            client.Listener.EnqueuePacket(PacketHeader.GoodWillManager, data);
        }

        public static Goodwill GetGoodwillFromTile(ServerClient client, int tileToCheck)
        {
            SettlementFile settlementFile = SettlementManager.GetSettlementFileFromTile(tileToCheck);
            SiteFile siteFile = SiteManagerHelper.GetSiteFileFromTile(tileToCheck);

            string usernameToCheck;
            if (settlementFile != null) usernameToCheck = settlementFile.Username;
            else usernameToCheck = siteFile.Username;

            GuildFile guild = GuildManagerH.GetFactionFromFactionName(client.UserFile.GuildName);
            if (GuildManagerH.CheckIfUserIsInFaction(guild, usernameToCheck))
            {
                if (usernameToCheck == client.UserFile.Username) return Goodwill.Personal;
                else return Goodwill.Faction;
            }

            else if (client.UserFile.EnemyPlayers.Contains(usernameToCheck)) return Goodwill.Enemy;
            else if (client.UserFile.AllyPlayers.Contains(usernameToCheck)) return Goodwill.Ally;
            else return Goodwill.Neutral;
        }

        public static Goodwill GetSettlementGoodwill(ServerClient client, SettlementFile settlement)
        {
            GuildFile guild = GuildManagerH.GetFactionFromFactionName(client.UserFile.GuildName);
            if (GuildManagerH.CheckIfUserIsInFaction(guild, settlement.Username))
            {
                if (settlement.Username == client.UserFile.Username) return Goodwill.Personal;
                else return Goodwill.Faction;
            }

            else if (client.UserFile.EnemyPlayers.Contains(settlement.Username)) return Goodwill.Enemy;
            else if (client.UserFile.AllyPlayers.Contains(settlement.Username)) return Goodwill.Ally;
            else if (settlement.Username == client.UserFile.Username) return Goodwill.Personal;
            else return Goodwill.Neutral;
        }

        public static Goodwill GetSiteGoodwill(ServerClient client, SiteFile site)
        {
            if (client.UserFile.Username == site.Username) return Goodwill.Personal; //We check if the players is the owner

            if (!string.IsNullOrEmpty(site.GuildName))
            {
                GuildFile factionFile = GuildManagerH.GetFactionFromFactionName(site.GuildName);

                if (client.UserFile.GuildName == factionFile.Name) return Goodwill.Faction; // We check if the player is in the faction

                foreach (string str in client.UserFile.EnemyPlayers) // We check if the player is enemy with the faction
                {
                    if (GuildManagerH.CheckIfUserIsInFaction(factionFile, str))
                    {
                        return Goodwill.Enemy;
                    }
                }

                foreach (string str in client.UserFile.AllyPlayers) // We check if the player is allied with the faction
                {
                    if (GuildManagerH.CheckIfUserIsInFaction(factionFile, str))
                    {
                        return Goodwill.Ally;
                    }
                }
            }
            else
            {
                if (client.UserFile.EnemyPlayers.Contains(site.Username)) return Goodwill.Enemy; //We check if the player is enemy of the owner

                else if (client.UserFile.AllyPlayers.Contains(site.Username)) return Goodwill.Ally; // We check if the player is allied to the owner
            }
            return Goodwill.Neutral;
        }

        public static void ClearAllFactionMemberGoodwills(GuildFile factionFile)
        {
            ServerClient[] clients = ServerNetwork.Instance.GetConnectedClientsSafe();
            List<ServerClient> clientsToGet = new List<ServerClient>();

            GuildMember[] guildMembers = GuildManagerH.GetAllFactionMembers(factionFile);

            foreach (ServerClient client in clients)
            {
                if (GuildManagerH.CheckIfUserIsInFaction(factionFile, client.UserFile.Username)) clientsToGet.Add(client);
            }

            foreach (ServerClient client in clientsToGet)
            {
                for (int i = 0; i < guildMembers.Count(); i++)
                {
                    if (client.UserFile.EnemyPlayers.Contains(guildMembers[i].Username))
                    {
                        client.UserFile.EnemyPlayers.Remove(guildMembers[i].Username);
                    }

                    else if (client.UserFile.AllyPlayers.Contains(guildMembers[i].Username))
                    {
                        client.UserFile.AllyPlayers.Remove(guildMembers[i].Username);
                    }
                }
            }

            UserFile[] userFiles = UserManagerH.GetAllUserFiles();
            List<UserFile> usersToGet = new List<UserFile>();

            foreach (UserFile file in userFiles)
            {
                if (GuildManagerH.CheckIfUserIsInFaction(factionFile, file.Username)) usersToGet.Add(file);
            }

            foreach (UserFile file in usersToGet)
            {
                for (int i = 0; i < guildMembers.Count(); i++)
                {
                    if (file.EnemyPlayers.Contains(guildMembers[i].Username))
                    {
                        file.EnemyPlayers.Remove(guildMembers[i].Username);
                    }

                    else if (file.AllyPlayers.Contains(guildMembers[i].Username))
                    {
                        file.AllyPlayers.Remove(guildMembers[i].Username);
                    }
                }

                UserManagerH.SaveUserFile(file);
            }
        }

        public static void UpdateClientGoodwills(ServerClient client)
        {
            SettlementFile[] settlements = SettlementManager.GetAllSettlements();

            FactionGoodwillData factionGoodwillData = new FactionGoodwillData();
            SiteFile[] sites = SiteManagerHelper.GetAllSites();

            List<Goodwill> tempList = new List<Goodwill>();
            foreach (SettlementFile settlement in settlements)
            {
                if (settlement.Username == client.UserFile.Username) continue;

                factionGoodwillData._settlementTiles.Add(settlement.Tile);
                tempList.Add(GetSettlementGoodwill(client, settlement));
            }
            factionGoodwillData._settlementGoodwills = tempList.ToArray();

            tempList = new List<Goodwill>();
            foreach (SiteFile site in sites)
            {
                factionGoodwillData._siteTiles.Add(site.Tile);
                tempList.Add(GetSiteGoodwill(client, site));
            }
            factionGoodwillData._siteGoodwills = tempList.ToArray();

            client.Listener.EnqueuePacket(PacketHeader.GoodWillManager, factionGoodwillData);
        }
    }
}