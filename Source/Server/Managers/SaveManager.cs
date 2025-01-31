using GameServer.Core;
using GameServer.Misc;
using GameServer.TCP;
using Shared;
using static Shared.CommonEnumerators;

namespace GameServer.Managers
{
    [RTManager]
    public static class SaveManager
    {
        // Variables

        public readonly static string fileExtension = ".mpsave";

        public readonly static string tempFileExtension = ".mpsavetemp";

        public static void ParsePacket(ServerClient client, Packet packet)
        {
            SaveData data = Serializer.ConvertBytesToObject<SaveData>(packet.contents);

            if (data._stepMode == SaveStepMode.Receive) SaveReceiverManager.ReceiveSaveFromClient(client, data);
            else if (data._stepMode == SaveStepMode.Send) SaveSenderManager.SendSaveToClient(client);
            else if (data._stepMode == SaveStepMode.Reset) ResetClientSave(client);
            else ResponseShortcutManager.SendIllegalPacket(client, "Received invalid step mode");
        }

        public static void OnUserSave(ServerClient client, SaveData fileTransferData)
        {
            if (fileTransferData._instructions == (int)SaveMode.Disconnect) client.listener.disconnectFlag = true;

            InformationDisplayer.DisplaySaveGame(client);
        }

        public static bool CheckIfUserHasSave(ServerClient client)
        {
            string[] saves = Directory.GetFiles(Master.savesPath);
            foreach (string save in saves)
            {
                if (!save.EndsWith(fileExtension)) continue;
                if (Path.GetFileNameWithoutExtension(save) == client.userFile.Uid) return true;
            }

            return false;
        }

        public static byte[] GetUserSaveFromUsername(string username)
        {
            string[] saves = Directory.GetFiles(Master.savesPath);
            foreach (string save in saves)
            {
                if (!save.EndsWith(fileExtension)) continue;
                if (Path.GetFileNameWithoutExtension(save) == username) return File.ReadAllBytes(save);
            }

            return null;
        }

        public static void ResetClientSave(ServerClient client)
        {
            if (!CheckIfUserHasSave(client))
            {
                ResponseShortcutManager.SendIllegalPacket(client, $"Player {client.userFile.Uid}'s save was attempted to be reset while the player doesn't have a save");
                return;
            }
            client.listener.disconnectFlag = true;

            ResetPlayerData(client, client.userFile.Uid);
        }

        public static void ResetPlayerData(ServerClient client, string uid)
        {
            BackupManager.BackupUser(uid);

            if (client != null) client.listener.disconnectFlag = true;

            // Delete save file
            try { File.Delete(Path.Combine(Master.savesPath, uid + fileExtension)); }
            catch { Printer.Warning($"Failed to find {client.userFile.Label}'s save"); }

            // Delete site files
            SiteFile[] playerSites = SiteManagerHelper.GetAllSitesFromUID(uid);
            foreach (SiteFile site in playerSites) SiteManager.DestroySiteFromFile(site);

            // Delete settlement files
            SettlementFile[] playerSettlements = PlayerSettlementManager.GetAllSettlementsFromUsername(uid);
            foreach (SettlementFile settlement in playerSettlements)
            {
                PlayerSettlementData settlementData = new PlayerSettlementData();
                settlementData._settlementFile.Tile = settlement.Tile;
                settlementData._settlementFile.UID = settlement.UID;

                PlayerSettlementManager.RemoveSettlement(client, settlementData);
            }

            InformationDisplayer.DisplayResetPlayer(uid);
        }
    }

    public static class SaveSenderManager
    {
        public static void SendSaveToClient(ServerClient client)
        {
            string baseClientSavePath = Path.Combine(Master.savesPath, client.userFile.Uid + SaveManager.fileExtension);

            //if this is the first packet
            if (client.listener.uploadManager == null)
            {
                InformationDisplayer.DisplayLoadGame(client);

                client.listener.uploadManager = new UploadManager(baseClientSavePath);
                client.listener.uploadManager.PrepareUpload();
            }

            SaveData data = new SaveData();
            data._fileBytes = client.listener.uploadManager.ReadFile();
            data._stepMode = SaveStepMode.Receive;
            if (!Master.serverConfig.SyncLocalSave) data._instructions = (int)SaveMode.Strict;

            Packet packet = Packet.CreatePacketFromObject(nameof(SaveManager), data);
            client.listener.EnqueuePacket(packet);

            OnSaveSent(client);
        }

        private static void OnSaveSent(ServerClient client)
        {
            client.listener.uploadManager.FinishFileWrite();
            client.listener.uploadManager = null;
        }
    }

    public static class SaveReceiverManager
    {
        public static void ReceiveSaveFromClient(ServerClient client, SaveData data)
        {
            string baseClientSavePath = Path.Combine(Master.savesPath, client.userFile.Uid + SaveManager.fileExtension);
            string tempClientSavePath = Path.Combine(Master.tempPath, client.userFile.Uid + SaveManager.tempFileExtension);

            if (client.listener.downloadManager == null)
            {
                client.listener.downloadManager = new DownloadManager(tempClientSavePath);
                client.listener.downloadManager.PrepareDownload();
            }

            client.listener.downloadManager.WriteFile(data._fileBytes);

            OnSaveReceived(client, data, baseClientSavePath, tempClientSavePath);
        }

        private static void OnSaveReceived(ServerClient client, SaveData data, string baseClientSavePath, string tempClientSavePath)
        {
            client.listener.downloadManager.FinishFileWrite();
            client.listener.downloadManager = null;

            byte[] completedSave = File.ReadAllBytes(tempClientSavePath);
            File.WriteAllBytes(baseClientSavePath, completedSave);
            File.Delete(tempClientSavePath);

            SaveManager.OnUserSave(client, data);
        }
    }
}
