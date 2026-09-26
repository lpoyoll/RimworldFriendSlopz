"""Shared helpers for vector stages: zone bounds, stable IDs, vector reading, height sampling."""
from __future__ import annotations

import hashlib
from pathlib import Path

import numpy as np

from .config import PipelineConfig, Zone
from .terrain import Raster, mosaic, read_raster

CRS = "EPSG:27700"


def zone_bounds(cfg: PipelineConfig, zone: Zone, margin_m: float = 0.0) -> tuple[float, float, float, float]:
    """(min_e, min_n, max_e, max_n) of the zone's cell grid, optionally grown by a margin."""
    size_e = zone.tiles_x * cfg.landscape.tile_quads * cfg.landscape.resolution_m + cfg.landscape.resolution_m
    size_n = zone.tiles_y * cfg.landscape.tile_quads * cfg.landscape.resolution_m + cfg.landscape.resolution_m
    return (zone.min_e - margin_m, zone.max_n - size_n - margin_m, zone.min_e + size_e + margin_m, zone.max_n + margin_m)


def stable_id(prefix: str, *parts: object) -> str:
    """prefix_ + 16 hex chars from a SHA-1 of the parts. Same input, same ID, every run."""
    h = hashlib.sha1("|".join(str(p) for p in parts).encode("utf-8")).hexdigest()[:16]
    return f"{prefix}_{h}"


def read_vectors(paths, bounds, layer_for):
    """Read and concatenate several files (e.g. one per OS 100 km square). layer_for(path) picks the layer."""
    import geopandas as gpd
    import pandas as pd

    parts = [read_vector(p, bounds, layer_for(p)) for p in paths]
    parts = [p for p in parts if len(p)]
    if not parts:
        return gpd.GeoDataFrame(geometry=[], crs=CRS)
    return gpd.GeoDataFrame(pd.concat(parts, ignore_index=True), geometry="geometry", crs=CRS)


def read_vector(path: str | Path, bounds: tuple[float, float, float, float], layer: str | None = None):
    """Read a vector file (GPKG, GeoJSON, SHP, OSM PBF) clipped to BNG bounds, reprojected to EPSG:27700."""
    import geopandas as gpd
    from shapely.geometry import box

    kwargs = {"layer": layer} if layer else {}
    probe = gpd.read_file(path, rows=0, **kwargs)
    src_crs = probe.crs or CRS
    bbox = gpd.GeoSeries([box(*bounds)], crs=CRS).to_crs(src_crs).total_bounds
    gdf = gpd.read_file(path, bbox=tuple(bbox), **kwargs)
    if gdf.crs is None:
        gdf = gdf.set_crs(src_crs)
    gdf = gdf.to_crs(CRS)
    return gdf[gdf.geometry.notna() & ~gdf.geometry.is_empty].reset_index(drop=True)


class Heightfield:
    """A 1 m raster (DTM or DSM) covering some bounds, with point and area sampling."""

    def __init__(self, raster: Raster):
        self.r = raster

    @classmethod
    def from_files(cls, paths: list[Path], bounds: tuple[float, float, float, float], res: float = 1.0) -> "Heightfield":
        min_e, min_n, max_e, max_n = (np.floor(bounds[0]), np.floor(bounds[1]), np.ceil(bounds[2]), np.ceil(bounds[3]))
        rows, cols = int((max_n - min_n) / res), int((max_e - min_e) / res)
        data = mosaic([read_raster(Path(p)) for p in paths], float(min_e), float(max_n), rows, cols, res)
        return cls(Raster(data, float(min_e), float(max_n), res))

    def _rc(self, e, n):
        return (self.r.max_n - np.asarray(n)) / self.r.cellsize - 0.5, (np.asarray(e) - self.r.min_e) / self.r.cellsize - 0.5

    def sample(self, e, n) -> np.ndarray:
        """Bilinear height at points (NaN outside or on nodata)."""
        rr, cc = self._rc(e, n)
        rr, cc = np.atleast_1d(rr).astype(float), np.atleast_1d(cc).astype(float)
        h, w = self.r.data.shape
        r0, c0 = np.floor(rr).astype(int), np.floor(cc).astype(int)
        fr, fc = rr - r0, cc - c0
        out = np.full(rr.shape, np.nan)
        ok = (r0 >= 0) & (c0 >= 0) & (r0 + 1 < h) & (c0 + 1 < w)
        # clamp edge points that sit exactly on the last cell centre
        edge = (~ok) & (r0 >= 0) & (c0 >= 0) & (r0 < h) & (c0 < w)
        d = self.r.data
        a, b = r0[ok], c0[ok]
        out[ok] = (d[a, b] * (1 - fr[ok]) * (1 - fc[ok]) + d[a, b + 1] * (1 - fr[ok]) * fc[ok]
                   + d[a + 1, b] * fr[ok] * (1 - fc[ok]) + d[a + 1, b + 1] * fr[ok] * fc[ok])
        out[edge] = d[r0[edge], c0[edge]]
        return out

    def cells_in(self, geom):
        """(rows, cols, e, n) of cell centres inside a shapely geometry."""
        import shapely

        min_e, min_n, max_e, max_n = geom.bounds
        cs = self.r.cellsize
        c_lo = max(int(np.floor((min_e - self.r.min_e) / cs)), 0)
        c_hi = min(int(np.ceil((max_e - self.r.min_e) / cs)), self.r.data.shape[1])
        r_lo = max(int(np.floor((self.r.max_n - max_n) / cs)), 0)
        r_hi = min(int(np.ceil((self.r.max_n - min_n) / cs)), self.r.data.shape[0])
        if c_lo >= c_hi or r_lo >= r_hi:
            return (np.empty(0, int),) * 2 + (np.empty(0),) * 2
        rr, cc = np.mgrid[r_lo:r_hi, c_lo:c_hi]
        e = self.r.min_e + (cc + 0.5) * cs
        n = self.r.max_n - (rr + 0.5) * cs
        inside = shapely.contains_xy(geom, e, n)
        return rr[inside], cc[inside], e[inside], n[inside]
