using GameServer.Files;
using GameServer.Misc;
using Shared;
using Shared.Files;
using Shared.Files.Guild;
using System.Linq;
using System.Security.Policy;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;
using static Shared.CommonEnumerators;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GameServer.Managers
{

    public static class GoodwillManager
    {
        [HandlesPacket(PacketHeader.GoodWillManager)]
        private static void ParsePacket(ServerClient client, byte[] bytes, PacketHeader header)
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

            GuildFile guild = GuildManagerH.GetFactionFromName(client.UserFile.GuildName);
            if (guild != null && GuildManagerH.CheckIfUserIsInFaction(guild, data._username))
            {
                ResponseShortcutManager.SendBreakPacket(client);
                return;
            }

            client.UserFile.UpdateGoodwill(data._username, data._goodwill);

            //Goodwill from settlements

            List<Goodwill> tempSettlementList = new List<Goodwill>();
            SettlementFile[] settlements = SettlementManager.GetAllSettlements().Where(fetch => fetch.Username == data._username).ToArray();
            foreach (SettlementFile settlement in settlements)
            {
                data._settlementTiles.Add(settlement.Tile);
                tempSettlementList.Add(GetSettlementGoodwill(client, settlement));
            }
            data._settlementGoodwills = tempSettlementList.ToArray();

            //Goodwill from sites

            List<Goodwill> tempSiteList = new List<Goodwill>();
            SiteFile[] sites = SiteManagerHelper.GetAllSites().Where(fetch => fetch.Username == data._username).ToArray();
            foreach (SiteFile site in sites)
            {
                data._siteTiles.Add(site.Tile);
                tempSiteList.Add(GetSiteGoodwill(client, site));
            }
            data._siteGoodwills = tempSiteList.ToArray();

            client.Listener.EnqueuePacket(PacketHeader.GoodWillManager, data);
        }

        public static Goodwill GetSettlementGoodwill(ServerClient client, SettlementFile settlement)
        {
            GuildFile guild = GuildManagerH.GetFactionFromName(client.UserFile.GuildName);

            if (client.UserFile.Username == settlement.Username) return Goodwill.Personal;
            if (guild == null) return FindGoodwillFromUsername(client.UserFile, settlement.Username);
            else
            {
                if (GuildManagerH.GetAllFactionMembers(guild).FirstOrDefault(fetch => fetch.Username == settlement.Username) != null) return Goodwill.Faction;
                else return FindGoodwillFromUsername(client.UserFile, settlement.Username);
            }
        }

        public static Goodwill GetSiteGoodwill(ServerClient client, SiteFile site)
        {
            GuildFile guild = GuildManagerH.GetFactionFromName(client.UserFile.GuildName);

            if (client.UserFile.Username == site.Username) return Goodwill.Personal;
            else if (guild == null) return FindGoodwillFromUsername(client.UserFile, site.Username);
            else
            {
                if (GuildManagerH.GetAllFactionMembers(guild).FirstOrDefault(fetch => fetch.Username == site.Username) != null) return Goodwill.Faction;
                else return FindGoodwillFromUsername(client.UserFile, site.Username);
            }
        }

        public static Goodwill FindGoodwillFromUsername(UserFile file, string username)
        {
            if (file.PlayerGoodwills.Count == 0) return Goodwill.Neutral;
            else
            {
                PlayerGoodwill toFind = file.PlayerGoodwills.First(fetch => fetch.Name == username);
                if (toFind == null) return Goodwill.Neutral;
                else if (toFind.Name == file.Username) return Goodwill.Personal;
                else return toFind.Goodwill;
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