using GameClient.Core.Preferences;
using GameClient.TCP;
using GameClient.Values;
using Shared;

namespace GameClient.Managers
{
    [RTManager]
    public static class GlobalDataManager
    {
        private static void ParsePacket(Packet packet)
        {
            ServerGlobalData serverGlobalData = Serializer.ConvertBytesToObject<ServerGlobalData>(packet.Contents);

            ClientValues.SetValues(serverGlobalData);
            SessionValues.SetValues(serverGlobalData);
            EventManagerHelper.SetValues(serverGlobalData);
            GameParameterManager.SetValues(serverGlobalData);
            PlayerSettlementManagerHelper.SetValues(serverGlobalData);
            NPCSettlementManagerHelper.SetValues(serverGlobalData);
            SiteManagerH.SetValues(serverGlobalData);
            RoadManagerHelper.SetValues(serverGlobalData);
            PollutionManagerHelper.SetValues(serverGlobalData);
            ModManager.ReceiveMods(serverGlobalData);
            RecentServersHandler.AddServerToList(serverGlobalData._serverValues.ServerName, $"{Network.ip}:{Network.port}");
        }
    }
}