using GameClient.Core;
using GameClient.Core.Configs;
using GameClient.Dialogs;
using HarmonyLib;
using RTShared;
using RTShared.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static RTShared.Misc.Printer;

namespace GameClient.Misc
{
    public static class HarmonyHandler
    {
        private static Harmony MainInstance { get; set; } = null;

        public static Harmony SynchronousInstance { get; private set; } = null;

        private static readonly string HarmonyStartID = $"{Master.ModID}-Start";

        private static readonly string HarmonyMainID = $"{Master.ModID}-Main";

        public static void EnableStartPatches() { new Harmony(HarmonyStartID).PatchCategory("Start"); }

        public static void EnableMainPatches()
        {
            if (MainInstance == null) MainInstance = new Harmony(HarmonyMainID);
            MainInstance.PatchAllUncategorized(Assembly.GetExecutingAssembly());
        }

        [OnSessionEnd]
        private static void DisableMainPatches() 
        {
            PatchClassProcessor[] sequence = (from type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly())
                where type.HasHarmonyAttribute() select type).Select(CreateClassProcessor).ToArray();

            sequence.DoIf((PatchClassProcessor patchClass) => string.IsNullOrEmpty(patchClass.Category), delegate (PatchClassProcessor patchClass)
            {
                patchClass.Unpatch();
            });
        }

        [OnSynchronousStart]
        private static void EnableSynchronousPatches()
        {
            if (SynchronousInstance == null) SynchronousInstance = new Harmony("RimWorld Together Synchronous");

            SynchronousInstance.PatchCategory(Assembly.GetExecutingAssembly(), "Synchronous");

            Printer.Warning("Patched Synchronous methods", Verbosity.Verbose);
        }

        [OnSessionEnd]
        [OnSynchronousEnd]
        private static void DisableSynchronousPatches()
        {
            if (SynchronousInstance != null)
            {
                SynchronousInstance.UnpatchCategory(Assembly.GetExecutingAssembly(), "Synchronous");

                Printer.Warning("Unpatched Synchronous methods", Verbosity.Verbose);
            }
        }

        private static PatchClassProcessor CreateClassProcessor(Type type) { return new PatchClassProcessor(MainInstance, type); }

        public static bool CheckForModCollision()
        {
            if (ModConfigGetter.BypassModCheck) return true;

            EnableMainPatches();

            List<string> collidingMods = new List<string>();

            foreach (MethodBase method in MainInstance.GetPatchedMethods())
            {
                HarmonyLib.Patches patchInfo = Harmony.GetPatchInfo(method);

                foreach (HarmonyLib.Patch patch in patchInfo.Prefixes)
                {
                    if (patch.owner != HarmonyMainID && patch.owner != HarmonyStartID)
                    {
                        if (!collidingMods.Contains(patch.owner)) collidingMods.Add(patch.owner);
                    }
                }

                foreach (HarmonyLib.Patch patch in patchInfo.Transpilers)
                {
                    if (patch.owner != HarmonyMainID && patch.owner != HarmonyStartID)
                    {
                        if (!collidingMods.Contains(patch.owner)) collidingMods.Add(patch.owner);
                    }
                }
            }

            DisableMainPatches();

            if (collidingMods.Count == 0)
            {
                ModConfigGetter.BypassModCheck = true;
                return true;
            }

            else 
            {
                DLG_Base.PushNewDialog(new DLG_Compatibility(collidingMods));
                return false;
            }
        }
    }
}
