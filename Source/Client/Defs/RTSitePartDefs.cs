using GameClient.WorldObjects;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace GameClient.Defs
{
    [StaticConstructorOnStartup]
    public static class RTSitePartDefs
    {
        public static SitePartDef[] Defs { get; set; } = new SitePartDef[]
        {
            RTSitePartDefOf.RTFarmland,
            RTSitePartDefOf.RTHunterCamp,
            RTSitePartDefOf.RTQuarry,
            RTSitePartDefOf.RTSawmill,
            RTSitePartDefOf.RTBank,
            RTSitePartDefOf.RTLaboratory,
            RTSitePartDefOf.RTRefinery,
            RTSitePartDefOf.RTHerbalWorkshop,
            RTSitePartDefOf.RTTextileFactory,
            RTSitePartDefOf.RTFoodProcessor
        };
    }
}
