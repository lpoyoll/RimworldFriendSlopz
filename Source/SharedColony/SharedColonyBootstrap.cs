using HarmonyLib;
using RTClient.Dialogs;
using System.Reflection;
using Verse;

namespace Rimjob.SharedColony
{
    [StaticConstructorOnStartup]
    public static class SharedColonyBootstrap
    {
        static SharedColonyBootstrap()
        {
            new Harmony("rimjob.shared-colony").PatchAll();
            EnableSynchronousMode();
            Log.Message("[Rimjob] Shared-colony visibility patches loaded");
        }

        internal static void EnableSynchronousMode()
        {
            PropertyInfo property = AccessTools.Property(typeof(DLG_Options), "EnablePreviewFeatures");
            if (property != null)
            {
                property.SetValue(null, true, null);
                return;
            }

            AccessTools.Field(typeof(DLG_Options), "EnablePreviewFeatures")?.SetValue(null, true);
        }
    }
}
