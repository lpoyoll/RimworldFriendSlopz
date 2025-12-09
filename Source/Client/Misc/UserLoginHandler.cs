using GameClient.Dialogs;
using GameClient.Files;
using GameClient.Managers;
using GameClient.Values;
using Shared;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using TCPNetwork.Packets;
using UnityEngine;
using Verse;
using Verse.Steam;
using static Mono.Security.X509.X520;

namespace GameClient.Misc
{
    public static class UserLoginHandler
    {
        public static void UseLoginData()
        {
            if (SessionValues.CurrentNetworkState != CommonEnumerators.ClientNetworkState.Connected) return;
            else
            {
                LoginData data = new LoginData();

                if (Input.GetKey(KeyCode.LeftShift))
                {
                    data._username = "Test";
                    data._password = "1234";
                }

                else
                {
                    PersistentSettings settings = PersistentSettings.Load();
                    data._username = settings.UserSettings.Username;
                    data._password = settings.UserSettings.Password;
                }

                ClientValues.Username = data._username;
                data._runningMods = ModManagerH.GetRunningModList();
                ClientNetwork.Instance.ClientListener.EnqueuePacket(PacketHeader.LoginManager, data);
            }
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
                    PersistentSettings settings = PersistentSettings.Load();
                    settings.UserSettings.Set(RT_Dialog_Inputs.DialogInputResults[0], Hasher.GetHashFromString(RT_Dialog_Inputs.DialogInputResults[1]));
                    settings.Save();

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

        public static void QuickConnectUser()
        {
            PersistentSettings settings = PersistentSettings.Load();
            TCPNetwork.Network.Ip = settings.ServerSettings.LatestIP;
            TCPNetwork.Network.Port = settings.ServerSettings.LatestPort;

            if (StringChecker.CheckIfStringValid(TCPNetwork.Network.Ip) && StringChecker.CheckIfStringValid(TCPNetwork.Network.Port))
            {
                UserLoginManagerH.ShowQuickConnectFloatMenu();
            }

            else
            {
                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Message("ERROR", new string[] { "You must join a server first to use this feature!" }));
            }
        }
    }

    public static class UserLoginManagerH
    {
        public static bool CheckIfLoginIsValid()
        {
            PersistentSettings settings = PersistentSettings.Load();
            if (!StringChecker.CheckIfStringValid(settings.UserSettings.Username)) return false;
            else if (!StringChecker.CheckIfStringValid(settings.UserSettings.Password)) return false;
            else return true;
        }

        public static void ShowQuickConnectFloatMenu()
        {
            List<Tuple<string, int>> quickConnectTuples = new List<Tuple<string, int>>()
            {
                Tuple.Create($"Join latest server > {TCPNetwork.Network.Ip}:{TCPNetwork.Network.Port}", 0),
            };

            FloatMenuOption tuple1 = new FloatMenuOption(quickConnectTuples[0].Item1, delegate
            {
                RT_Dialog_Base.PushNewDialog(new RT_Dialog_Wait("Trying to connect to server"));
                ClientNetwork _ = new ClientNetwork();
            });

            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>() { tuple1 }));
        }
    }
}
