using GameServer.Core;
using GameServer.PacketManager;
using Shared;
using Shared.Files;
using Shared.Files.Sites;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;

namespace GameServer.Managers
{
    public class GlobalDataManager
    {
        public static void SendServerGlobalData(ServerClient client)
        {
            PKT_ServerGlobalData globalData = new PKT_ServerGlobalData();
            globalData._serverName = Master.ServerConfig.Name;
            globalData._isClientAdmin = client.UserFile.IsAdmin;
            globalData._isClientFactionMember = GuildManagerH.GetFactionFromName(client.UserFile.GuildName) != null;
            globalData._actionValues = Master.ActionConfigs;
            globalData._roadValues = Master.ActionConfigs.RoadsAction.RoadValues;
            globalData._siteValues = Master.ActionConfigs.SiteAction.SiteTypes;
            globalData._playerSettlements = PM_Settlements.GetSettlementsFromGoodwill(client);
            globalData._playerSites = PM_Sites.GetSitesFromGoodwill(client);
            globalData._scenarioValues = Master.ScenarioValues;
            globalData._difficultyValues = Master.DifficultyValues;
            globalData._storytellerValues = Master.StorytellerValues;

            if (Master.WorldValues != null)
            {
                globalData._modConfigs = Master.ModConfig;
                globalData._roads = Master.WorldValues.Roads;
                globalData._pollutedTiles = Master.WorldValues.PollutedTiles;
                globalData._npcSettlements = Master.WorldValues.NPCSettlements;
                globalData._eventValues = EventManagerH.LoadedEvents;
            }

            client.Listener.EnqueuePacket(PacketHeader.GlobalDataManager, globalData);
        }
    }
}
