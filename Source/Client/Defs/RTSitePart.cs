using GameClient.WorldObjects;
using RimWorld;
using Verse;

namespace GameClient.Defs
{
    public class RTSitePart : IExposable
    {
        public WO_Site site;

        public SitePartDef def;

        public RTSitePart(WO_Site site, SitePartDef def)
        {
            this.site = site;
            this.def = def;
        }

        public void ExposeData() { Scribe_Defs.Look(ref def, "def"); }
    }
}
