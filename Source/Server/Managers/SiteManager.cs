using GameServer.Core;
using GameServer.Misc;
using Shared;
using static Shared.CommonEnumerators;
using Shared.Files;
using TCPNetwork.Packets;
using Shared.Files.Guild;
using TCPNetwork.Files.Client;

namespace GameServer.Managers
{
    public static class SiteManager
    {
        [HandlesPacket(PacketHeader.SiteManager)]
        private static void ParsePacket(ServerClient client, byte[] bytes, PacketHeader header)
        {
            if (!Master.ActionConfigs.SiteAction.IsEnabled)
            {
                ResponseShortcutManager.SendIllegalPacket(client, "Tried to use disabled feature!");
                return;
            }

            SiteData data = Serializer.ConvertBytesToObject<SiteData>(bytes);

            Printer.Warning(data, LogImportanceMode.Extreme);

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

            }
        }

        public static void ConfirmNewSite(ServerClient client, SiteFile siteFile)
        {
            SiteManagerHelper.SaveSite(siteFile);

            SiteData siteData = new SiteData();
            siteData._stepMode = SiteStepMode.Build;
            siteData._file = siteFile;

            foreach (ServerClient cClient in ServerNetwork.Instance.GetConnectedClientsSafe())
            {
                siteData._file.Goodwill = GoodwillManager.GetSiteGoodwill(cClient, siteFile);
                cClient.Listener.EnqueuePacket(PacketHeader.SiteManager, siteData);
            }

            siteData._stepMode = SiteStepMode.Accept;
            client.Listener.EnqueuePacket(PacketHeader.SiteManager, siteData);

            InformationDisplayer.DisplayAddSite(siteFile.Tile.ToString());
        }

        private static void AddNewSite(ServerClient client, SiteData siteData)
        {
            if (SettlementManager.CheckIfTileIsInUse(siteData._file.Tile)) ResponseShortcutManager.SendIllegalPacket(client, $"A site tried to be added to tile {siteData._file.Tile}, but that tile already has a settlement");
            else if (SiteManagerHelper.CheckIfTileIsInUse(siteData._file.Tile)) ResponseShortcutManager.SendIllegalPacket(client, $"A site tried to be added to tile {siteData._file.Tile}, but that tile already has a site");
            else
            {
                SiteFile siteFile = new SiteFile();

                siteFile.Tile = siteData._file.Tile;
                siteFile.Username = client.UserFile.Username;
                siteFile.Type = SiteManagerHelper.GetTypeFromDef(siteData._file.Type.DefName);
                if (!string.IsNullOrEmpty(client.UserFile.GuildName)) siteFile.GuildName = client.UserFile.GuildName;
                ConfirmNewSite(client, siteFile);
            }
        }

        private static void DestroySite(ServerClient client, SiteData siteData)
        {
            SiteFile siteFile = SiteManagerHelper.GetSiteFileFromTile(siteData._file.Tile);
            if (siteFile.Username == client.UserFile.Username) DestroySiteFromFile(siteFile);
            else return;
        }

        private static void VisitSite(ServerClient client, SiteData siteData)
        {
            if (MapManager.CheckIfMapExists(siteData._file.Tile)) siteData._siteMap = MapManager.GetMapFromTile(siteData._file.Tile);
            else siteData._siteMap = null;

            client.Listener.EnqueuePacket(PacketHeader.SiteManager, siteData);
        }

        private static void RaidSite(ServerClient client, SiteData siteData)
        {
            if (MapManager.CheckIfMapExists(siteData._file.Tile)) siteData._siteMap = MapManager.GetMapFromTile(siteData._file.Tile);
            else siteData._siteMap = null;

            client.Listener.EnqueuePacket(PacketHeader.SiteManager, siteData);
        }

        public static void DestroySiteFromFile(SiteFile siteFile)
        {
            SiteData siteData = new SiteData();
            siteData._stepMode = SiteStepMode.Destroy;
            siteData._file = siteFile;

            ServerNetwork.Instance.SendPacketToAllClients(PacketHeader.SiteManager, siteData);

            File.Delete(Path.Combine(Master.SitesPath, siteFile.Tile + CommonValues.DefaultSaveFormat));

            InformationDisplayer.DisplayRemoveSite(siteFile.Tile.ToString());
        }

        public static void StartSiteTicker()
        {
            while (true)
            {
                Thread.Sleep(TimeSpan.FromMinutes(Master.ActionConfigs.SiteAction.TimeIntervalMinutes));

                try { SiteRewardTick(); }
                catch (Exception e) { Printer.Error($"Site tick failed, this should never happen. Exception > {e}"); }
            }
        }

