using Verse;
using static Shared.Misc.Printer;

namespace GameClient.Core.Configs
{
    public class ModConfigGetter : Verse.ModSettings
    {
        public static bool BypassModCheck { get; set; } = false;

        public static Verbosity CurrentVerboseMode;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref CurrentVerboseMode, nameof(CurrentVerboseMode));

            base.ExposeData();
        }
    }
}
