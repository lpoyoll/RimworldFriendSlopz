"""Stage B (buildings): merge OS OpenMap Local, Microsoft ML footprints and OSM attributes; attach INSPIRE plots.

Rules (docs/02):
  * OS OpenMap Local is the base geometry.
  * A Microsoft footprint is added only where no OS footprint overlaps it (IoU < 0.1 and < 30% of its area covered).
  * OSM contributes attributes only (type, levels, height, name, shop/amenity), by largest overlap.
  * Every footprint gets a stable ID from its source + source ID, and lists its sources.
"""
from __future__ import annotations

import re

import numpy as np

from .geo import CRS, stable_id

MIN_AREA_M2 = 6.0
SIMPLIFY_M = 0.2
MS_MAX_IOU = 0.1
MS_MAX_COVERED = 0.3

_HSTORE = re.compile(r'"((?:[^"\\]|\\.)*)"=>"((?:[^"\\]|\\.)*)"')


def parse_other_tags(s) -> dict[str, str]:
    """Parse GDAL's OSM 'other_tags' hstore string."""
    if not isinstance(s, str) or not s:
        return {}
    return {k: v for k, v in _HSTORE.findall(s)}


def clean_polygons(gdf):
    """Valid, oriented, simplified single polygons above the minimum area."""
    import shapely
    from shapely.geometry import Polygon

    gdf = gdf.copy()
    gdf["geometry"] = shapely.make_valid(gdf.geometry.values)
    gdf = gdf.explode(index_parts=False)
    gdf["part"] = gdf.groupby(level=0).cumcount()
    gdf = gdf[gdf.geometry.geom_type == "Polygon"]
    gdf["geometry"] = [shapely.geometry.polygon.orient(g.simplify(SIMPLIFY_M, preserve_topology=True), 1.0) for g in gdf.geometry]
    gdf = gdf[gdf.geometry.area >= MIN_AREA_M2]
    return gdf[gdf.geometry.apply(lambda g: isinstance(g, Polygon) and g.is_valid)].reset_index(drop=True)


def _source_ids(gdf, source: str, id_field: str | None) -> list[str]:
    if id_field and id_field in gdf.columns:
        # A source polygon repaired into several parts keeps one ID per part.
        return [stable_id("fp", source, v) if p == 0 else stable_id("fp", source, v, p) for v, p in zip(gdf[id_field], gdf["part"])]
    # No ID (MS footprints): hash rounded centroid, stable across runs.
    return [stable_id("fp", source, f"{g.centroid.x:.1f},{g.centroid.y:.1f}") for g in gdf.geometry]


def merge_footprints(os_gdf, ms_gdf=None, osm_gdf=None, os_id_field: str = "id"):
    """Return a GeoDataFrame: footprint_id, sources, osm attributes, geometry (EPSG:27700)."""
    import geopandas as gpd
    import pandas as pd

    base = clean_polygons(os_gdf[["geometry"] + ([os_id_field] if os_id_field in os_gdf.columns else [])])
    out = gpd.GeoDataFrame({
        "footprint_id": _source_ids(base, "os_openmap_local", os_id_field),
        "sources": [["os_openmap_local"] for _ in range(len(base))],
    }, geometry=base.geometry.values, crs=CRS)

    if ms_gdf is not None and len(ms_gdf):
        ms = clean_polygons(ms_gdf[["geometry"]])
        keep = []
        tree = out.sindex
        for g in ms.geometry:
            hits = tree.query(g, predicate="intersects")
            ok = True
            for i in hits:
                o = out.geometry.iloc[i]
                inter = g.intersection(o).area
                if inter / g.union(o).area >= MS_MAX_IOU or inter / g.area >= MS_MAX_COVERED:
                    ok = False
                    break
            keep.append(ok)
        ms = ms[np.array(keep, dtype=bool)]
        if len(ms):
            add = gpd.GeoDataFrame({
                "footprint_id": _source_ids(ms, "ms_building_footprints", None),
                "sources": [["ms_building_footprints"] for _ in range(len(ms))],
            }, geometry=ms.geometry.values, crs=CRS)
            out = gpd.GeoDataFrame(pd.concat([out, add], ignore_index=True), geometry="geometry", crs=CRS)

    out = attach_osm_attributes(out, osm_gdf)
    out = out.drop_duplicates("footprint_id").reset_index(drop=True)
    return out


OSM_ATTRS = ("building", "building:levels", "height", "roof:shape", "name", "shop", "amenity", "addr:street", "addr:housenumber")


def attach_osm_attributes(fp, osm_gdf):
    """Copy OSM tags onto each footprint from the OSM polygon with the largest overlap (>= 50% of footprint)."""
    fp = fp.copy()
    fp["osm"] = [{} for _ in range(len(fp))]
    if osm_gdf is None or not len(osm_gdf):
        return fp
    osm = osm_gdf[osm_gdf.geometry.geom_type.isin(["Polygon", "MultiPolygon"])].reset_index(drop=True)
    tags = []
    for _, row in osm.iterrows():
        t = parse_other_tags(row.get("other_tags"))
        for k in ("building", "name", "shop", "amenity"):
            v = row.get(k)
            if isinstance(v, str) and v:
                t[k] = v
        tags.append({k: t[k] for k in OSM_ATTRS if k in t})
    tree = osm.sindex
    for i, g in enumerate(fp.geometry):
        best, best_a = None, 0.0
        for j in tree.query(g, predicate="intersects"):
            a = g.intersection(osm.geometry.iloc[j]).area
            if a > best_a:
                best, best_a = j, a
        if best is not None and best_a / g.area >= 0.5 and tags[best]:
            fp.at[i, "osm"] = tags[best]
            fp.at[i, "sources"] = list(fp.at[i, "sources"]) + ["osm"]
    return fp


def attach_plots(fp, plots_gdf, plot_id_field: str = "INSPIREID"):
    """Give each footprint the INSPIRE plot containing its representative point."""
    fp = fp.copy()
    fp["plot_id"] = None
    if plots_gdf is None or not len(plots_gdf):
        return fp
    tree = plots_gdf.sindex
    for i, g in enumerate(fp.geometry):
        p = g.representative_point()
        hits = tree.query(p, predicate="within")
        if len(hits):
            v = plots_gdf.iloc[hits[0]][plot_id_field] if plot_id_field in plots_gdf.columns else hits[0]
            fp.at[i, "plot_id"] = stable_id("plt", "hmlr_inspire", v)
            if "hmlr_inspire" not in fp.at[i, "sources"]:
                fp.at[i, "sources"] = list(fp.at[i, "sources"]) + ["hmlr_inspire"]
    return fp


def shared_wall_counts(fp, min_shared_m: float = 3.0) -> list[int]:
    """How many neighbours each footprint shares a wall with (terrace = 2, semi = 1, detached = 0)."""
    geoms = list(fp.geometry)
    tree = fp.sindex
    counts = []
    for i, g in enumerate(geoms):
        n = 0
        grown = g.buffer(0.3)
        for j in tree.query(grown, predicate="intersects"):
            if j == i:
                continue
            shared = grown.intersection(geoms[j].exterior).length
            if shared >= min_shared_m:
                n += 1
        counts.append(n)
    return counts
