using GameServer.Core;
using GameServer.Misc;
using GameServer.TCP;
using Shared;
using static Shared.CommonEnumerators;

namespace GameServer.Managers
{
    [RTManager]
    public static class WorldManager
    {
        public static string baseWorldPath = Path.Combine(Master.configsPath, "WorldConfig.json");

        private static void ParsePacket(ServerClient client, Packet packet)
        {
            WorldData data = Serializer.ConvertBytesToObject<WorldData>(packet.Contents);

            switch (data._stepMode)
            {
                case WorldStepMode.Sent:
                    WorldManagerReceiver.ReceiveWorld(client, data);
                    break;
            }
        }

        public static bool CheckIfWorldExists() { return File.Exists(baseWorldPath); }

        public static void RequireWorldFile(ServerClient client)
        {
            WorldData worldData = new WorldData();
            worldData._stepMode = WorldStepMode.AskFor;

            Packet packet = Packet.CreateFromObject(nameof(WorldManager), worldData);
            client.listener.EnqueuePacket(packet);
        }
    }

    public static class WorldManagerSender
    {
        public static void SendWorld(ServerClient client)
        {
            WorldData data = new WorldData();
            data._fileBytes = GZip.DecompressBytes(File.ReadAllBytes(WorldManager.baseWorldPath));
            data._stepMode = WorldStepMode.Sent;

            Packet packet = Packet.CreateFromObject(nameof(WorldManager), data);
            client.listener.EnqueuePacket(packet);
        }
    }

    public static class WorldManagerReceiver
    {
        public static void ReceiveWorld(ServerClient client, WorldData data)
        {
            File.WriteAllBytes(WorldManager.baseWorldPath, GZip.CompressBytes(data._fileBytes));
            Main_.LoadValueFile(ServerFileMode.World);
        }
    }
}
