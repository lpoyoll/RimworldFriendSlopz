using GameServer.Core;
using GameServer.Misc;
using GameServer.TCP;
using Shared;
using static Shared.CommonEnumerators;

namespace GameServer.Managers
{

    public static class WorldManager
    {
        [HandlesPacket(PacketHeader.WorldManager)]
        private static void ParsePacket(ServerClient client, byte[] bytes)
        {
            WorldData data = Serializer.ConvertBytesToObject<WorldData>(bytes);

            Printer.Warning(data, LogImportanceMode.Extreme);

            switch (data._stepMode)
            {
                case WorldStepMode.Sent:
                    ReceiveWorld(client, data);
                    break;
            }
        }

        public static bool CheckIfWorldExists() { return File.Exists(WorldValuesFile.FilePath); }

        public static void RequireWorldFile(ServerClient client)
        {
            WorldData worldData = new WorldData();
            worldData._stepMode = WorldStepMode.AskFor;

            client.Listener.EnqueuePacket(PacketHeader.WorldManager, worldData);
        }

        public static void SendWorld(ServerClient client)
        {
            WorldData data = new WorldData();
            data._fileBytes = GZip.DecompressBytes(File.ReadAllBytes(WorldValuesFile.FilePath));
            data._stepMode = WorldStepMode.Sent;

            client.Listener.EnqueuePacket(PacketHeader.WorldManager, data);
        }

        public static void ReceiveWorld(ServerClient client, WorldData data)
        {
            File.WriteAllBytes(WorldValuesFile.FilePath, GZip.CompressBytes(data._fileBytes));
            Master.WorldValues = WorldValuesFile.Load();
        }
    }
}
