using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameClient.Values;
using GameClient.WorldObjects;
using HarmonyLib;
using RimWorld.Planet;
using RimWorld;
using UnityEngine;
using Verse;
using static Shared.CommonEnumerators;
using GameClient.TCP;
using GameClient.Managers;
using GameClient.Dialogs;
using GameClient.Core;
using GameClient.Misc;

namespace GameClient.Patches.Pages
{
    [HarmonyPatch(typeof(Dialog_Options), "DoModOptions")]
    public static class PreventModOptionsButton
    {
        public static bool executedMessage;

        [HarmonyPrefix]
        public static bool DoPre(Dialog_Options __instance)
        {
            if (Network.state == ClientNetworkState.Disconnected) return true;
            else if (!SessionValues.configFile.EnforcedConfigs) return true;
            else if (ServerValues.isAdmin) return true;
            else
            {
                __instance.Close();

                if (!executedMessage)
                {
                    executedMessage = true;

                    DialogManager.PushNewDialog(new RT_Dialog_Message("Error",
                        new string[] { "Mod options can't be changed in this server!" },
                        delegate { executedMessage = false; }));
                }

                return false;
            }
        }
    }
}