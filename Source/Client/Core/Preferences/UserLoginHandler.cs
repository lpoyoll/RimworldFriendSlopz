using System;
using System.Collections.Generic;
using System.IO;
using GameClient.Dialogs;
using GameClient.Files;
using GameClient.Managers;
using GameClient.Misc;
using GameClient.Values;
using TCPNetwork.Packets;
using Shared;
using Steamworks;
using UnityEngine;
using Verse;
using Verse.Steam;

namespace GameClient.Core.Preferences
{
    public static class UserLoginHandler
    {
        public static void SaveLoginData(LoginDataFile file) { Serializer.SerializeToFile(Master.LoginDataPath, file); }

        public static LoginDataFile LoadLoginData()
        {
            RemoveOnNextUpdate.FixSpacesInUsername();

            // For testing purposes

            if (Input.GetKey(KeyCode.LeftShift)) return GetTestingLoginFile();
            else
            {
                if (File.Exists(Master.LoginDataPath)) return Serializer.SerializeFromFile<LoginDataFile>(Master.LoginDataPath);
                else return new LoginDataFile();
            }
        }

        public static void DeleteLoginData() { File.Delete(Master.LoginDataPath); }

        public static void UseLoginData()
        {
            if (SessionValues.CurrentNetworkState != CommonEnumerators.ClientNetworkState.Connected) return;
            else
            {
                LoginDataFile file = LoadLoginData();
                ClientValues.Username = file.Username;

                LoginData data = new LoginData();
                data._username = file.Username;
                data._password = file.Password;
                data._runningMods = ModManagerH.GetRunningModList();

                ClientNetwork.Instance.ClientListener.EnqueuePacket(PacketHeader.LoginManager, data);
            }
        }

        private static LoginDataFile GetTestingLoginFile()
        {
            LoginDataFile file = new LoginDataFile();
            file.Username = "Username";
            file.Password = "1234";
            return file;
        }

        public static void PromptCreateAccount(bool isQuickConnect)
        {
            Action toDo = delegate
            {
                bool isInvalid = false;
                if (!StringChecker.CheckIfStringValid(RT_Dialog_Inputs.DialogInputResults[0])) isInvalid = true;
                else if (!StringChecker.CheckIfStringValid(RT_Dialog_Inputs.DialogInputResults[1])) isInvalid = true;

                if (isInvalid)
                {
                    RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR",
                        new string[] { "Your login details contains illegal characters", "Please try again" }));
                }

                else
                {
                    AssignLoginDetails(RT_Dialog_Inputs.DialogInputResults[0], RT_Dialog_Inputs.DialogInputResults[1]);

                    if (isQuickConnect) QuickConnectUser();
                    else ConnectionManager.ShowConnectDialogs();
                }
            };

            Action toDo2 = delegate
            {
                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Inputs("Account Setup",
                    new string[] { "Username", "Password" }, new bool[] { false, true }, toDo));
            };

            RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("Account Setup", new string[] { "Please create or log into your account" }, toDo2));
        }

        public static void AssignLoginDetails(string username, string password)
        {
            LoginDataFile file = LoadLoginData();
            file.Username = username;
            file.Password = Hasher.GetHashFromString(password);

            SaveLoginData(file);
        }

        public static void QuickConnectUser()
        {
            UserLoginManagerH.SetupQuickConnectVariables();
            if (UserLoginManagerH.CheckIfQuickConnectIsValid()) UserLoginManagerH.ShowQuickConnectFloatMenu();
            else RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "You must join a server first to use this feature!" }));
        }
    }

    public static class UserLoginManagerH
    {
        public static bool CheckIfLoginIsValid()
        {
            LoginDataFile file = UserLoginHandler.LoadLoginData();
            if (string.IsNullOrWhiteSpace(file.Username)) return false;
            else if (string.IsNullOrWhiteSpace(file.Password)) return false;
            else return true;
        }

        public static void SetupQuickConnectVariables()
        {
            ConnectionDataFile connectionData = ConnectionDataHandler.LoadConnectionData();
            ClientNetwork.Ip = connectionData.IP;
            ClientNetwork.Port = connectionData.Port;
        }

        public static void ShowQuickConnectFloatMenu()
        {
            List<Tuple<string, int>> quickConnectTuples = new List<Tuple<string, int>>()
            {
                Tuple.Create($"Join latest server > {ClientNetwork.Ip}:{ClientNetwork.Port}", 0),
            };

            FloatMenuOption tuple1 = new FloatMenuOption(quickConnectTuples[0].Item1, delegate
            {
                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Wait("Trying to connect to server"));
                ClientNetwork _ = new ClientNetwork();
            });

            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>() { tuple1 }));
        }

        public static bool CheckIfQuickConnectIsValid()
        {
            if (string.IsNullOrWhiteSpace(ClientNetwork.Ip)) return false;
            else if (string.IsNullOrWhiteSpace(ClientNetwork.Port)) return false;
            else return true;
        }
    }
}
