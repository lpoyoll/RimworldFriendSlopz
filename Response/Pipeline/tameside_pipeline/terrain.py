"""Stage A: Environment Agency LiDAR DTM -> UE5 Landscape heightmap tiles.

Grid convention: the zone is a grid of 1 m cells whose top-left *edge* is (min_e, max_n).
Each landscape vertex sits at a cell centre, so vertex (row, col) is at
    E = min_e + (col + 0.5) * res,   N = max_n - (row + 0.5) * res
which lines up with EA tiles (edges on whole kilometres, centres on .5 m).
Row 0 is the northern edge, which is also UE's minimum Y, so no flip is needed.
"""
from __future__ import annotations

import json
import struct
import zlib
from dataclasses import dataclass
from pathlib import Path

import numpy as np

from .config import PipelineConfig, Zone
from .coords import WorldOrigin, height_range_m, height_to_u16

SOURCE_ID = "ea_lidar_dtm_1m"


@dataclass
class Raster:
    data: np.ndarray  # float64, NaN = nodata, row 0 = north
    min_e: float  # west edge
    max_n: float  # north edge
    cellsize: float


# ---------------------------------------------------------------- input

def read_asc(path: str | Path) -> Raster:
    """Read an ESRI ASCII grid (the EA download format)."""
    header: dict[str, float] = {}
    with open(path, "r", encoding="ascii") as f:
        while True:
            pos = f.tell()
            line = f.readline()
            parts = line.split()
            if len(parts) == 2 and parts[0][0].isalpha():
                header[parts[0].lower()] = float(parts[1])
            else:
                f.seek(pos)
                break
        ncols, nrows = int(header["ncols"]), int(header["nrows"])
        data = np.loadtxt(f, dtype=np.float64, ndmin=2)
    if data.shape != (nrows, ncols):
        raise ValueError(f"{path}: expected {nrows}x{ncols}, got {data.shape}")
    cs = header["cellsize"]
    if "xllcenter" in header:
        xll, yll = header["xllcenter"] - cs / 2, header["yllcenter"] - cs / 2
    else:
        xll, yll = header["xllcorner"], header["yllcorner"]
    nodata = header.get("nodata_value")
    if nodata is not None:
        data[data == nodata] = np.nan
    return Raster(data, xll, yll + nrows * cs, cs)


def read_geotiff(path: str | Path) -> Raster:
    import rasterio  # optional dependency

    with rasterio.open(path) as ds:
        if ds.crs is not None and ds.crs.to_epsg() != 27700:
            raise ValueError(f"{path}: CRS {ds.crs} is not EPSG:27700; reproject first.")
        data = ds.read(1).astype(np.float64)
        if ds.nodata is not None:
            data[data == ds.nodata] = np.nan
        t = ds.transform
        if t.b != 0 or t.d != 0 or abs(t.a) != abs(t.e):
            raise ValueError(f"{path}: rotated or non-square pixels are not supported.")
        return Raster(data, t.c, t.f, t.a)


def read_raster(path: Path) -> Raster:
    suffix = path.suffix.lower()
    if suffix == ".asc":
        return read_asc(path)
    if suffix in (".tif", ".tiff"):
        return read_geotiff(path)
    raise ValueError(f"Unsupported raster format: {path}")


# ---------------------------------------------------------------- mosaic

def zone_shape(zone: Zone, tile_quads: int) -> tuple[int, int]:
    """(rows, cols) of vertices for a zone."""
    return zone.tiles_y * tile_quads + 1, zone.tiles_x * tile_quads + 1


def mosaic(rasters: list[Raster], min_e: float, max_n: float, rows: int, cols: int, res: float) -> np.ndarray:
    """Paste source rasters into the zone grid. Cells with no data stay NaN."""
    out = np.full((rows, cols), np.nan, dtype=np.float64)
    for r in rasters:
        if not np.isclose(r.cellsize, res):
            raise ValueError(f"Source cellsize {r.cellsize} != target {res}; resample first.")
        col_off = (r.min_e - min_e) / res
        row_off = (max_n - r.max_n) / res
        if not (np.isclose(col_off, round(col_off)) and np.isclose(row_off, round(row_off))):
            raise ValueError("Source grid is not aligned to the zone grid.")
        c0, r0 = int(round(col_off)), int(round(row_off))
        h, w = r.data.shape
        # intersect
        dr0, dc0 = max(r0, 0), max(c0, 0)
        dr1, dc1 = min(r0 + h, rows), min(c0 + w, cols)
        if dr0 >= dr1 or dc0 >= dc1:
            continue
        src = r.data[dr0 - r0 : dr1 - r0, dc0 - c0 : dc1 - c0]
        dst = out[dr0:dr1, dc0:dc1]
        take = ~np.isnan(src)
        dst[take] = src[take]
    return out


def fill_nodata(a: np.ndarray, max_iter: int = 2000) -> tuple[np.ndarray, int]:
    """Fill NaNs by repeated averaging of valid 4-neighbours (grows inward from hole edges).

    Returns the filled array and how many cells were filled.
    """
    a = a.copy()
    missing = np.isnan(a)
    n_missing = int(missing.sum())
    if n_missing == 0:
        return a, 0
    if n_missing == a.size:
        raise ValueError("No valid height data in zone; check inputs cover the zone.")
    for _ in range(max_iter):
        nan = np.isnan(a)
        if not nan.any():
            break
        v = np.where(nan, 0.0, a)
        w = (~nan).astype(np.float64)
        s = np.zeros_like(a)
        c = np.zeros_like(a)
        s[1:, :] += v[:-1, :]; c[1:, :] += w[:-1, :]
        s[:-1, :] += v[1:, :]; c[:-1, :] += w[1:, :]
        s[:, 1:] += v[:, :-1]; c[:, 1:] += w[:, :-1]
        s[:, :-1] += v[:, 1:]; c[:, :-1] += w[:, 1:]
        fillable = nan & (c > 0)
        a[fillable] = s[fillable] / c[fillable]
    else:
        a[np.isnan(a)] = np.nanmean(a)
    return a, n_missing