        public static void SiteRewardTick()
        {
            SiteFile[] sites = SiteManagerHelper.GetAllSites();

            foreach (ServerClient client in ServerNetwork.Instance.GetConnectedClientsSafe())
            {
                List<SiteRewardFile> rewards = new List<SiteRewardFile>();

                // Get player specific sites
                List<SiteFile> sitesToAdd = new List<SiteFile>();
                if (string.IsNullOrEmpty(client.UserFile.GuildName)) sitesToAdd = sites.ToList().FindAll(fetch => fetch.Username == client.UserFile.Username);
                else sitesToAdd.AddRange(sites.ToList().FindAll(fetch => fetch.GuildName == client.UserFile.GuildName));

                foreach (SiteFile site in sitesToAdd)
                {
                    SiteRewardFile rewardFile = new SiteRewardFile();
                    foreach (SiteRewardFile reward in site.Type.Rewards)
                    {
                        if (client.UserFile.SiteConfigs.Any(S => S.RewardDefName == reward.DefName))
                        {
                            rewardFile.DefName = reward.DefName;
                            rewardFile.Amount = reward.Amount;
                        }
                    }

                    if (rewardFile.DefName == "") rewardFile = site.Type.Rewards.First();

                    rewards.Add(rewardFile);
                }

                if (rewards.Count == 0) continue;
                else
                {
                    SiteData siteData = new SiteData();
                    siteData._stepMode = SiteStepMode.Rewards;
                    siteData._rewardFiles = rewards.ToArray();

                    client.Listener.EnqueuePacket(PacketHeader.SiteManager, siteData);
                }
            }

            InformationDisplayer.DisplaySiteTick();
        }

        public static void ChangeUserSiteConfig(ServerClient client, SiteData data)
        {
            SiteRewardConfigData config = data._rewardConfig;
            SiteConfigFile toModify = client.UserFile.SiteConfigs.First(fetch => fetch.DefName == config._siteDef);
            toModify.RewardDefName = config._rewardDef;

            client.UserFile.SaveUserFile();
        }

        public static void SetSiteInfoForClient(ServerClient client)
        {
            if (client.UserFile.SiteConfigs.Length > 0) return;
            else
            {
                List<SiteConfigFile> configFiles = new List<SiteConfigFile>();
                for (int i = 0; i < Master.ActionConfigs.SiteAction.SiteTypes.Length; i++)
                {
                    SiteConfigFile toAdd = new SiteConfigFile();
                    toAdd.DefName = Master.ActionConfigs.SiteAction.SiteTypes[i].DefName;
                    toAdd.RewardDefName = Master.ActionConfigs.SiteAction.SiteTypes[i].Rewards.First().DefName;

                    configFiles.Add(toAdd);
                }

                client.UserFile.UpdateSiteConfigs(configFiles.ToArray());
            }
        }
    }

    public static class SiteManagerHelper
    {
        public static void SaveSite(SiteFile siteFile)
        {
            siteFile.SavingSemaphore.WaitOne();

            try { Serializer.SerializeToFile(Path.Combine(Master.SitesPath, siteFile.Tile + CommonValues.DefaultSaveFormat), siteFile); }
            catch (Exception e) { Printer.Error(e.ToString()); }

            siteFile.SavingSemaphore.Release();
        }

        public static void UpdateFaction(SiteFile siteFile, GuildFile toUpdateWith)
        {
            if (toUpdateWith == null) siteFile.GuildName = null;
            else siteFile.GuildName = toUpdateWith.Name;
            SaveSite(siteFile);
        }

        public static SiteFile[] GetAllSitesFromUsername(string username)
        {
            List<SiteFile> sitesList = new List<SiteFile>();

            string[] sites = Directory.GetFiles(Master.SitesPath);
            foreach (string site in sites)
            {
                SiteFile siteFile = Serializer.SerializeFromFile<SiteFile>(site);
                if (siteFile.Username == username) sitesList.Add(siteFile);
            }

            return sitesList.ToArray();
        }

        public static SiteFile GetSiteFileFromTile(int tileToGet)
        {
            string[] sites = Directory.GetFiles(Master.SitesPath);
            foreach (string site in sites)
            {
                SiteFile siteFile = Serializer.SerializeFromFile<SiteFile>(site);
                if (siteFile.Tile == tileToGet) return siteFile;
            }

            return null;
        }

        public static void GetSiteInfo(ServerClient client, SiteData siteData)
        {
            SiteFile siteFile = GetSiteFileFromTile(siteData._file.Tile);
            siteData._file = siteFile;

            client.Listener.EnqueuePacket(PacketHeader.SiteManager, siteData);
        }

        public static SiteFile[] GetAllSites()
        {
            List<SiteFile> sitesList = new List<SiteFile>();
            try
            {
                string[] sites = Directory.GetFiles(Master.SitesPath);
                foreach (string site in sites) sitesList.Add(Serializer.SerializeFromFile<SiteFile>(site));
            }
            catch (Exception ex) { Printer.Error($"Sites could not be loaded, either your formatting is wrong in the file 'SiteConfig.json' or you have not updated your sites to the newest version ('Update' command).\n\n{ex.ToString()}"); }

            return sitesList.ToArray();
        }

        public static bool CheckIfTileIsInUse(int tileToCheck)
        {
            string[] sites = Directory.GetFiles(Master.SitesPath);
            foreach (string site in sites)
            {
                SiteFile siteFile = Serializer.SerializeFromFile<SiteFile>(site);
                if (siteFile.Tile == tileToCheck) return true;
            }

            return false;
        }

        public static SiteInfoFile GetTypeFromDef(string defName)
        {
            SiteInfoFile site = Master.ActionConfigs.SiteAction.SiteTypes.Where(S => S.DefName == defName).FirstOrDefault();
            if (site != null) return site;
            return null;
        }

        public static void SetSitePresets()
        {
            if (Master.ActionConfigs.SiteAction.SiteTypes.Length > 0) return;
            else
            {

            }
        }
    }
}
