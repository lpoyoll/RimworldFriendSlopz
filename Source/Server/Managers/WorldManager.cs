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

        public static string tempWorldPath = Path.Combine(Master.tempPath, "WorldConfig.temp");

        public static void ParsePacket(ServerClient client, Packet packet)
        {
            WorldData data = Serializer.ConvertBytesToObject<WorldData>(packet.contents);

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

            Packet packet = Packet.CreatePacketFromObject(nameof(WorldManager), worldData);
            client.listener.EnqueuePacket(packet);
        }
    }

    public static class WorldManagerSender
    {
        public static void SetupWorldSender(ServerClient client)
        {
            if (client.listener.uploadManager != null) return;
            else
            {
                client.listener.uploadManager = new UploadManager(WorldManager.baseWorldPath);
                client.listener.uploadManager.PrepareUpload();
            }
        }

        public static void SendWorld(ServerClient client)
        {
            SetupWorldSender(client);

            WorldData data = new WorldData();
            data._fileBytes = client.listener.uploadManager.ReadFile();
            data._stepMode = WorldStepMode.Sent;

            Packet packet = Packet.CreatePacketFromObject(nameof(WorldManager), data);
            client.listener.EnqueuePacket(packet);

            OnWorldSent(client);
        }

        private static void OnWorldSent(ServerClient client) 
        {
            client.listener.uploadManager.FinishFileWrite();
            client.listener.uploadManager = null;
        }
    }

    public static class WorldManagerReceiver
    {
        public static void SetupWorldReceiver(ServerClient client)
        {
            if (client.listener.downloadManager != null) return;
            else
            {
                client.listener.downloadManager = new DownloadManager(WorldManager.tempWorldPath);
                client.listener.downloadManager.PrepareDownload();
            }
        }

        public static void ReceiveWorld(ServerClient client, WorldData data)
        {
            SetupWorldReceiver(client);

            client.listener.downloadManager.WriteFile(data._fileBytes);

            OnWorldReceived(client, WorldManager.baseWorldPath, WorldManager.tempWorldPath);
        }

        private static void OnWorldReceived(ServerClient client, string baseSavePath, string tempSavePath)
        {
            client.listener.downloadManager.FinishFileWrite();
            client.listener.downloadManager = null;

            byte[] completedSave = File.ReadAllBytes(tempSavePath);
            File.WriteAllBytes(baseSavePath, completedSave);
            File.Delete(tempSavePath);

            Main_.LoadValueFile(ServerFileMode.World);
        }
    }
}
