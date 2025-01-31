using HarmonyLib;
using RimWorld;
using Shared;
using System.IO;
using System.Reflection;
using Verse;
using static Shared.CommonEnumerators;
using static GameClient.Managers.DisconnectionManager;
using System.Xml;
using System.Xml.XPath;
using System;
using GameClient.Core;
using GameClient.Misc;
using GameClient.Values;
using GameClient.TCP;

namespace GameClient.Managers
{
    [RTManager]
    public static class SaveManager
    {
        // Variables

        public static string customSaveName => $"Server - {Network.ip} - {Network.port} - {ClientValues.username}";

        public static string saveFilePath => Path.Combine(Master.savesFolderPath, customSaveName + ".rws");

        public static string tempSaveFilePath => saveFilePath + ".mpsave";

        public static string serverSaveFilePath => saveFilePath + ".rws.temp";

        public static void ParsePacket(Packet packet)
        {
            SaveData data = Serializer.ConvertBytesToObject<SaveData>(packet.contents);

            if (data._stepMode == SaveStepMode.Receive) SaveReceiverManager.ReceiveSaveFromServer(data);
            else if (data._stepMode == SaveStepMode.Send) SaveSenderManager.SendSaveToServer();
            else throw new NotImplementedException();
        }

        public static void ForceSave()
        {
            FieldInfo FticksSinceSave = AccessTools.Field(typeof(Autosaver), "ticksSinceSave");
            FticksSinceSave.SetValue(Current.Game.autosaver, 0);

            ClientValues.autosaveCurrentTicks = 0;

            GameDataSaveLoader.SaveGame(customSaveName);
        }

        public static void RequestResetSave()
        {
            SaveData data = new SaveData();
            data._stepMode = SaveStepMode.Reset;

            Packet packet = Packet.CreatePacketFromObject(nameof(SaveManager), data);
            Network.listener.EnqueuePacket(packet);
        }

        public static double GetRealPlayTimeInteractingFromSave(string filePath)
        {
            if (!File.Exists(filePath)) return 0;

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filePath);
                XPathNavigator nav = doc.CreateNavigator();

                return double.Parse(nav.SelectSingleNode("/savegame/game/info/realPlayTimeInteracting").Value);
            }
            catch { return 0; }
        }
    }

    public static class SaveSenderManager
    {
        public static void SendSaveToServer()
        {
            if (Network.listener.uploadManager == null)
            {
                byte[] saveBytes = File.ReadAllBytes(SaveManager.saveFilePath);
                saveBytes = GZip.CompressBytes(saveBytes);

                File.WriteAllBytes(SaveManager.tempSaveFilePath, saveBytes);
                Network.listener.uploadManager = new UploadManager(SaveManager.tempSaveFilePath);
                Network.listener.uploadManager.PrepareUpload();
            }

            SaveData data = new SaveData();
            data._fileBytes = Network.listener.uploadManager.ReadFile();
            data._stepMode = SaveStepMode.Receive;

            // Set the instructions of the packet
            if (isIntentionalDisconnect && (intentionalDisconnectReason == DCReason.SaveQuitToMenu || intentionalDisconnectReason == DCReason.SaveQuitToOS))
            {
                data._instructions = (int)SaveMode.Disconnect;
            }
            else data._instructions = (int)SaveMode.Autosave;

            Packet packet = Packet.CreatePacketFromObject(nameof(SaveManager), data);
            Network.listener.EnqueuePacket(packet);

            OnSaveSent();
        }

        private static void OnSaveSent()
        {
            Network.listener.uploadManager.FinishFileWrite();
            Network.listener.uploadManager = null;
            File.Delete(SaveManager.tempSaveFilePath);
        }
    }

    public static class SaveReceiverManager
    {
        public static void ReceiveSaveFromServer(SaveData data)
        {
            //If this is the first packet
            if (Network.listener.downloadManager == null)
            {
                Printer.Message($"Receiving save from server");

                Network.listener.downloadManager = new DownloadManager(SaveManager.tempSaveFilePath);
                Network.listener.downloadManager.PrepareDownload();
            }

            Network.listener.downloadManager.WriteFile(data._fileBytes);

            OnSaveReceived(data);
        }

        private static void OnSaveReceived(SaveData data)
        {
            Network.listener.downloadManager.FinishFileWrite();
            Network.listener.downloadManager = null;

            byte[] fileBytes = File.ReadAllBytes(SaveManager.tempSaveFilePath);
            fileBytes = GZip.DecompressBytes(fileBytes);

            File.WriteAllBytes(SaveManager.serverSaveFilePath, fileBytes);
            File.Delete(SaveManager.tempSaveFilePath);

            if (data._instructions != (int)SaveMode.Strict && File.Exists(SaveManager.saveFilePath))
            {
                if (SaveManager.GetRealPlayTimeInteractingFromSave(SaveManager.serverSaveFilePath) >= SaveManager.GetRealPlayTimeInteractingFromSave(SaveManager.saveFilePath))
                {
                    Printer.Message("Loading remote save");
                    File.Delete(SaveManager.saveFilePath);
                    File.Move(SaveManager.serverSaveFilePath, SaveManager.saveFilePath);
                }

                else
                {
                    Printer.Message("Loading local save");
                    File.Delete(SaveManager.serverSaveFilePath);
                }
            }

            else
            {
                File.Delete(SaveManager.saveFilePath);
                File.Move(SaveManager.serverSaveFilePath, SaveManager.saveFilePath);
            }

            GameDataSaveLoader.LoadGame(SaveManager.customSaveName);
        }
    }
}
