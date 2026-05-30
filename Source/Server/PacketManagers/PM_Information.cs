using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using RTShared;
using RTShared.Files;
using RTNetwork;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using RTNetwork.Components;

namespace GameServer.PacketManager
{
    public class PM_Information : PM_Base
    {
        [HandlesPacket(PacketHeader.Information)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Information data = Serializer.ConvertBytesToObject<PKT_Information>(bytes);

            switch (data._stepMode)
            {
                case PKT_Information.InfoStepMode.Connection:
                    SendInformation(client, data);
                    break;

                case PKT_Information.InfoStepMode.Wealth:
                    SendWealth(client, data);
                    break;
            }
        }

        private static void SendInformation(ServerClient client, PKT_Information data)
        {
            FL_Settlement settlementToFind = PM_Settlements.GetSettlementFileFromTile(data._settlementTile);
            ServerClient clientToFind = ServerNetwork.GetConnectedClientFromUsername(settlementToFind.Username);

            data._isPlayerOnline = clientToFind != null ? true : false;

            client.Listener.EnqueuePacket(PacketHeader.Information, data);
        }

        private static void SendWealth(ServerClient client, PKT_Information data)
        {
            if (!PM_Map.CheckIfMapExists(data._settlementTile)) ResponseShortcutManager.SendUnavailablePacket(client);
            else 
            {
                data._settlementWealth = PM_Map.GetMapFromTile(data._settlementTile).Wealth;
                client.Listener.EnqueuePacket(PacketHeader.Information, data);
            }
        }
    }
}
