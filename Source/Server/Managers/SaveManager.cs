using GameServer.Core;
using GameServer.Misc;
using Shared;
using static Shared.CommonEnumerators;
using Shared.Files;
using TCPNetwork.Packets;
using TCPNetwork.Files.Client;

namespace GameServer.Managers
{
    public static class SaveManager
    {
        [HandlesPacket(PacketHeader.SaveManager)]
        private static void ParsePacket(ServerClient client, byte[] bytes)
        {
            SaveData data = Serializer.ConvertBytesToObject<SaveData>(bytes);

            Printer.Warning(data, LogImportanceMode.Extreme);

            if (data._stepMode == SaveStepMode.Receive) SaveReceiverManager.ReceiveSaveFromClient(client, data);
            else if (data._stepMode == SaveStepMode.Send) SaveSenderManager.SendSaveToClient(client);
            else if (data._stepMode == SaveStepMode.Reset) ResetClientSave(client);
            else ResponseShortcutManager.SendIllegalPacket(client, "Received invalid step mode");
        }

        public static void OnUserSave(ServerClient client, SaveData fileTransferData)
        {
            if (fileTransferData._instructions == (int)SaveMode.Disconnect) client.Listener.DisconnectFlag = true;

            InformationDisplayer.DisplaySaveGame(client);
        }

        public static bool CheckIfUserHasSave(ServerClient client)
        {
            string[] saves = Directory.GetFiles(Master.SavesPath);
            foreach (string save in saves)
            {
                if (Path.GetFileNameWithoutExtension(save) == client.UserFile.Username) return true;
            }

            return false;
        }

        public static void ResetClientSave(ServerClient client)
        {
            if (!CheckIfUserHasSave(client))
            {
                ResponseShortcutManager.SendIllegalPacket(client, $"Player {client.UserFile.Username}'s save was attempted to be reset while the player doesn't have a save");
                return;
            }
            client.Listener.DisconnectFlag = true;

            ResetPlayerData(client, client.UserFile.Username);
        }

        public static void ResetPlayerData(ServerClient client, string username)
        {
            BackupManager.BackupUser(username);

            if (client != null) client.Listener.DisconnectFlag = true;

            // Delete save file
            try { File.Delete(Path.Combine(Master.SavesPath, username + CommonValues.DefaultSaveFormat)); }
            catch { Printer.Warning($"Failed to find {client.UserFile.Username}'s save"); }

            // Delete site files
            SiteFile[] playerSites = SiteManagerHelper.GetAllSitesFromUsername(username);
            foreach (SiteFile site in playerSites) SiteManager.DestroySiteFromFile(site);

            // Delete settlement files
            SettlementFile[] playerSettlements = SettlementManager.GetAllSettlementsFromUsername(username);
            foreach (SettlementFile settlement in playerSettlements)
            {
                PlayerSettlementData settlementData = new PlayerSettlementData();
                settlementData._settlementFile.Tile = settlement.Tile;
                settlementData._settlementFile.Username = settlement.Username;

                SettlementManager.RemoveSettlement(client, settlementData);
            }

            InformationDisplayer.DisplayResetPlayer(username);
        }
    }

    public static class SaveSenderManager
    {
        public static void SendSaveToClient(ServerClient client)
        {
            string baseClientSavePath = Path.Combine(Master.SavesPath, client.UserFile.Username + CommonValues.DefaultSaveFormat);

            InformationDisplayer.DisplayLoadGame(client);

            SaveData data = new SaveData();
            data._fileBytes = File.ReadAllBytes(baseClientSavePath);
            data._stepMode = SaveStepMode.Receive;
            if (!Master.ServerConfig.SyncLocalSave) data._instructions = SaveMode.Strict;

            client.Listener.EnqueuePacket(PacketHeader.SaveManager, data);
        }
    }

    public static class SaveReceiverManager
    {
        public static void ReceiveSaveFromClient(ServerClient client, SaveData data)
        {
            string baseClientSavePath = Path.Combine(Master.SavesPath, client.UserFile.Username + CommonValues.DefaultSaveFormat);
            string tempClientSavePath = Path.Combine(Master.TempPath, client.UserFile.Username + CommonValues.TempSaveFormat);

            File.WriteAllBytes(tempClientSavePath, data._fileBytes);

            OnSaveReceived(client, data, baseClientSavePath, tempClientSavePath);
        }

        private static void OnSaveReceived(ServerClient client, SaveData data, string baseClientSavePath, string tempClientSavePath)
        {
            byte[] completedSave = File.ReadAllBytes(tempClientSavePath);
            File.WriteAllBytes(baseClientSavePath, completedSave);
            File.Delete(tempClientSavePath);

            SaveManager.OnUserSave(client, data);
        }
    }
}
