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
using System.Collections.Generic;
using GameClient.Dialogs;
using System.Linq;
using TCPNetwork.Packets;
using System.Threading.Tasks;
using System.Threading;

namespace GameClient.Managers
{
    public static class SaveManager
    {
        public static string LatestSavePath { get; set; } = string.Empty;

        public static string CustomSaveName => $"MP - {ClientNetwork.Ip} - {ClientNetwork.Port} - {ClientValues.Username}";

        public static string SaveFilePath => Path.Combine(Master.SavesFolderPath, CustomSaveName + ".rws");

        public static string TempSaveFilePath => SaveFilePath + ".rws.temp";

        [HandlesPacket(PacketHeader.SaveManager)]
        private static void ParsePacket(byte[] bytes)
        {
            SaveData data = Serializer.ConvertBytesToObject<SaveData>(bytes);

            Printer.Warning(data, LogImportanceMode.Extreme);

            if (data._stepMode == SaveStepMode.Receive) SaveManager.ReceiveSaveFromServer(data);
            else if (data._stepMode == SaveStepMode.Send)
            {
                LatestSavePath = SaveFilePath;
                SaveManager.SendSaveToServer();
            }
        }

        public static void ForceSave()
        {
            Printer.Warning("Force saving", LogImportanceMode.Verbose);

            RT_Dialog_Base.PushNewDialog(new RT_Dialog_Wait("Saving your game"));

            Task.Run(delegate
            {
                Thread.Sleep(1);

                MainThreadHandler.Instance.Enqueue(delegate
                {
                    FieldInfo FticksSinceSave = AccessTools.Field(typeof(Autosaver), "ticksSinceSave");
                    FticksSinceSave.SetValue(Current.Game.autosaver, 0);
                    GameDataSaveLoader.SaveGame(CustomSaveName);
                });
            });
        }

        public static void RequestResetSave()
        {
            SaveData data = new SaveData();
            data._stepMode = SaveStepMode.Reset;

            ClientNetwork.Instance.ClientListener.EnqueuePacket(PacketHeader.SaveManager, data);
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

        public static Dictionary<string, string> GetAllSaveFiles() 
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (string file in Directory.GetFiles(Master.SavesFolderPath))
            {
                if(Path.GetExtension(file) == ".rws")
                    result.Add(Path.GetFileNameWithoutExtension(file), file);
            }
            return result;
        }

        public static void OpenSaveUploaderMenu()
        {
            Dictionary<string, string> saves = SaveManager.GetAllSaveFiles();
            RT_Dialog_ListingWithButton dialog = new RT_Dialog_ListingWithButton("Save uploader",
                "Select a save to upload:",
                saves.Keys.ToArray(),
                delegate
                {
                    RT_Dialog_YesNo D2 = new RT_Dialog_YesNo("This feature is in beta and might fail, are you sure?", delegate
                    {
                        if (saves.TryGetValue(RT_Dialog_ListingWithButton.DialogButtonListingResultString, out string file))
                        {
                            byte[] data = File.ReadAllBytes(file);
                            File.WriteAllBytes(SaveManager.SaveFilePath, data);
                            RT_Dialog_Base.PushNewDialog(new RT_Dialog_Wait("Waiting for save upload"));

                            DisconnectionManager.SetIntentionalDisconnect(true, DisconnectionManager.DCReason.SaveQuitToMenu);

                            SaveManager.LatestSavePath = SaveManager.SaveFilePath;

                            SaveManager.SendSaveToServer();
                        }
                    });

                    RT_Dialog_Base.PushNewDialog(D2);
                });

            RT_Dialog_Base.PushNewDialog(dialog);
        }

        public static void SendSaveToServer()
        {
            byte[] saveBytes;
            if (string.IsNullOrEmpty(SaveManager.LatestSavePath)) saveBytes = File.ReadAllBytes(SaveManager.SaveFilePath);
            else saveBytes = File.ReadAllBytes(SaveManager.LatestSavePath);
            saveBytes = GZip.CompressBytes(saveBytes);

            SaveData data = new SaveData();
            data._stepMode = SaveStepMode.Receive;
            data._fileBytes = saveBytes;

            // Set the instructions of the packet
            if (IsIntentionalDisconnect && (IntentionalDisconnectReason == DCReason.SaveQuitToMenu || IntentionalDisconnectReason == DCReason.SaveQuitToOS))
            {
                data._instructions = (int)SaveMode.Disconnect;
            }
            else data._instructions = SaveMode.Autosave;

            ClientNetwork.Instance.ClientListener.EnqueuePacket(PacketHeader.SaveManager, data);
        }

        public static void ReceiveSaveFromServer(SaveData data)
        {
            Printer.Message($"Receiving save from server", LogImportanceMode.Verbose);

            File.WriteAllBytes(CommonValues.DefaultSaveFormat, data._fileBytes);

            OnSaveReceived(data);
        }

        private static void OnSaveReceived(SaveData data)
        {
            byte[] fileBytes = File.ReadAllBytes(CommonValues.DefaultSaveFormat);
            fileBytes = GZip.DecompressBytes(fileBytes);

            File.WriteAllBytes(SaveManager.TempSaveFilePath, fileBytes);
            File.Delete(CommonValues.DefaultSaveFormat);

            if (data._instructions != SaveMode.Strict && File.Exists(SaveManager.SaveFilePath))
            {
                if (SaveManager.GetRealPlayTimeInteractingFromSave(SaveManager.TempSaveFilePath) >= SaveManager.GetRealPlayTimeInteractingFromSave(SaveManager.SaveFilePath))
                {
                    Printer.Message("Loading remote save", LogImportanceMode.Verbose);
                    File.Delete(SaveManager.SaveFilePath);
                    File.Move(SaveManager.TempSaveFilePath, SaveManager.SaveFilePath);
                }

                else
                {
                    Printer.Message("Loading local save", LogImportanceMode.Verbose);
                    File.Delete(SaveManager.TempSaveFilePath);
                }
            }

            else
            {
                File.Delete(SaveManager.SaveFilePath);
                File.Move(SaveManager.TempSaveFilePath, SaveManager.SaveFilePath);
            }

            GameDataSaveLoader.LoadGame(SaveManager.CustomSaveName);
        }
    }
}
