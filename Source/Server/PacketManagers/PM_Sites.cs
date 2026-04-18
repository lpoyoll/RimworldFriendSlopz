using GameServer.Core;
using GameServer.Misc;
using Shared;
using TCPNetwork.Packets;
using TCPNetwork.Files.Client;
using Shared.Files.Sites;
using Shared.Misc;
using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using static TCPNetwork.Packets.PKT_Site;
using TCPNetwork.PacketManagers;

namespace GameServer.PacketManager
{
    public class PM_Sites : PM_Base
    {
        [HandlesPacket(PacketHeader.SiteManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            if (!Master.ActionConfigs.SiteAction.IsEnabled)
            {
                ResponseShortcutManager.SendIllegalPacket(client, "Tried to use disabled feature!");
                return;
            }

            PKT_Site data = Serializer.ConvertBytesToObject<PKT_Site>(bytes);

            switch (data._stepMode)
            {
                case SiteStepMode.Build:
                    AddNewSite(client, data);
                    break;

                case SiteStepMode.Destroy:
                    DestroySite(client, data);
                    break;

                case SiteStepMode.Info:
                    SiteManagerHelper.GetSiteInfo(client, data);
                    break;

                case SiteStepMode.Config:
                    ChangeUserSiteConfig(client, data);
                    break;

                case SiteStepMode.Rewards:
                    SendRewardsToPlayer(client);
                    break;

                case SiteStepMode.Worker:
                    ManageWorker(client, data);
                    break;
            }
        }

        public static void ConfirmNewSite(ServerClient client, Site siteFile)
        {
            siteFile.SaveSite();

            PKT_Site siteData = new PKT_Site();
            siteData._stepMode = SiteStepMode.Build;
            siteData._file = siteFile;

            foreach (ServerClient cClient in ServerNetwork.GetConnectedClients())
            {
                siteData._file.Goodwill = PM_Goodwills.GetSiteGoodwill(cClient, siteFile);
                cClient.Listener.EnqueuePacket(PacketHeader.SiteManager, siteData);
            }

            siteData._stepMode = SiteStepMode.Accept;
            client.Listener.EnqueuePacket(PacketHeader.SiteManager, siteData);

            InformationDisplayer.DisplayAddSite(siteFile.Tile.ToString());
        }

        private static void AddNewSite(ServerClient client, PKT_Site siteData)
        {
            if (PM_Settlements.CheckIfTileIsInUse(siteData._file.Tile)) ResponseShortcutManager.SendIllegalPacket(client, $"A site tried to be added to tile {siteData._file.Tile}, but that tile already has a settlement");
            else if (SiteManagerHelper.CheckIfTileIsInUse(siteData._file.Tile)) ResponseShortcutManager.SendIllegalPacket(client, $"A site tried to be added to tile {siteData._file.Tile}, but that tile already has a site");
            else
            {
                Site siteFile = new Site();

                siteFile.Tile = siteData._file.Tile;
                siteFile.Username = client.GetOrSetClientData<UserFile>().Username;
                siteFile.Type = SiteManagerHelper.GetTypeFromDef(siteData._file.Type.DefName);
                if (!string.IsNullOrEmpty(client.GetOrSetClientData<UserFile>().GuildName)) siteFile.GuildName = client.GetOrSetClientData<UserFile>().GuildName;
                ConfirmNewSite(client, siteFile);
            }
        }

        private static void DestroySite(ServerClient client, PKT_Site siteData)
        {
            Site siteFile = SiteManagerHelper.GetSiteFileFromTile(siteData._file.Tile);
            if (siteFile.Username == client.GetOrSetClientData<UserFile>().Username) DestroySiteFromFile(siteFile);
            else ResponseShortcutManager.SendNoPowerPacket(client);
        }

        public static void DestroySiteFromFile(Site siteFile)
        {
            PKT_Site siteData = new PKT_Site();
            siteData._stepMode = SiteStepMode.Destroy;
            siteData._file = siteFile;

            ServerNetwork.SendPacketToAllClients(PacketHeader.SiteManager, siteData);

            File.Delete(Path.Combine(Master.SitesPath, siteFile.Tile + CommonValues.DefaultSaveFormat));

            InformationDisplayer.DisplayRemoveSite(siteFile.Tile.ToString());
        }

        private static void ManageWorker(ServerClient client, PKT_Site data)
        {
            Site site = SiteManagerHelper.GetSiteFileFromTile(data._file.Tile);
            site.WorkerString = data._file.WorkerString;
            site.SaveSite();
        }

        public static void SendRewardsToEveryPlayer()
        {
            foreach (ServerClient client in ServerNetwork.GetConnectedClients())
            {
                SendRewardsToPlayer(client);
            }
        }

        public static void SendRewardsToPlayer(ServerClient client)
        {
            Site[] availableSites = SiteManagerHelper.GetAllSites().Where(fetch => (fetch.Username == client.GetOrSetClientData<UserFile>().Username ||
                (client.GetOrSetClientData<UserFile>().GuildName != null && client.GetOrSetClientData<UserFile>().GuildName == fetch.GuildName)) && !string.IsNullOrEmpty(fetch.WorkerString)).ToArray();

            if (availableSites.Length > 0)
            {
                List<SiteReward> toReward = new List<SiteReward>();
                foreach (Site site in availableSites) toReward.Add(client.GetOrSetClientData<UserFile>().SiteConfigs.First(fetch => fetch.DefName == site.Type.DefName).Reward);

                PKT_Site siteData = new PKT_Site();
                siteData._stepMode = SiteStepMode.Rewards;
                siteData._rewardFiles = toReward.ToArray();
                client.Listener.EnqueuePacket(PacketHeader.SiteManager, siteData);
            }
        }

        public static void ChangeUserSiteConfig(ServerClient client, PKT_Site data)
        {
            PKT_SiteRewardConfig config = data._rewardConfig;

            PlayerSiteConfig toFind = client.GetOrSetClientData<UserFile>().SiteConfigs.First(fetch => fetch.DefName == config._siteDef);
            toFind.Reward.DefName = config._rewardDef;

            SiteType type = Master.ActionConfigs.SiteAction.SiteTypes.First(fetch => fetch.DefName == config._siteDef);
            toFind.Reward.Amount = type.Rewards.First(fetch => fetch.DefName == config._rewardDef).Amount;

            client.GetOrSetClientData<UserFile>().SaveUserFile();
        }

        public static void SetSiteInfoForClient(ServerClient client) { client.GetOrSetClientData<UserFile>().UpdateSiteConfigs(Master.ActionConfigs.SiteAction.SiteTypes); }

        public static List<Site> GetSitesFromGoodwill(ServerClient client)
        {
            List<Site> tempList = new List<Site>();
            foreach (Site site in SiteManagerHelper.GetAllSites())
            {
                Site file = new Site();

                file.Tile = site.Tile;
                file.Username = site.Username;
                file.Goodwill = PM_Goodwills.GetSiteGoodwill(client, site);
                file.Type = site.Type;
                file.GuildName = site.GuildName;

                tempList.Add(file);
            }

            return tempList;
        }
    }

