using GameClient.Core;
using GameClient.Dialogs;
using GameClient.Dialogs.Default;
using HarmonyLib;
using RimWorld;
using RTShared;
using RTShared.Misc;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;
using RTNetwork;
using RTNetwork.PacketManagers;
using RTNetwork.Packets;
using Verse;
using static RTShared.Misc.Printer;
using static RTNetwork.Packets.PKT_Save;
using RTNetwork.Components;
using GameClient.Managers;

namespace GameClient.PacketManagers
{
    public class PM_Saves : PM_Base
    {
        public static string LatestSavePath { get; set; } = string.Empty;

        public static string CustomSaveName => $"MP - {Network.Ip} - {Network.Port} - {SessionManager.Username}";

        public static string SaveFilePath => Path.Combine(Master.SavesFolderPath, CustomSaveName + ".rws");

        public static string TempSaveFilePath => SaveFilePath + ".temp";

        [HandlesPacket(PacketHeader.Save)]
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
            Printer.Warning("Force saving", Verbosity.Verbose);
            DLG_Base.PushNewDialog(new DLG_Wait());
            Find.MainTabsRoot.EscapeCurrentTab(playSound: false);

            Task.Run(delegate
            {
                Thread.Sleep(100);

                MainThreadManager.Instance.Enqueue(delegate
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
            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Save, data);
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

        private static void SendSaveToServer()
        {
            byte[] saveBytes;
            if (string.IsNullOrEmpty(LatestSavePath)) saveBytes = File.ReadAllBytes(SaveFilePath);
            else saveBytes = File.ReadAllBytes(LatestSavePath);

            PKT_Save data = new PKT_Save();
            data._stepMode = SaveStepMode.Receive;
            data._forceDisconnect = SessionManager.IsExiting;
            data._fileBytes = saveBytes;

            Network.ServerEndpoint.EnqueuePacket(PacketHeader.Save, data);
        }

        private static void OnSaveReceived(PKT_Save data)
        {
            Printer.Message($"Receiving save from server", Verbosity.Verbose);
            File.WriteAllBytes(TempSaveFilePath, data._fileBytes);

            if (data._forceUseSave)
            {
                File.Delete(SaveFilePath);
                File.Move(TempSaveFilePath, SaveFilePath);
            }

            else
            {
                if (GetRealPlayTimeInteractingFromSave(TempSaveFilePath) >= GetRealPlayTimeInteractingFromSave(SaveFilePath))
                {
                    Printer.Message("Loading remote save", Verbosity.Verbose);

                    File.Delete(PM_Saves.SaveFilePath);
                    File.Move(PM_Saves.TempSaveFilePath, PM_Saves.SaveFilePath);
                }

                else
                {
                    Printer.Message("Loading local save", Verbosity.Verbose);

                    File.Delete(PM_Saves.TempSaveFilePath);
                }
            }

            GameDataSaveLoader.LoadGame(PM_Saves.CustomSaveName);
        }

        public static void OnSave()
        {
            if (DLG_Options.CurrentSyncingMode == DLG_Options.SyncingMode.Complete || SessionManager.IsExiting)
            {
                Printer.Message("Sending maps to server", Verbosity.Verbose);
                PM_Map.SendPlayerMapsToServer();
            }

            Printer.Message("Sending save to server", Verbosity.Verbose);
            PM_Saves.SendSaveToServer();
        }
    }
}
