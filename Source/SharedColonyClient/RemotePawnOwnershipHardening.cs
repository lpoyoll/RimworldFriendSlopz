using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RWTSharedColony
{
    /// <summary>
    /// v0.1.19+: remote pawns are visible/inspectable mirrors, never locally controllable.
    /// Server-admin status is deliberately irrelevant to pawn ownership.
    ///
    /// RWT's synchronous code uses PatchHandler.BypassFlag both while applying network
    /// actions and in a few local synchronous paths. Therefore BypassFlag alone is not
    /// a sufficient authority check. A remote pawn action is allowed only when the call
    /// is actually inside RTClient's synchronous packet-manager stack.
    /// </summary>
    public static class RemotePawnControlGuard
    {
        public static bool IsRemotePawn(Pawn pawn)
        {
            return pawn != null && PlayerFactionRegistry.IsRemoteFaction(pawn.Faction);
        }

        public static bool IsApplyingNetworkAction()
        {
            if (!SharedColonyState.IsRwtBypassActive()) return false;

            try
            {
                StackFrame[] frames = new StackTrace(false).GetFrames();
                if (frames == null) return false;

                foreach (StackFrame frame in frames)
                {
                    MethodBase method = frame?.GetMethod();
                    string typeName = method?.DeclaringType?.FullName;
                    if (string.IsNullOrWhiteSpace(typeName)) continue;

                    if (typeName.StartsWith("RTClient.PacketManagers.Synchronous.", StringComparison.Ordinal) ||
                        string.Equals(typeName, "RTClient.PacketManagers.Synchronous.PM_Synchronous", StringComparison.Ordinal))
                        return true;
                }
            }
            catch
            {
                // Fail closed for remote pawn control.
            }

            return false;
        }

        public static string OwnerLabel(Pawn pawn)
        {
            string name = pawn?.Faction?.Name;
            return string.IsNullOrWhiteSpace(name) ? "another player" : name;
        }

        public static void RejectLocalControl(Pawn pawn, string action)
        {
            try
            {
                string owner = OwnerLabel(pawn);
                RimjobClientDiagnostics.Important($"Blocked local {action} of remote pawn {pawn?.LabelShort ?? "<unknown>"}; owner={owner}; admin={RTClient.Managers.SessionManager.IsAdmin}.");
                Messages.Message($"{pawn?.LabelShortCap ?? "This pawn"} belongs to {owner}. Only that player can control this pawn.",
                    MessageTypeDefOf.RejectInput, false);
            }
            catch
            {
                // Ownership enforcement must never fail because the notification failed.
            }
        }
    }

    [HarmonyPatch]
    public static class RemotePawnPlayerControlledPropertyPatch
    {
        public static bool Prepare() => AccessTools.PropertyGetter(typeof(Pawn), "IsColonistPlayerControlled") != null;

        public static MethodBase TargetMethod() => AccessTools.PropertyGetter(typeof(Pawn), "IsColonistPlayerControlled");

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Pawn __instance, ref bool __result)
        {
            if (RemotePawnControlGuard.IsRemotePawn(__instance)) __result = false;
        }
    }

    /// <summary>
    /// Remove right-click orders for a remote pawn. RimWorld 1.6 ChoicesAtFor
    /// returns List&lt;FloatMenuOption&gt;; the Harmony __result type must match exactly.
    /// v0.1.19 used IEnumerable here, which could abort PatchAll and indirectly
    /// disable the shared starting-tile patches.
    /// </summary>
    [HarmonyPatch]
    public static class RemotePawnFloatMenuPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            return typeof(FloatMenuMakerMap).GetMethods(AccessTools.all)
                .Where(method => method.Name == "ChoicesAtFor" &&
                                 method.ReturnType == typeof(List<FloatMenuOption>) &&
                                 method.GetParameters().Any(parameter => parameter.ParameterType == typeof(Pawn)));
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(object[] __args, ref List<FloatMenuOption> __result)
        {
            Pawn pawn = __args?.OfType<Pawn>().FirstOrDefault();
            if (!RemotePawnControlGuard.IsRemotePawn(pawn)) return true;

            __result = new List<FloatMenuOption>();
            return false;
        }
    }

    [HarmonyPatch]
    public static class RemotePawnDraftOwnershipPatch
    {
        public static bool Prepare() => AccessTools.PropertySetter(typeof(Pawn_DraftController), "Drafted") != null;

        public static MethodBase TargetMethod() => AccessTools.PropertySetter(typeof(Pawn_DraftController), "Drafted");

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(Pawn_DraftController __instance)
        {
            Pawn pawn = AccessTools.Field(typeof(Pawn_DraftController), "pawn")?.GetValue(__instance) as Pawn;
            if (!RemotePawnControlGuard.IsRemotePawn(pawn)) return true;
            if (RemotePawnControlGuard.IsApplyingNetworkAction()) return true;

            RemotePawnControlGuard.RejectLocalControl(pawn, "draft/undraft");
            return false;
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
    public static class RemotePawnStartJobOwnershipPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(Pawn ___pawn)
        {
            if (!SharedTileLiveSync.IsSharedSessionActive) return true;
            if (!RemotePawnControlGuard.IsRemotePawn(___pawn)) return true;
            return RemotePawnControlGuard.IsApplyingNetworkAction();
        }
    }
}