    public static class SiteManagerHelper
    {
        public static Site[] GetAllSitesFromUsername(string username)
        {
            List<Site> sitesList = new List<Site>();

            string[] sites = Directory.GetFiles(Master.SitesPath);
            foreach (string site in sites)
            {
                Site siteFile = Serializer.SerializeFromFile<Site>(site);
                if (siteFile.Username == username) sitesList.Add(siteFile);
            }

            return sitesList.ToArray();
        }

        public static Site GetSiteFileFromTile(int tileToGet)
        {
            string[] sites = Directory.GetFiles(Master.SitesPath);
            foreach (string site in sites)
            {
                Site siteFile = Serializer.SerializeFromFile<Site>(site);
                if (siteFile.Tile == tileToGet) return siteFile;
            }

            return null;
        }

        public static void GetSiteInfo(ServerClient client, PKT_Site data)
        {
            Site siteFile = GetSiteFileFromTile(data._file.Tile);
            data._stepMode = SiteStepMode.Info;
            data._file = siteFile;

            client.Listener.EnqueuePacket(PacketHeader.SiteManager, data);
        }

        public static Site[] GetAllSites()
        {
            List<Site> sitesList = new List<Site>();
            try
            {
                string[] sites = Directory.GetFiles(Master.SitesPath);
                foreach (string site in sites) sitesList.Add(Serializer.SerializeFromFile<Site>(site));
            }
            catch (Exception ex) { Printer.Error($"Sites could not be loaded, either your formatting is wrong in the file 'SiteConfig.json' or you have not updated your sites to the newest version ('Update' command).\n\n{ex.ToString()}"); }

            return sitesList.ToArray();
        }

        public static bool CheckIfTileIsInUse(int tileToCheck)
        {
            string[] sites = Directory.GetFiles(Master.SitesPath);
            foreach (string site in sites)
            {
                Site siteFile = Serializer.SerializeFromFile<Site>(site);
                if (siteFile.Tile == tileToCheck) return true;
            }

            return false;
        }

        public static SiteType GetTypeFromDef(string defName)
        {
            SiteType site = Master.ActionConfigs.SiteAction.SiteTypes.Where(S => S.DefName == defName).FirstOrDefault();
            if (site != null) return site;
            return null;
        }
    }
}
