using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GameClient.Values;
using HarmonyLib;
using Verse;
using RimWorld;
using GameClient.TCP;
using static Shared.CommonEnumerators;
using GameClient.Misc;

namespace GameClient.Patches
{
    [HarmonyPatch(typeof(QuestManager), nameof(QuestManager.Add))]
    public static class PatchAddPollution
    {
        [HarmonyPrefix]
        public static bool DoPre(Quest quest)
        {
            if (Network.state == ClientNetworkState.Disconnected) return true;

            foreach (Faction faction in FactionValues.playerFactions)
            {
                if (quest.InvolvedFactions.Contains(faction))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
