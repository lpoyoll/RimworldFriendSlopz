using System;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RWTSharedColony
{
    /// <summary>
    /// RimWorld normally refuses to use a world tile that already contains a
    /// world object. For Rimjob, an RTSettlement is deliberately shareable:
    /// several independently owned player settlements may occupy the same
    /// server tile up to the configured Shared Colony capacity.
    ///
    /// There are two separate vanilla gates here:
    /// 1. TileFinder validates whether a tile is legal.
    /// 2. Page_SelectStartingSite requires WorldInterface.SelectedTile to be
    ///    valid, even when clicking a world object only selects the object.
    ///
    /// v0.1.9 handled gate 1 only. v0.1.10 also promotes a clicked RTSettlement
    /// object's tile into SelectedTile before CanDoNext/DoNext run.
    /// </summary>
    [HarmonyPatch(typeof(TileFinder), nameof(TileFinder.IsValidTileForNewSettlement))]
    public static class SharedTileSettlementPatch
    {
        public static bool Prefix(ref bool __result, PlanetTile tile, StringBuilder reason, bool forGravship)
        {
            try
            {
                // Never interfere with gravship landing checks or game/load caches.
                if (forGravship ||
                    Scribe.mode != LoadSaveMode.Inactive ||
                    Current.ProgramState != ProgramState.Playing ||
                    Current.Game == null ||
                    Find.World == null ||
                    Find.WorldGrid == null ||
                    Find.WorldObjects == null)
                {
                    return true;
                }

                int tileId = tile.tileId;
                if (tileId < 0 || tileId >= Find.WorldGrid.TilesCount)
                {
                    return true;
                }

                var objectsAtTile = Find.WorldObjects.AllWorldObjects
                    .Where(worldObject => worldObject != null && worldObject.Tile == tile)
                    .ToList();

                var sharedSettlements = objectsAtTile
                    .Where(SharedTileSelectionUtility.IsRemotePlayerSettlement)
                    .ToList();

                // Not a Rimjob shared-colony tile: use ordinary RimWorld rules.
                if (sharedSettlements.Count == 0)
                {
                    return true;
                }

                // Do not make arbitrary occupied tiles legal. If anything other
                // than RTSettlement exists on this exact tile, vanilla decides.
                if (objectsAtTile.Any(worldObject => !SharedTileSelectionUtility.IsRemotePlayerSettlement(worldObject)))
                {
                    return true;
                }

                int capacity = Math.Max(1, SharedColonyState.TileCapacity);
                if (sharedSettlements.Count >= capacity)
                {
                    SetReason(reason, $"Rimjob shared tile is full ({sharedSettlements.Count}/{capacity} settlements)." );
                    __result = false;
                    return false;
                }

                // Keep the important vanilla terrain rule even though we are
                // deliberately bypassing the occupied-world-object rule.
                var worldTile = Find.WorldGrid[tileId];
                var biome = worldTile.PrimaryBiome;
                if (worldTile.WaterCovered || biome == null || !biome.canBuildBase)
                {
                    return true;
                }

                SetReason(reason, $"Rimjob shared tile ({sharedSettlements.Count}/{capacity} occupied)." );
                __result = true;
                return false;
            }
            catch (Exception exception)
            {
                // Fail closed: if our special-case logic cannot prove this is a
                // safe shared tile, fall back to RimWorld's original validator.
                Log.Warning($"[RWT Shared Colony] shared starting-tile check failed: {exception.Message}");
                return true;
            }
        }

        private static void SetReason(StringBuilder reason, string value)
        {
            if (reason == null) return;
            reason.Clear();
            reason.Append(value);
        }
    }

    internal static class SharedTileSelectionUtility
    {
        public static bool IsRemotePlayerSettlement(WorldObject worldObject)
        {
            return worldObject?.def?.defName == "RTSettlement";
        }

        /// <summary>
        /// Clicking an occupied RTSettlement selects the WorldObject, not the
        /// underlying tile. Vanilla Page_SelectStartingSite.CanDoNext checks
        /// only WorldInterface.SelectedTile and therefore reports
        /// "Please select a site". Promote the selected RTSettlement's tile so
        /// the ordinary next-page flow can continue through our TileFinder rule.
        /// </summary>
        public static void PromoteSelectedSharedTile()
        {
            try
            {
                if (Find.WorldInterface == null || Find.WorldSelector == null) return;
                if (Find.WorldInterface.SelectedTile.Valid) return;

                WorldObject selectedObject = Find.WorldSelector.FirstSelectedObject;
                if (!IsRemotePlayerSettlement(selectedObject)) return;
                if (!selectedObject.Tile.Valid) return;

                Find.WorldInterface.SelectedTile = selectedObject.Tile;
                if (Find.GameInitData != null)
                {
                    Find.GameInitData.startingTile = selectedObject.Tile;
                }

                Log.Message($"[RWT Shared Colony] selected occupied player tile {selectedObject.Tile.tileId} for shared settlement");
            }
            catch (Exception exception)
            {
                Log.Warning($"[RWT Shared Colony] could not promote selected shared tile: {exception.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(Page_SelectStartingSite), "CanDoNext")]
    public static class SharedTileStartingSiteCanNextPatch
    {
        public static void Prefix()
        {
            SharedTileSelectionUtility.PromoteSelectedSharedTile();
        }
    }

    [HarmonyPatch(typeof(Page_SelectStartingSite), "DoNext")]
    public static class SharedTileStartingSiteDoNextPatch
    {
        public static void Prefix()
        {
            SharedTileSelectionUtility.PromoteSelectedSharedTile();
        }
    }
}
