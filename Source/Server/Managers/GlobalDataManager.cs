using GameServer.Core;
using GameServer.PacketManager;
using GameServer.PacketManagers;
using RTShared;
using RTShared.Files;
using RTNetwork;
using RTShared.Files.ServerClient;
using RTNetwork.Packets;
using RTNetwork.Components;

namespace GameServer.Managers
{
    public class GlobalDataManager
    {
        public static void SendServerGlobalData(ServerClient client)
        {
            PKT_ServerGlobalData globalData = new PKT_ServerGlobalData();
            globalData.IsClientAdmin = client.GetData<FL_Player>().IsAdmin;
            globalData.IsClientFactionMember = GuildManagerH.GetGuildFromName(client.GetData<FL_Player>().GuildName) != null;
            globalData.ActionValues = Master.ActionConfigs;
            globalData.RoadValues = Master.ActionConfigs.RoadAction.RoadValues;
            globalData.WorldObjects = PM_WorldObject.GetAllWorldObjects();
            globalData.PlayerSettlements = PM_Settlements.GetSettlementsFromGoodwill(client);
            globalData.PlayerSites = PM_Sites.GetSitesFromGoodwill(client);
            globalData.ScenarioValues = Master.ScenarioValues;
            globalData.DifficultyValues = Master.DifficultyValues;
            globalData.StorytellerValues = Master.StorytellerValues;
            globalData.ModConfigs = Master.ModConfig.ModConfigs;
            globalData.EventValues = PM_Events.LoadedEvents;

            if (Master.WorldValues != null)
            {
                globalData.Roads = Master.WorldValues.Roads;
                globalData.PollutedTiles = Master.WorldValues.PollutedTiles;
            }

            client.Listener.EnqueuePacket(PacketHeader.GlobalData, globalData);
        }
    }
}
