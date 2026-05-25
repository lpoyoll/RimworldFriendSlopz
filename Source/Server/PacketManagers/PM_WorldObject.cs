using GameServer.Core;
using GameServer.Hooks.TCPNetwork;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCPNetwork;
using TCPNetwork.PacketManagers;
using TCPNetwork.Packets;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GameServer.PacketManagers
{
    public class PM_WorldObject : PM_Base
    {
        [HandlesPacket(PacketHeader.WorldObject)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_WorldObject packet = Serializer.ConvertBytesToObject<PKT_WorldObject>(bytes);

            switch (packet.CurrentStepMode)
            {
                case PKT_WorldObject.StepMode.Add:
                    AddWorldObject(client, packet);
                    break;

                case PKT_WorldObject.StepMode.Remove:
                    RemoveWorldObject(client, packet);
                    break;
            }
        }

        private static void AddWorldObject(ServerClient client, PKT_WorldObject packet)
        {
            string path = Path.Combine(Master.WorldObjectsPath, packet.WorldObject.Tile + ".json");
            Serializer.SerializeToFile(path, packet.WorldObject);

            ServerNetwork.SendPacketToAllClients(PacketHeader.WorldObject, packet);
        }

        private static void RemoveWorldObject(ServerClient client, PKT_WorldObject packet)
        {
            string path = Path.Combine(Master.WorldObjectsPath, packet.WorldObject.Tile + ".json");
            File.Delete(path);

            ServerNetwork.SendPacketToAllClients(PacketHeader.WorldObject, packet);
        }
    }
}
