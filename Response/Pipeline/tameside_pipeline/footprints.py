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

    if not len(gdf):
        return gdf.assign(part=[]).reset_index(drop=True)
    gdf = gdf.copy()
    gdf["geometry"] = shapely.force_2d(shapely.make_valid(gdf.geometry.values))  # OS shapefiles carry Z=0
    gdf = gdf.explode(index_parts=False)
    gdf["part"] = gdf.groupby(level=0).cumcount()
    gdf = gdf[gdf.geometry.geom_type == "Polygon"]
    gdf["geometry"] = [shapely.geometry.polygon.orient(g.simplify(SIMPLIFY_M, preserve_topology=True), 1.0) for g in gdf.geometry]
    gdf = gdf[gdf.geometry.area >= MIN_AREA_M2]
    keep = np.array([isinstance(g, Polygon) and g.is_valid for g in gdf.geometry], dtype=bool)
    return gdf[keep].reset_index(drop=True)


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
    """Copy OSM tags onto each footprint and count the OSM buildings inside it.

    OS OpenMap Local merges attached buildings (a semi pair or a terrace row is one polygon), while OSM often maps
    each house. An OSM polygon matches if it covers >= 50% of the footprint, or if >= 50% of it lies inside the
    footprint. Tags come from the largest match; `osm_units` counts the matched OSM buildings.
    """
    fp = fp.copy()
    fp["osm"] = [{} for _ in range(len(fp))]
    fp["osm_units"] = 0
    if osm_gdf is None or not len(osm_gdf):
        return fp
    osm = osm_gdf[osm_gdf.geometry.geom_type.isin(["Polygon", "MultiPolygon"])].reset_index(drop=True)
    if "building" in osm.columns:
        osm = osm[osm["building"].notna()].reset_index(drop=True)
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
        best, best_a, units = None, 0.0, 0
        for j in tree.query(g, predicate="intersects"):
            o = osm.geometry.iloc[j]
            a = g.intersection(o).area
            if a / g.area >= 0.5 or a / o.area >= 0.5:
                units += 1
                if a > best_a:
                    best, best_a = j, a
        fp.at[i, "osm_units"] = units
        if best is not None and tags[best]:
            fp.at[i, "osm"] = tags[best]
            fp.at[i, "sources"] = list(fp.at[i, "sources"]) + ["osm"]
    return fp


def attach_address_counts(fp, points_gdf, tolerance_m: float = 0.5):
    """Count OS Open UPRN address points in each footprint (a proxy for dwellings / units)."""
    fp = fp.copy()
    fp["addresses"] = 0
    if points_gdf is None or not len(points_gdf):
        return fp
    tree = points_gdf.sindex
    for i, g in enumerate(fp.geometry):
        fp.at[i, "addresses"] = len(tree.query(g.buffer(tolerance_m), predicate="intersects"))
        if fp.at[i, "addresses"] and "os_open_uprn" not in fp.at[i, "sources"]:
            fp.at[i, "sources"] = list(fp.at[i, "sources"]) + ["os_open_uprn"]
    return fp


def read_uprn_csv(path, bounds):
    """OS Open UPRN CSV -> point GeoDataFrame clipped to bounds."""
    import geopandas as gpd
    import pandas as pd

    df = pd.read_csv(path, usecols=lambda c: c.lstrip("\ufeff") in ("UPRN", "X_COORDINATE", "Y_COORDINATE"))
    df.columns = [c.lstrip("\ufeff") for c in df.columns]
    min_e, min_n, max_e, max_n = bounds
    df = df[(df.X_COORDINATE >= min_e) & (df.X_COORDINATE <= max_e) & (df.Y_COORDINATE >= min_n) & (df.Y_COORDINATE <= max_n)]
    return gpd.GeoDataFrame(df[["UPRN"]], geometry=gpd.points_from_xy(df.X_COORDINATE, df.Y_COORDINATE), crs=CRS)


def split_units(geom, n: int) -> list:
    """Split a merged footprint (semi pair, terrace row) into n equal bays along its long axis.

    Party walls run across the long axis of the minimum rotated rectangle. Returns n polygons (or [geom] if n < 2).
    """
    import math

    from shapely import affinity
    from shapely.geometry import box as sbox

    if n < 2:
        return [geom]
    rect = geom.minimum_rotated_rectangle
    xy = list(rect.exterior.coords)[:4]
    edges = [(xy[k], xy[(k + 1) % 4]) for k in range(2)]
    (a, b) = max(edges, key=lambda e: math.dist(*e))
    ang = math.degrees(math.atan2(b[1] - a[1], b[0] - a[0]))
    c = rect.centroid
    flat = affinity.rotate(geom, -ang, origin=c)
    minx, miny, maxx, maxy = flat.bounds
    w = (maxx - minx) / n
    parts = []
    for k in range(n):
        strip = sbox(minx + k * w, miny - 1, minx + (k + 1) * w, maxy + 1)
        piece = flat.intersection(strip)
        if not piece.is_empty:
            if piece.geom_type != "Polygon":
                piece = max(getattr(piece, "geoms", [piece]), key=lambda q: q.area)
            parts.append(affinity.rotate(piece, ang, origin=c))
    return parts


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
