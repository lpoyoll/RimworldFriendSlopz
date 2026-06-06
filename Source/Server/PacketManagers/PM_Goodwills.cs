using GameServer.Managers;
using RTShared;
using RTShared.Files;
using RTNetwork;
using RTShared.Files.ServerClient;
using RTNetwork.PacketManagers;
using RTNetwork.Packets.Goodwills;
using static RTShared.CommonEnumerators;
using RTNetwork.Components;

namespace GameServer.PacketManager
{
    public class PM_Goodwills : PM_Base
    {
        [HandlesPacket(PacketHeader.GoodWill)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_FactionGoodwill data = Serializer.ConvertBytesToObject<PKT_FactionGoodwill>(bytes);

            ChangeUserGoodwills(client, data);
        }

        public static void ChangeUserGoodwills(ServerClient client, PKT_FactionGoodwill data)
        {
            FL_Settlement settlementFile = PM_Settlements.GetSettlementFileFromTile(data._tile);
            FL_Site siteFile = PM_Sites.GetSiteFileFromTile(data._tile);

            if (settlementFile != null) data._username = settlementFile.Username;
            else data._username = siteFile.Username;

            FL_Guild guild = GuildManagerH.GetGuildFromName(client.GetData<FL_Player>().GuildName);
            if (guild != null && GuildManagerH.CheckIfUserIsInGuild(guild, data._username))
            {
                ResponseShortcutManager.SendBreakPacket(client);
                return;
            }

            client.GetData<FL_Player>().UpdateGoodwill(data._username, data._goodwill);
            UpdateClientGoodwills(client);
        }

        public static void UpdateClientGoodwills(ServerClient client)
        {
            FL_Settlement[] settlements = PM_Settlements.GetAllSettlements().Where(fetch => fetch.Username != client.GetData<FL_Player>().Username).ToArray();
            FL_Site[] sites = PM_Sites.GetAllSites().Where(fetch => fetch.Username != client.GetData<FL_Player>().Username).ToArray();

            PKT_FactionGoodwill factionGoodwillData = new PKT_FactionGoodwill();
            foreach (FL_Settlement settlement in settlements)
            {
                PKT_SettlementGoodwill goodwill = new PKT_SettlementGoodwill();
                goodwill.Tile = settlement.Tile;
                goodwill.Goodwill = GetSettlementGoodwill(client, settlement);

                factionGoodwillData._settlements.Add(goodwill);
            }

            foreach (FL_Site site in sites)
            {
                PKT_SiteGoodwill goodwill = new PKT_SiteGoodwill();
                goodwill.Tile = site.Tile;
                goodwill.Goodwill = GetSiteGoodwill(client, site);

                factionGoodwillData._sites.Add(goodwill);
            }

            client.Listener.EnqueuePacket(PacketHeader.GoodWill, factionGoodwillData);
        }

        public static Goodwill GetSettlementGoodwill(ServerClient client, FL_Settlement settlement)
        {
            FL_Guild guild = GuildManagerH.GetGuildFromName(client.GetData<FL_Player>().GuildName);

            if (client.GetData<FL_Player>().Username == settlement.Username) return Goodwill.Personal;
            else if (guild == null) return FindGoodwillFromUsername(client.GetData<FL_Player>(), settlement.Username);
            else
            {
                if (GuildManagerH.GetAllGuildMembers(guild).FirstOrDefault(fetch => fetch.Username == settlement.Username) != null) return Goodwill.Guild;
                else return FindGoodwillFromUsername(client.GetData<FL_Player>(), settlement.Username);
            }
        }

        public static Goodwill GetSiteGoodwill(ServerClient client, FL_Site site)
        {
            FL_Guild guild = GuildManagerH.GetGuildFromName(client.GetData<FL_Player>().GuildName);

            if (client.GetData<FL_Player>().Username == site.Username) return Goodwill.Personal;
            else if (guild == null) return FindGoodwillFromUsername(client.GetData<FL_Player>(), site.Username);
            else
            {
                if (GuildManagerH.GetAllGuildMembers(guild).FirstOrDefault(fetch => fetch.Username == site.Username) != null) return Goodwill.Guild;
                else return FindGoodwillFromUsername(client.GetData<FL_Player>(), site.Username);
            }
        }

        public static Goodwill FindGoodwillFromUsername(FL_Player file, string username)
        {
            if (file.Goodwills.Count == 0) return Goodwill.Neutral;
            else
            {
                FL_PlayerGoodwill toFind = file.Goodwills.FirstOrDefault(fetch => fetch.Name == username);
                if (toFind == null) return Goodwill.Neutral;
                else if (toFind.Name == file.Username) return Goodwill.Personal;
                else return toFind.Goodwill;
            }
        }
    }
}