using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace GameClient.Misc
{
    public static class Finder
    {
        public static Map GetMapFromID(int id) { return Find.Maps.FirstOrDefault(fetch => fetch.uniqueID == id); }

        public static Pawn GetPawnFromID(Map map, string id) { return GetAllPawnsInMap(map).FirstOrDefault(fetch => fetch.ThingID == id); }

        public static Thing GetThingFromID(Map map, string id) { return GetAllThingsInMap(map).FirstOrDefault(fetch => fetch.ThingID == id); }

        public static Pawn[] GetAllPawnsInMap(Map map) { return map.mapPawns.AllPawns.ToArray(); }

        public static Thing[] GetAllThingsInMap(Map map) { return map.listerThings.AllThings.ToArray(); }
    }
}
