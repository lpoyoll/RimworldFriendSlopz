using GameServer.Core;
using GameServer.Hooks.TCPNetwork;
using GameServer.Managers;
using RTShared;
using RTShared.Files;
using RTShared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RTNetwork;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
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
                    AddWorldObject(client, packet, packet.WorldObject);
                    break;

                case PKT_WorldObject.StepMode.Remove:
                    RemoveWorldObject(client, packet);
                    break;

                case PKT_WorldObject.StepMode.Bulk:
                    AddAllWorldObjects(client, packet);
                    break;
            }
        }

        private static void AddWorldObject(ServerClient client, PKT_WorldObject packet, FL_WorldObject file)
        {
            string path = Path.Combine(Master.WorldObjectsPath, file.Tile + ".json");
            if (File.Exists(path)) ResponseShortcutManager.SendIllegalPacket(client);
            else
            {
                Serializer.SerializeToFile(path, file);
                if (packet != null) ServerNetwork.SendPacketToAllClients(PacketHeader.WorldObject, packet);
                Printer.Warning($"WorldObject at '{file.Tile}' has been added", Printer.Verbosity.Verbose);
            }
        }

        private static void RemoveWorldObject(ServerClient client, PKT_WorldObject packet)
        {
            string path = Path.Combine(Master.WorldObjectsPath, packet.WorldObject.Tile + ".json");
            if (!File.Exists(path)) ResponseShortcutManager.SendIllegalPacket(client);
            else
            {
                File.Delete(path);
                ServerNetwork.SendPacketToAllClients(PacketHeader.WorldObject, packet);
                Printer.Warning($"WorldObject at '{packet.WorldObject.Tile}' has been removed", Printer.Verbosity.Verbose);
            }
        }

        private static void AddAllWorldObjects(ServerClient client, PKT_WorldObject packet)
        {
            foreach (FL_WorldObject file in packet.Bulk) { AddWorldObject(null, null, file); }
        }

        public static List<FL_WorldObject> GetAllWorldObjects()
        {
            List<FL_WorldObject> worldObjects = new List<FL_WorldObject>();
            foreach (string str in Directory.GetFiles(Master.WorldObjectsPath))
            {
                worldObjects.Add(Serializer.SerializeFromFile<FL_WorldObject>(str));
            }

            return worldObjects;
        }
    }
}
