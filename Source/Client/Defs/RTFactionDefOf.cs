using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameClient.Defs
{
    [DefOf]
    public static class RTFactionDefOf
    {
        public static FactionDef RTNeutral;

        public static FactionDef RTAlly;

        public static FactionDef RTEnemy;

        public static FactionDef RTFaction;

        static RTFactionDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(FactionDefOf));
    }
}
