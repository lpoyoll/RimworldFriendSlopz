using System;
using Verse;
using static Shared.CommonEnumerators;
using static Shared.Misc.Printer;

namespace GameClient.Core.Configs
{
    public class ModConfigGetter : Verse.ModSettings
    {
        public enum SyncingMode { Fast, Complete }

        public static bool RejectTransfersBool;

        public static bool RejectSiteRewardsBool;

        public static bool BypassModCompatibilityCheck;

        public static LogImportanceMode CurrentVerboseMode;

        public static SyncingMode CurrentSyncingMode;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref RejectTransfersBool, nameof(RejectTransfersBool));
            Scribe_Values.Look(ref RejectSiteRewardsBool, nameof(RejectSiteRewardsBool));
            Scribe_Values.Look(ref CurrentVerboseMode, nameof(CurrentVerboseMode));
            Scribe_Values.Look(ref BypassModCompatibilityCheck, nameof(BypassModCompatibilityCheck));
            Scribe_Values.Look(ref CurrentSyncingMode, nameof(CurrentSyncingMode));

            base.ExposeData();
        }
    }
}
