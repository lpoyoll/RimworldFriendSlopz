using RimWorld;

namespace GameClient.Defs
{
    [DefOf]
    public static class RTSitePartDefOf
    {
        public static SitePartDef RTBase;

        static RTSitePartDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(SitePartDefOf));
    }
}
