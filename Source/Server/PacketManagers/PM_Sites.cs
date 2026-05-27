using GameServer.Core;
using GameServer.Misc;
using Shared;
using TCPNetwork.Packets;
using TCPNetwork.Files.Client;
using Shared.Misc;
using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using static TCPNetwork.Packets.PKT_Site;
using TCPNetwork.PacketManagers;
using TCPNetwork;
using Shared.Files;

namespace GameServer.PacketManager
{
    public class PM_Sites : PM_Base
    {
        [HandlesPacket(PacketHeader.Site)]
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

                case SiteStepMode.Rewards:
                    SendRewardsToPlayer(client);
                    break;

                case SiteStepMode.Worker:
                    ManageWorker(client, data);
                    break;

                case SiteStepMode.RetrieveWorker:
                    RetrieveWorker(client, data);
                    break;
            }
        }

        public static void ConfirmNewSite(ServerClient client, FL_Site siteFile)
        {
            siteFile.SaveSite();

            PKT_Site siteData = new PKT_Site();
            siteData._stepMode = SiteStepMode.Build;
            siteData.File = siteFile;

            foreach (ServerClient cClient in ServerNetwork.GetConnectedClients())
            {
                siteData.File.Goodwill = PM_Goodwills.GetSiteGoodwill(cClient, siteFile);
                cClient.Listener.EnqueuePacket(PacketHeader.Site, siteData);
            }

            siteData._stepMode = SiteStepMode.Accept;
            client.Listener.EnqueuePacket(PacketHeader.Site, siteData);

            InformationDisplayer.DisplayAddSite(siteFile.Tile.ToString());
        }

        private static void AddNewSite(ServerClient client, PKT_Site siteData)
        {
            if (PM_Settlements.CheckIfTileIsInUse(siteData.File.Tile)) ResponseShortcutManager.SendIllegalPacket(client, $"A site tried to be added to tile {siteData.File.Tile}, but that tile already has a settlement");
            else if (CheckIfTileIsInUse(siteData.File.Tile)) ResponseShortcutManager.SendIllegalPacket(client, $"A site tried to be added to tile {siteData.File.Tile}, but that tile already has a site");
            else
            {
                FL_Site siteFile = new FL_Site();

                siteFile.Tile = siteData.File.Tile;
                siteFile.Username = client.GetData<UserFile>().Username;
                if (!string.IsNullOrEmpty(client.GetData<UserFile>().GuildName)) siteFile.GuildName = client.GetData<UserFile>().GuildName;
                ConfirmNewSite(client, siteFile);
            }
        }

        private static void DestroySite(ServerClient client, PKT_Site siteData)
        {
            FL_Site siteFile = GetSiteFileFromTile(siteData.File.Tile);
            if (siteFile.Username == client.GetData<UserFile>().Username) DestroySiteFromFile(siteFile);
            else ResponseShortcutManager.SendNoPowerPacket(client);
        }

        public static void DestroySiteFromFile(FL_Site siteFile)
        {
            PKT_Site siteData = new PKT_Site();
            siteData._stepMode = SiteStepMode.Destroy;
            siteData.File = siteFile;

            ServerNetwork.SendPacketToAllClients(PacketHeader.Site, siteData);

            File.Delete(Path.Combine(Master.SitesPath, siteFile.Tile + CommonValues.DefaultSaveFormat));

            InformationDisplayer.DisplayRemoveSite(siteFile.Tile.ToString());
        }

        private static void ManageWorker(ServerClient client, PKT_Site packet)
        {
            if (string.IsNullOrWhiteSpace(packet.File.WorkerString))
            {
                FL_Site siteFile = GetSiteFileFromTile(packet.File.Tile);
                packet._stepMode = SiteStepMode.Worker;
                packet.File = siteFile;

                client.Listener.EnqueuePacket(PacketHeader.Site, packet);
            }

            else
            {
                FL_Site site = GetSiteFileFromTile(packet.File.Tile);
                site.WorkerString = packet.File.WorkerString;
                site.SaveSite();
            }
        }

        private static void RetrieveWorker(ServerClient client, PKT_Site packet)
        {
            FL_Site site = GetSiteFileFromTile(packet.File.Tile);
            site.WorkerString = string.Empty;
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
            List<FL_Site> availableSites = GetPlayerRewardableSites(client);

            if (availableSites.Count > 0)
            {
                PKT_Site siteData = new PKT_Site();
                siteData._stepMode = SiteStepMode.Rewards;
                siteData.Files = availableSites;

                client.Listener.EnqueuePacket(PacketHeader.Site, siteData);
            }
        }

        private static List<FL_Site> GetPlayerRewardableSites(ServerClient client)
        {
            return GetAllSites().Where(fetch => (fetch.Username == client.GetData<UserFile>().Username ||
                (client.GetData<UserFile>().GuildName != null && client.GetData<UserFile>().GuildName == fetch.GuildName)) &&
                    !string.IsNullOrEmpty(fetch.WorkerString)).ToList();
        }

        public static List<FL_Site> GetSitesFromGoodwill(ServerClient client)
        {
            List<FL_Site> tempList = new List<FL_Site>();
            foreach (FL_Site site in GetAllSites())
            {
                FL_Site file = new FL_Site();

                file.Tile = site.Tile;
                file.Username = site.Username;
                file.Goodwill = PM_Goodwills.GetSiteGoodwill(client, site);
                file.GuildName = site.GuildName;

                tempList.Add(file);
            }

            return tempList;
        }

        public static FL_Site[] GetAllSitesFromUsername(string username)
        {
            List<FL_Site> sitesList = new List<FL_Site>();

            string[] sites = Directory.GetFiles(Master.SitesPath);
            foreach (string site in sites)
            {
                FL_Site siteFile = Serializer.SerializeFromFile<FL_Site>(site);
                if (siteFile.Username == username) sitesList.Add(siteFile);
            }

            return sitesList.ToArray();
        }

        public static FL_Site GetSiteFileFromTile(int tileToGet)
        {
            string[] sites = Directory.GetFiles(Master.SitesPath);
            foreach (string site in sites)
            {
                FL_Site siteFile = Serializer.SerializeFromFile<FL_Site>(site);
                if (siteFile.Tile == tileToGet) return siteFile;
            }

            return null;
        }

        public static FL_Site[] GetAllSites()
        {
            List<FL_Site> sitesList = new List<FL_Site>();
            try
            {
                string[] sites = Directory.GetFiles(Master.SitesPath);
                foreach (string site in sites) sitesList.Add(Serializer.SerializeFromFile<FL_Site>(site));
            }
            catch (Exception ex) { Printer.Error($"Sites could not be loaded, either your formatting is wrong in the file 'SiteConfig.json' or you have not updated your sites to the newest version ('Update' command).\n\n{ex.ToString()}"); }

            return sitesList.ToArray();
        }

        public static bool CheckIfTileIsInUse(int tileToCheck)
        {
            string[] sites = Directory.GetFiles(Master.SitesPath);
            foreach (string site in sites)
            {
                FL_Site siteFile = Serializer.SerializeFromFile<FL_Site>(site);
                if (siteFile.Tile == tileToCheck) return true;
            }

            return false;
        }
    }
}
