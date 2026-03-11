using GameServer.Hooks.TCPNetwork;
using Shared;
using Shared.Files;
using Shared.Files.Maps;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;

namespace GameServer.PacketManager
{
    public class PM_Information : PM_Base
    {
        [HandlesPacket(PacketHeader.InformationManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            InformationData data = Serializer.ConvertBytesToObject<InformationData>(bytes);

            switch (data._stepMode)
            {
                case InformationData.InfoStepMode.Connection:
                    SendInformation(client, data);
                    break;

                case InformationData.InfoStepMode.Wealth:
                    SendWealth(client, data);
                    break;
            }
        }

        private static void SendInformation(ServerClient client, InformationData data)
        {
            SettlementFile settlementToFind = PM_Settlements.GetSettlementFileFromTile(data._settlementTile);
            ServerClient clientToFind = ServerNetwork.GetConnectedClientFromUsername(settlementToFind.Username);

            data._isPlayerOnline = clientToFind != null ? true : false;

            client.Listener.EnqueuePacket(PacketHeader.InformationManager, data);
        }

        private static void SendWealth(ServerClient client, InformationData data)
        {
            data._settlementRawData = PM_Maps.GetMapFromTile(data._settlementTile);

            client.Listener.EnqueuePacket(PacketHeader.InformationManager, data);
        }
    }
}
