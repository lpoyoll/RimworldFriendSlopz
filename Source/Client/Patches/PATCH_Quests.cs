using System;
using System.Linq;
using GameClient.Managers;
using HarmonyLib;
using RimWorld;

namespace GameClient.Patches
{
    [HarmonyPatch(typeof(QuestManager), nameof(QuestManager.Add))]
    public static class PatchAddQuest
    {
        [HarmonyPrefix]
        public static bool DoPre(Quest quest)
        {
            foreach (Faction faction in SessionManager.PlayerFactions)
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
