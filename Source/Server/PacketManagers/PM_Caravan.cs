using GameServer.Hooks.TCPNetwork;
using RTShared;
using RTShared.Files;
using RTNetwork;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using static RTNetwork.Packets.PKT_Caravan;

namespace GameServer.PacketManager
{
    public class PM_Caravan : PM_Base
    {
        [HandlesPacket(PacketHeader.Caravan)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Caravan data = Serializer.ConvertBytesToObject<PKT_Caravan>(bytes);

            switch (data._stepMode)
            {
                case CaravanStepMode.Add:
                    AddCaravan(client, data._caravanFile);
                    break;

                case CaravanStepMode.Remove:
                    RemoveCaravan(client, data._caravanFile);
                    break;

                case CaravanStepMode.Move:
                    MoveCaravan(client, data._caravanFile);
                    break;
            }
        }

        private static void AddCaravan(ServerClient client, FL_Caravan file)
        {
            PKT_Caravan data = new PKT_Caravan();
            data._stepMode = CaravanStepMode.Add;
            data._caravanFile = file;

            ServerNetwork.SendPacketToAllClients(PacketHeader.Caravan, data, client);
        }

        public static void RemoveCaravan(ServerClient client, FL_Caravan file)
        {
            PKT_Caravan data = new PKT_Caravan();
            data._stepMode = CaravanStepMode.Remove;
            data._caravanFile = file;

            ServerNetwork.SendPacketToAllClients(PacketHeader.Caravan, data, client);
        }

        private static void MoveCaravan(ServerClient client, FL_Caravan file)
        {
            PKT_Caravan data = new PKT_Caravan();
            data._stepMode = CaravanStepMode.Move;
            data._caravanFile = file;

            ServerNetwork.SendPacketToAllClients(PacketHeader.Caravan, data, client);
        }
    }
}
