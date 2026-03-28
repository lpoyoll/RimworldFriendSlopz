using RimWorld;

namespace GameClient.Defs
{
    [DefOf]
    public static class RTSitePartDefOf
    {
        public static SitePartDef RTFarmland;

        public static SitePartDef RTHunterCamp;

        public static SitePartDef RTQuarry;

        public static SitePartDef RTSawmill;

        public static SitePartDef RTBank;

        public static SitePartDef RTLaboratory;

        public static SitePartDef RTRefinery;

        public static SitePartDef RTHerbalWorkshop;

        public static SitePartDef RTTextileFactory;

        public static SitePartDef RTFoodProcessor;

        static RTSitePartDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(SitePartDefOf));
    }
}
