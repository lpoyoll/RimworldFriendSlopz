using System;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RWTSharedColony
{
    /// <summary>
    /// Keeps the shared starting-site path independent from optional UI/control
    /// patches. If a later Harmony patch fails during the assembly-wide PatchAll,
    /// this bootstrap still installs the three critical same-tile hooks under a
    /// separate Harmony owner.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class CriticalSharedTileBootstrap
    {
        private const string HarmonyId = "rimjob.shared-tile-critical";

        static CriticalSharedTileBootstrap()
        {
            try
            {
                Harmony harmony = new Harmony(HarmonyId);

                MethodInfo validator = AccessTools.Method(typeof(TileFinder), nameof(TileFinder.IsValidTileForNewSettlement));
                MethodInfo validatorPostfix = AccessTools.Method(typeof(SharedTileSettlementPatch), nameof(SharedTileSettlementPatch.Postfix));
                PatchIfMissing(harmony, validator, null, validatorPostfix);

                MethodInfo canNext = AccessTools.Method(typeof(Page_SelectStartingSite), "CanDoNext");
                MethodInfo canNextPrefix = AccessTools.Method(typeof(SharedTileStartingSiteCanNextPatch), nameof(SharedTileStartingSiteCanNextPatch.Prefix));
                PatchIfMissing(harmony, canNext, canNextPrefix, null);

                MethodInfo doNext = AccessTools.Method(typeof(Page_SelectStartingSite), "DoNext");
                MethodInfo doNextPrefix = AccessTools.Method(typeof(SharedTileStartingSiteDoNextPatch), nameof(SharedTileStartingSiteDoNextPatch.Prefix));
                PatchIfMissing(harmony, doNext, doNextPrefix, null);

                Log.Message("[Rimjob] Critical shared-tile starting-site patches verified.");
            }
            catch (Exception exception)
            {
                Log.Error("[Rimjob] CRITICAL: could not install shared-tile starting-site patches: " + exception);
            }
        }

        private static void PatchIfMissing(Harmony harmony, MethodBase original, MethodInfo prefix, MethodInfo postfix)
        {
            if (original == null) throw new MissingMethodException("Critical Rimjob target method was not found.");

            Patches info = Harmony.GetPatchInfo(original);
            bool alreadyPatched = info != null && info.Owners.Any(owner =>
                string.Equals(owner, HarmonyId, StringComparison.Ordinal) ||
                string.Equals(owner, "rwt.shared-colony", StringComparison.Ordinal));
            if (alreadyPatched) return;

            HarmonyMethod prefixPatch = prefix == null ? null : new HarmonyMethod(prefix) { priority = Priority.First };
            HarmonyMethod postfixPatch = postfix == null ? null : new HarmonyMethod(postfix) { priority = Priority.Last };
            harmony.Patch(original, prefix: prefixPatch, postfix: postfixPatch);
        }
    }
}
