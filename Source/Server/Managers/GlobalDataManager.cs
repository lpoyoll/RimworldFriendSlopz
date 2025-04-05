using GameServer.Core;
using GameServer.TCP;
using Shared;

namespace GameServer.Managers
{
    [RTManager]
    public static class GlobalDataManager
    {
        public static void SendServerGlobalData(ServerClient client)
        {
            ServerGlobalData globalData = new ServerGlobalData();

            globalData._isClientAdmin = client.userFile.IsAdmin;
            globalData._isClientFactionMember = !string.IsNullOrEmpty(client.userFile.GuildName);

            globalData._serverValues = new ServerValuesFile(Master.serverConfig.Name);
            globalData._eventValues = EventManagerHelper.loadedEvents;
            globalData._siteValues = Master.siteValues;
            globalData._difficultyValues = Master.difficultyValues;
            globalData._scenarioValues = Master.scenarioValues;
            globalData._storytellerValues = Master.storytellerValues;
            globalData._actionValues = Master.actionConfigs;
            globalData._roadValues = Master.roadValues;
            globalData._modConfigs = Master.modConfig;

            if (Master.worldValues != null)
            {
                globalData._roads = Master.worldValues.Roads;
                globalData._pollutedTiles = Master.worldValues.PollutedTiles;
                globalData._playerSettlements = GlobalDataManagerHelper.GetServerSettlements(client);
                globalData._npcSettlements = Master.worldValues.NPCSettlements;
                globalData._playerSites = GlobalDataManagerHelper.GetServerSites(client);
            }

            client.listener.EnqueuePacket(PacketHeader.GlobalDataManager, globalData);
        }
    }

    public static class GlobalDataManagerHelper
    {
        public static SettlementFile[] GetServerSettlements(ServerClient client)
        {
            List<SettlementFile> tempList = new List<SettlementFile>();
            foreach (SettlementFile settlement in SettlementManager.GetAllSettlements())
            {
                SettlementFile file = new SettlementFile();

                if (settlement.UID == client.userFile.Uid) continue;
                else
                {
                    file.Tile = settlement.Tile;
                    file.UID = settlement.UID;
                    file.Label = settlement.Label;
                    file.Goodwill = GoodwillManager.GetSettlementGoodwill(client, settlement);

                    tempList.Add(file);
                }
            }

            return tempList.ToArray();
        }

        public static SiteFile[] GetServerSites(ServerClient client)
        {
            List<SiteFile> tempList = new List<SiteFile>();
            foreach (SiteFile site in SiteManagerHelper.GetAllSites())
            {
                SiteFile file = new SiteFile();

                file.Tile = site.Tile;
                file.UID = site.UID;
                file.Goodwill = GoodwillManager.GetSiteGoodwill(client, site);
                file.Type = site.Type;
                file.GuildName = site.GuildName;

                tempList.Add(file);
            }

            return tempList.ToArray();
        }
    }
}
