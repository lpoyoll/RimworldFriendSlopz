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
            globalData.IsClientAdmin = client.GetOrSetClientData<UserFile>().IsAdmin;
            globalData.IsClientFactionMember = GuildManagerH.GetFactionFromName(client.GetOrSetClientData<UserFile>().GuildName) != null;
            globalData.ActionValues = Master.ActionConfigs;
            globalData.RoadValues = Master.ActionConfigs.RoadsAction.RoadValues;
            globalData.SiteValues = Master.ActionConfigs.SiteAction.SiteTypes;
            globalData.PlayerSettlements = PM_Settlements.GetSettlementsFromGoodwill(client);
            globalData.PlayerSites = PM_Sites.GetSitesFromGoodwill(client);
            globalData.ScenarioValues = Master.ScenarioValues;
            globalData.DifficultyValues = Master.DifficultyValues;
            globalData.StorytellerValues = Master.StorytellerValues;
            globalData.ModConfigs = Master.ModConfig.ModConfigs;
            globalData.EventValues = EventManagerH.LoadedEvents;

            if (Master.WorldValues != null)
            {
                globalData.Roads = Master.WorldValues.Roads;
                globalData.PollutedTiles = Master.WorldValues.PollutedTiles;
                globalData._npcSettlements = Master.WorldValues.NPCSettlements;
            }

            client.Listener.EnqueuePacket(PacketHeader.GlobalDataManager, globalData);
        }
    }
}
