using System;
using System.Collections.Generic;
using System.IO;
using GameClient.Dialogs;
using GameClient.Files;
using GameClient.Managers;
using GameClient.Misc;
using GameClient.TCP;
using GameClient.Values;
using Shared;
using UnityEngine;
using Verse;

namespace GameClient.Core.Preferences
{
    public static class UserLoginHandler
    {
        public static void SaveLoginData(LoginDataFile file) { Serializer.SerializeToFile(Master.loginDataPath, file); }

        public static LoginDataFile LoadLoginData()
        {
            // For testing purposes

            if (Input.GetKey(KeyCode.LeftShift)) return GetTestingLoginFile();
            else
            {
                if (File.Exists(Master.loginDataPath)) return Serializer.SerializeFromFile<LoginDataFile>(Master.loginDataPath);
                else return new LoginDataFile();
            }
        }

        public static void DeleteLoginData() { File.Delete(Master.loginDataPath); }

        public static void UseLoginData()
        {
            Printer.Warning("or Here?");

            if (Network.state != CommonEnumerators.ClientNetworkState.Connected) return;
            else
            {
                Printer.Warning(1);

                LoginDataFile file = LoadLoginData();
                ClientValues.Username = file.Username;
                ClientValues.Uid = file.UID;

                Printer.Warning(2);

                LoginData data = new LoginData();
                data._uid = file.UID;
                data._username = file.Username;
                data._runningMods = ModManagerH.GetRunningModList();

                Printer.Warning(3);

                Network.listener.EnqueuePacket(PacketHeader.LoginManager, data);

                Printer.Warning(4);
            }
        }

        private static LoginDataFile GetTestingLoginFile()
        {
            LoginDataFile file = new LoginDataFile();
            file.UID = "UID";
            file.Username = "Username";
            return file;
        }

        public static void PromptCreateAccount(bool isQuickConnect)
        {
            Action toDo = delegate
            {
                if (!StringChecker.CheckIfStringIsValid(DialogManager.dialogInputResults[0]))
                {
                    DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR",
                        new string[] { "Your username contains illegal characters", "Please choose another one and try again" }));
                }

                else
                {
                    AssignPlayerUsername(DialogManager.dialogInputResults[0]);
                    AssignPlayerHash();

                    if (isQuickConnect) QuickConnectUser();
                    else ConnectionManager.ShowConnectDialogs();
                }
            };

            DialogManager.PushNewDialog(new RT_Dialog_Inputs("Question", 
                new string[] { "What would you like your username to be?" }, new bool[] { false }, toDo));
        }

        public static void AssignPlayerUsername(string user)
        {
            LoginDataFile file = LoadLoginData();
            file.Username = user;
            SaveLoginData(file);
        }

        public static void AssignPlayerHash()
        {
            if (UserLoginManagerH.CheckIfLoginIsValid()) return;
            else
            {
                TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1);

                LoginDataFile file = LoadLoginData();
                file.UID = Hasher.GetHashFromString(timeSpan.TotalMilliseconds).Substring(0, 16);
                SaveLoginData(file);
            }
        }

        public static void QuickConnectUser()
        {
            UserLoginManagerH.SetupQuickConnectVariables();
            if (UserLoginManagerH.CheckIfQuickConnectIsValid()) UserLoginManagerH.ShowQuickConnectFloatMenu();
            else DialogManager.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "You must join a server first to use this feature!" }));
        }
    }

    public static class UserLoginManagerH
    {
        public static bool CheckIfLoginIsValid()
        {
            LoginDataFile file = UserLoginHandler.LoadLoginData();
            if (string.IsNullOrWhiteSpace(file.UID)) return false;
            else if (string.IsNullOrWhiteSpace(file.Username)) return false;
            else return true;
        }

        public static void SetupQuickConnectVariables()
        {
            ConnectionDataFile connectionData = ConnectionDataHandler.LoadConnectionData();
            Network.ip = connectionData.IP;
            Network.port = connectionData.Port;
        }

        public static void ShowQuickConnectFloatMenu()
        {
            List<Tuple<string, int>> quickConnectTuples = new List<Tuple<string, int>>()
            {
                Tuple.Create($"Join latest server > {Network.ip}:{Network.port}", 0),
                Tuple.Create("Show recently joined servers", 1)
            };

            FloatMenuOption tuple1 = new FloatMenuOption(quickConnectTuples[0].Item1, delegate
            {
                DialogManager.PushNewDialog(new RT_Dialog_Wait("Trying to connect to server"));
                Threader.GenerateThread(Threader.Mode.Start);
            });

            FloatMenuOption tuple2 = new FloatMenuOption(quickConnectTuples[1].Item1, 
                delegate { DialogManager.PushNewDialog(new RT_Dialog_ServerListing()); });

            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>() { tuple1, tuple2 }));
        }

        public static bool CheckIfQuickConnectIsValid()
        {
            if (string.IsNullOrWhiteSpace(Network.ip)) return false;
            else if (string.IsNullOrWhiteSpace(Network.port)) return false;
            else return true;
        }
    }
}
