using GameClient.Values;
using RimWorld;
using RimWorld.Planet;
using Shared;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace GameClient.Managers
{
    public static class RiverManager
    {
        public static void SetPlanetRivers()
        {
            if (SessionValues.WorldFile.Rivers == null) return;
            else AddRivers(SessionValues.WorldFile.Rivers, false);
        }

        public static void AddRivers(RiverDetails[] rivers, bool forceRefresh)
        {
            if (rivers == null) return;

            foreach (RiverDetails details in rivers)
            {
                RiverDef riverDef = DefDatabase<RiverDef>.AllDefs.First(fetch => fetch.defName == details.RiverDefName);

                AddRiverSimple(details.FromTile, details.ToTile, riverDef, forceRefresh);
            }

            //If we don't want to force refresh we wait for all and then refresh the layer

            if (!forceRefresh) RiverManagerHelper.ForceRiverLayerRefresh();
        }

        public static void AddRiverSimple(int tileAID, int tileBID, RiverDef riverDef, bool forceRefresh)
        {
            SurfaceTile tileA = Find.WorldGrid[tileAID];
            SurfaceTile tileB = Find.WorldGrid[tileBID];

            AddRiverLink(tileA, tileBID, riverDef);
            AddRiverLink(tileB, tileAID, riverDef);

            if (forceRefresh) RiverManagerHelper.ForceRiverLayerRefresh();
        }

        private static void AddRiverLink(SurfaceTile toAddTo, int neighborTileID, RiverDef riverDef)
        {
            if (toAddTo.Rivers != null)
            {
                foreach (SurfaceTile.RiverLink link in toAddTo.Rivers)
                {
                    if (link.neighbor == neighborTileID) return;
                }
            }

            SurfaceTile.RiverLink linkToAdd = new SurfaceTile.RiverLink
            {
                neighbor = neighborTileID,
                river = riverDef
            };

            toAddTo.potentialRivers ??= new List<SurfaceTile.RiverLink>();
            toAddTo.potentialRivers.Add(linkToAdd);
        }
    }

    public static class RiverManagerHelper
    {
        public static RiverDetails[] GetPlanetRivers()
        {
            List<RiverDetails> toGet = new List<RiverDetails>();

            foreach (SurfaceTile tile in Find.WorldGrid.Tiles)
            {
                if (tile.Rivers != null)
                {
                    foreach (SurfaceTile.RiverLink link in tile.Rivers)
                    {
                        RiverDetails details = new RiverDetails(Find.WorldGrid.Tiles.IndexOf(tile), link.neighbor, link.river.defName);

                        if (!CheckIfExists(details.FromTile, details.ToTile)) toGet.Add(details);
                    }
                }
            }
            return toGet.ToArray();

            bool CheckIfExists(int tileA, int tileB)
            {
                foreach (RiverDetails details in toGet)
                {
                    if (details.FromTile == tileA && details.ToTile == tileB) return true;
                    else if (details.FromTile == tileB && details.ToTile == tileA) return true;
                }

                return false;
            }
        }

        public static void ForceRiverLayerRefresh()
        {
            Find.World.renderer.SetDirty<WorldDrawLayer>(PlanetLayer.Selected);
            Find.World.renderer.RegenerateLayersIfDirtyInLongEvent();
        }
    }
}
