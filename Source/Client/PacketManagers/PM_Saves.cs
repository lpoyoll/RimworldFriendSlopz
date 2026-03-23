using GameClient.Core;
using GameClient.Dialogs;
using GameClient.Hooks.TCPNetwork;
using GameClient.Misc;
using HarmonyLib;
using RimWorld;
using Shared;
using Shared.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using TCPNetwork;
using TCPNetwork.Files.Client;
using TCPNetwork.Packets;
using Verse;
using static GameClient.Managers.DisconnectionManager;
using static Shared.CommonEnumerators;
using static Shared.Misc.Printer;
using static TCPNetwork.Packets.PKT_Save;

namespace GameClient.PacketManagers
{
    public class PM_Saves : PM_Base
    {
        public static string LatestSavePath { get; set; } = string.Empty;

        public static string CustomSaveName => $"MP - {Network.Ip} - {Network.Port} - {SessionHandler.Username}";

        public static string SaveFilePath => Path.Combine(Master.SavesFolderPath, CustomSaveName + ".rws");

        public static string TempSaveFilePath => SaveFilePath + ".rws.temp";

        [HandlesPacket(PacketHeader.SaveManager)]
        public override void Receive(ServerClient client, byte[] bytes, PacketHeader header)
        {
            PKT_Save data = Serializer.ConvertBytesToObject<PKT_Save>(bytes);

            switch (data._stepMode)
            {
                case SaveStepMode.Receive:
                    OnSaveReceived(data);
                    break;
            }
        }

        public static void ForceSave()
        {
            Printer.Warning("Force saving", LogImportanceMode.Verbose);
            DLG_Base.PushNewDialog(new DLG_Wait("Saving your game"));

            Task.Run(delegate
            {
                Thread.Sleep(100);

                MainThreadHandler.Instance.Enqueue(delegate
                {
                    ResetAutosaveTicks();
                    GameDataSaveLoader.SaveGame(CustomSaveName);
                });
            });
        }

        private static void ResetAutosaveTicks()
        {
            FieldInfo FticksSinceSave = AccessTools.Field(typeof(Autosaver), "ticksSinceSave");
            FticksSinceSave.SetValue(Current.Game.autosaver, 0);
        }

        public static void RequestResetSave()
        {
            PKT_Save data = new PKT_Save();
            data._stepMode = SaveStepMode.Reset;

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.SaveManager, data);
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
            foreach (string str in Directory.GetFiles(Master.SavesFolderPath))
            {
                if (Path.GetExtension(str) == ".rws") result.Add(Path.GetFileNameWithoutExtension(str), str);
            }

            return result;
        }

        public static void SendSaveToServer()
        {
            Printer.Message("Sending save to server", LogImportanceMode.Verbose);

            byte[] saveBytes;
            if (string.IsNullOrEmpty(LatestSavePath)) saveBytes = File.ReadAllBytes(SaveFilePath);
            else saveBytes = File.ReadAllBytes(LatestSavePath);

            PKT_Save data = new PKT_Save();
            data._stepMode = SaveStepMode.Receive;
            data._forceDisconnect = SessionHandler.IsExiting;
            data._fileBytes = GZip.CompressBytes(saveBytes);

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.SaveManager, data);
        }

        private static void OnSaveReceived(PKT_Save data)
        {
            Printer.Message($"Receiving save from server", LogImportanceMode.Verbose);

            byte[] saveBytes = GZip.DecompressBytes(data._fileBytes);
            File.WriteAllBytes(TempSaveFilePath, saveBytes);
            File.Delete(CommonValues.DefaultSaveFormat);

            if (data._forceUseSave || !File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                File.Move(TempSaveFilePath, SaveFilePath);
            }

            else
            {
                if (GetRealPlayTimeInteractingFromSave(TempSaveFilePath) >= GetRealPlayTimeInteractingFromSave(SaveFilePath))
                {
                    Printer.Message("Loading remote save", LogImportanceMode.Verbose);

                    File.Delete(PM_Saves.SaveFilePath);
                    File.Move(PM_Saves.TempSaveFilePath, PM_Saves.SaveFilePath);
                }

                else
                {
                    Printer.Message("Loading local save", LogImportanceMode.Verbose);

                    File.Delete(PM_Saves.TempSaveFilePath);
                }
            }

            GameDataSaveLoader.LoadGame(PM_Saves.CustomSaveName);
        }
    }
}
