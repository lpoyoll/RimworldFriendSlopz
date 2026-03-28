using RimWorld;

namespace GameClient.Defs
{
    [DefOf]
    public static class RTWorldObjectDefOf
    {
        public static WorldObjectDef RTCaravan;

        public static WorldObjectDef RTSettlement;

        public static WorldObjectDef RTSite;

        static RTWorldObjectDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(WorldObjectDefOf));
    }
}
