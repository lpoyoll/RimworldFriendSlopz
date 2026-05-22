using GameClient.Managers;
using GameClient.Misc;
using Shared;
using TCPNetwork;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;

namespace GameClient.PacketManagers
{
    public class PM_GlobalData : PM_Base
    {
        [HandlesPacket(PacketHeader.GlobalDataManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_ServerGlobalData serverGlobalData = Serializer.ConvertBytesToObject<PKT_ServerGlobalData>(bytes);
            SessionHandler.GlobalData = serverGlobalData;

            SessionHandler.SetValues();
            PM_GameParameter.SetValues();
            PM_Sites.SetValues();
            PM_RoadsHelper.SetValues();
            PM_Mods.SetValues(SessionHandler.GlobalData.ModConfigs);
            PM_Events.SetValues(SessionHandler.GlobalData.EventValues);
        }
    }
}