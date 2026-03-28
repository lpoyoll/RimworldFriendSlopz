using Verse;
using static Shared.Misc.Printer;

namespace GameClient.Core.Configs
{
    public class ModConfigGetter : Verse.ModSettings
    {
        public static bool BypassModCompatibilityCheck;

        public static LogImportanceMode CurrentVerboseMode;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref CurrentVerboseMode, nameof(CurrentVerboseMode));
            Scribe_Values.Look(ref BypassModCompatibilityCheck, nameof(BypassModCompatibilityCheck));

            base.ExposeData();
        }
    }
}
