using GameServer.Core;
using GameServer.Misc;
using GameServer.TCP;
using Shared;
using static Shared.CommonEnumerators;

namespace GameServer.Managers
{
    [RTManager]
    public static class CaravanManager
    {
        //Variables

        public static readonly string fileExtension = ".mpcaravan";

        public static void ParsePacket(ServerClient client, Packet packet)
        {
            CaravanData data = Serializer.ConvertBytesToObject<CaravanData>(packet.contents);

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

        private static void AddCaravan(ServerClient client, CaravanFile file)
        {
            CaravanData data = new CaravanData();
            data._stepMode = CaravanStepMode.Add;
            data._caravanFile = file;

            Packet packet = Packet.CreatePacketFromObject(nameof(CaravanManager), data);
            NetworkHelper.SendPacketToAllClients(packet);

            InformationDisplayer.DisplayAddCaravan(client.userFile.Uid);
        }

        public static void RemoveCaravan(ServerClient client, CaravanFile file)
        {
            CaravanData data = new CaravanData();
            data._stepMode = CaravanStepMode.Remove;
            data._caravanFile = file;

            Packet packet = Packet.CreatePacketFromObject(nameof(CaravanManager), data);
            NetworkHelper.SendPacketToAllClients(packet);

            InformationDisplayer.DisplayRemoveCaravan(client.userFile.Uid);
        }

        private static void MoveCaravan(ServerClient client, CaravanFile file)
        {
            CaravanData data = new CaravanData();
            data._stepMode = CaravanStepMode.Move;
            data._caravanFile = file;

            Packet packet = Packet.CreatePacketFromObject(nameof(CaravanManager), data);
            NetworkHelper.SendPacketToAllClients(packet, client);

            InformationDisplayer.DisplayMoveCaravan(client.userFile.Uid);
        }
    }
}
