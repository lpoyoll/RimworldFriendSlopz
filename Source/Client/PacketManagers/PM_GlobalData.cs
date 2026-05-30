using GameClient.Managers;
using RTShared;
using RTNetwork;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using RTNetwork.Components;

namespace GameClient.PacketManagers
{
    public class PM_GlobalData : PM_Base
    {
        [HandlesPacket(PacketHeader.GlobalData)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_ServerGlobalData serverGlobalData = Serializer.ConvertBytesToObject<PKT_ServerGlobalData>(bytes);
            SessionManager.GlobalData = serverGlobalData;

            SessionManager.SetValues();
            PM_GameParameter.SetValues();
            PM_RoadsHelper.SetValues();
            PM_Mods.SetValues(SessionManager.GlobalData.ModConfigs);
            PM_Events.SetValues(SessionManager.GlobalData.EventValues);
        }
    }
}