# ---------------------------------------------------------------- output

def write_png16(path: str | Path, img: np.ndarray) -> None:
    """Write a 16-bit grayscale PNG with no third-party dependencies."""
    if img.dtype != np.uint16 or img.ndim != 2:
        raise ValueError("expected 2D uint16 array")
    h, w = img.shape
    raw = np.empty((h, 1 + w * 2), dtype=np.uint8)
    raw[:, 0] = 0  # filter: none
    raw[:, 1:] = img.astype(">u2").view(np.uint8).reshape(h, w * 2)

    def chunk(tag: bytes, payload: bytes) -> bytes:
        return struct.pack(">I", len(payload)) + tag + payload + struct.pack(">I", zlib.crc32(tag + payload) & 0xFFFFFFFF)

    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n")
        f.write(chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 16, 0, 0, 0, 0)))
        f.write(chunk(b"IDAT", zlib.compress(raw.tobytes(), 6)))
        f.write(chunk(b"IEND", b""))


def read_png16(path: str | Path) -> np.ndarray:
    """Minimal reader for PNGs written by write_png16 (used in tests and QA)."""
    data = Path(path).read_bytes()
    pos, idat, w, h = 8, b"", 0, 0
    while pos < len(data):
        (length,) = struct.unpack(">I", data[pos : pos + 4])
        tag = data[pos + 4 : pos + 8]
        payload = data[pos + 8 : pos + 8 + length]
        if tag == b"IHDR":
            w, h = struct.unpack(">II", payload[:8])
        elif tag == b"IDAT":
            idat += payload
        pos += 12 + length
    raw = np.frombuffer(zlib.decompress(idat), dtype=np.uint8).reshape(h, 1 + w * 2)
    return raw[:, 1:].copy().view(">u2").reshape(h, w).astype(np.uint16)


def build_terrain(cfg: PipelineConfig, zone_name: str, inputs: list[Path], out_dir: Path) -> dict:
    """Run Stage A for one zone. Returns the manifest (also written to out_dir)."""
    src = cfg.require_source(SOURCE_ID)
    zone = cfg.zone(zone_name)
    ls = cfg.landscape
    q, res = ls.tile_quads, ls.resolution_m
    rows, cols = zone_shape(zone, q)

    rasters = [read_raster(p) for p in inputs]
    heights = mosaic(rasters, zone.min_e, zone.max_n, rows, cols, res)
    heights, filled = fill_nodata(heights)

    lo, hi = height_range_m(ls.z_scale, ls.location_z_cm)
    hmin, hmax = float(heights.min()), float(heights.max())
    if hmin < lo or hmax > hi:
        raise ValueError(f"Heights {hmin:.2f}..{hmax:.2f} m exceed landscape range {lo:.2f}..{hi:.2f} m; change z_scale.")
    u16 = height_to_u16(heights, ls.z_scale, ls.location_z_cm)

    out_dir.mkdir(parents=True, exist_ok=True)
    origin = WorldOrigin(cfg.origin_e, cfg.origin_n)
    first_e, first_n = zone.min_e + res / 2, zone.max_n - res / 2

    full_name = f"{zone_name}_full.png"
    write_png16(out_dir / full_name, u16)

    tiles = []
    for ty in range(zone.tiles_y):
        for tx in range(zone.tiles_x):
            r0, c0 = ty * q, tx * q
            name = f"{zone_name}_x{tx}_y{ty}.png"
            write_png16(out_dir / name, u16[r0 : r0 + q + 1, c0 : c0 + q + 1])
            tiles.append({
                "file": name, "x": tx, "y": ty,
                "bng_nw_vertex": [first_e + c0 * res, first_n - r0 * res],
                "vertices": q + 1,
            })

    loc = origin.bng_to_ue(first_e, first_n)
    manifest = {
        "zone": zone_name,
        "crs": "EPSG:27700",
        "resolution_m": res,
        "vertices": [cols, rows],
        "bng_extent_vertices": {"min_e": first_e, "max_e": first_e + (cols - 1) * res,
                                "min_n": first_n - (rows - 1) * res, "max_n": first_n},
        "world_origin_bng": [cfg.origin_e, cfg.origin_n],
        "ue_landscape": {
            "location_cm": [loc[0], loc[1], ls.location_z_cm],
            "scale": [res * 100.0, res * 100.0, ls.z_scale],
            "tile_quads": q,
            "full_heightmap": full_name,
            "note": "Tiles share their edge row/column with neighbours. Verify tiled import in-engine (see docs/06).",
        },
        "height_m": {"min": hmin, "max": hmax, "representable": [lo, hi]},
        "filled_nodata_cells": filled,
        "tiles": tiles,
        "inputs": [str(p.name) for p in inputs],
        "attribution": [src.get("attribution", "")],
    }
    (out_dir / "terrain_manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    return manifest
