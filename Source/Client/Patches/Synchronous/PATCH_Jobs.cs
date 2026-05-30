using GameClient.Managers;
using GameClient.Misc;
using GameClient.PacketManagers.Synchronous;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace GameClient.Patches.Synchronous
{
    [HarmonyPatchCategory("Synchronous")]
    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    public static class P_Pawn_JobTracker_StartJob
    {
        [HarmonyPrefix]
        public static bool StartJob(Pawn ___pawn, Job newJob)
        {
            if (PatchHandler.BypassFlag) return true;
            else if (!SynchronousManager.CheckIfShouldPatch(___pawn.MapHeld)) return true;
            else if (___pawn.Faction == SessionManager.NeutralFaction) return false;
            else
            {
                PM_SJob.Ask(newJob, ___pawn);
                return false;
            }
        }
    }
}
