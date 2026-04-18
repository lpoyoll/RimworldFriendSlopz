using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace GameClient.Patches
{
    [HarmonyPatch(typeof(Autosaver), "AutosaveIntervalDays", MethodType.Getter)]
    public static class Patch_Autosaver_AutosaveIntervalDays
    {
        [HarmonyPrefix]
        public static bool Prefix(ref float __result)
        {
            __result = Prefs.AutosaveIntervalDays;
            return false;
        }
    }

    [HarmonyPatch(typeof(Dialog_Options), "DoGeneralOptions")]
    public static class DoGeneralOptions_Patch
    {
        public static bool skipNextLabel = false;

        [HarmonyPrefix]
        public static void Prefix() { skipNextLabel = false; }

        [HarmonyPatch(typeof(Listing_Standard), "Label", new Type[] { typeof(TaggedString), typeof(float), typeof(string) })]
        public static class Label_Patch
        {
            [HarmonyPrefix]
            public static bool Prefix(ref TaggedString label)
            {
                if (skipNextLabel) return true;

                if (label == "MaxPermadeathAutosaveIntervalInfo".Translate(1f))
                {
                    label = "Permadeath Autosave interval currently overriden by player".Colorize(Color.green);
                    skipNextLabel = true;
                }

                return true;
            }
        }
    }
}
