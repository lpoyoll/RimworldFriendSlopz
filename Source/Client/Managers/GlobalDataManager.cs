using GameClient.Files;
using GameClient.Misc;
using Shared;
using TCPNetwork.Packets;

namespace GameClient.Managers
{
    public static class GlobalDataManager
    {
        [HandlesPacket(PacketHeader.GlobalDataManager)]
        private static void ParsePacket(byte[] bytes)
        {
            ServerGlobalData serverGlobalData = Serializer.ConvertBytesToObject<ServerGlobalData>(bytes);

            SessionHandler.SetValues(serverGlobalData);
            EventManagerH.SetValues(serverGlobalData);
            GameParameterManager.SetValues(serverGlobalData);
            PlayerSettlementManagerHelper.SetValues(serverGlobalData);
            NPCManagerH.SetValues(serverGlobalData);
            SiteManagerH.SetValues(serverGlobalData);
            RoadManagerHelper.SetValues(serverGlobalData);
            PollutionManagerHelper.SetValues(serverGlobalData);
            ModManager.ReceiveModConfigs(serverGlobalData);
        }
    }
}