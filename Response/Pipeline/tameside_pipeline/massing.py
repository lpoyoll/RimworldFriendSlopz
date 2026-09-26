"""Stage C: building heights and roof type from EA LiDAR (DSM - DTM), plus a first archetype guess.

Heights
  ground_z      median DTM inside the footprint (EA DTM is interpolated under buildings)
  ground_z_min  5th percentile DTM in a 0.5-2 m ring outside the footprint (lowest doorstep, for sloping streets)
  ridge         95th percentile of nDSM inside the footprint eroded by 0.5 m (avoids wall-edge pixels)
  eaves         25th percentile, over the 1.5 m band just inside the eroded edge, of each cell's nDSM projected
                out to the wall along its own slope (nDSM - slope * distance to wall)

Roof type (raster method; the LAZ point-cloud RANSAC upgrade is in docs/02)
  Downhill directions of pitched DSM cells (> 12 deg) are reduced to one axis with the double-angle mean.
  Slopes falling across that axis on both sides = gable; plus slopes along it = hip; one side only = mono-pitch.
"""
from __future__ import annotations

import math

import numpy as np

from .geo import Heightfield

STOREY_M = 2.7
GROUND_FLOOR_OFFSET_M = 0.3
PITCH_MIN_DEG = 12.0
DEFAULT_EAVES_M = 5.5


def _window_gradient(dsm: Heightfield, rr: np.ndarray, cc: np.ndarray):
    """dz/de and dz/dn at the given cells, from a local window of the DSM."""
    r0, r1 = max(rr.min() - 1, 0), min(rr.max() + 2, dsm.r.data.shape[0])
    c0, c1 = max(cc.min() - 1, 0), min(cc.max() + 2, dsm.r.data.shape[1])
    win = dsm.r.data[r0:r1, c0:c1]
    if min(win.shape) < 2:
        return np.zeros(len(rr)), np.zeros(len(rr))
    g_row, g_col = np.gradient(win, dsm.r.cellsize)
    return g_col[rr - r0, cc - c0], -g_row[rr - r0, cc - c0]  # rows run south, so dz/dn = -dz/drow


def classify_roof(dz_de: np.ndarray, dz_dn: np.ndarray) -> dict:
    """Roof type, ridge bearing (deg from north, 0-180) and confidence from surface gradients."""
    ok = ~(np.isnan(dz_de) | np.isnan(dz_dn))
    dz_de, dz_dn = dz_de[ok], dz_dn[ok]
    n = len(dz_de)
    if n < 4:
        return {"type": "complex", "confidence": 0.0}
    slope = np.degrees(np.arctan(np.hypot(dz_de, dz_dn)))
    pitched = slope > PITCH_MIN_DEG
    frac = pitched.mean()
    size_conf = min(1.0, n / 20.0)
    if frac < 0.3:
        return {"type": "flat", "confidence": round(float((1 - frac) * size_conf), 2)}

    # Downhill bearing of each pitched cell, clockwise from north.
    b = np.arctan2(-dz_de[pitched], -dz_dn[pitched])
    # Axis of fall via double-angle mean (opposite slopes reinforce instead of cancelling).
    fall_axis = 0.5 * math.atan2(np.sin(2 * b).mean(), np.cos(2 * b).mean())
    rel = np.angle(np.exp(1j * (b - fall_axis)))  # -pi..pi relative to the fall axis
    across_pos = (np.abs(rel) < math.pi / 4).mean()
    across_neg = (np.abs(rel) > 3 * math.pi / 4).mean()
    along = 1.0 - across_pos - across_neg
    ridge_bearing = (math.degrees(fall_axis) + 90.0) % 180.0

    if across_pos > 0.8 or across_neg > 0.8:
        t, dom = "mono_pitch", max(across_pos, across_neg)
    elif min(across_pos, across_neg) > 0.2 and along < 0.15:
        t, dom = "gable", across_pos + across_neg
    elif min(across_pos, across_neg) > 0.15 and along >= 0.15:
        t, dom = "hip", 1.0 - abs(across_pos - across_neg)
    else:
        t, dom = "complex", 0.5
    return {"type": t, "ridge_bearing_deg": round(ridge_bearing, 1), "confidence": round(float(dom * size_conf * frac), 2)}


def compute_massing(geom, dtm: Heightfield, dsm: Heightfield) -> tuple[dict, list[str]]:
    """Massing block for one footprint (building_facade.schema.json 'massing') plus QA flags."""
    flags: list[str] = []
    _, _, e_all, n_all = dtm.cells_in(geom)
    if len(e_all) == 0:  # very thin footprint: fall back to its centroid
        c = geom.representative_point()
        e_all, n_all = np.array([c.x]), np.array([c.y])
    ground = dtm.sample(e_all, n_all)
    ground_z = float(np.nanmedian(ground)) if np.isfinite(ground).any() else float("nan")

    ring = geom.buffer(2.0).difference(geom.buffer(0.5))
    _, _, re_, rn_ = dtm.cells_in(ring)
    ring_z = dtm.sample(re_, rn_) if len(re_) else np.array([])
    ground_min = float(np.nanpercentile(ring_z, 5)) if np.isfinite(ring_z).any() else ground_z

    inner = geom.buffer(-0.5)
    if inner.is_empty or inner.area < 2.0:
        inner = geom
    rr, cc, e, n = dsm.cells_in(inner)
    ndsm = dsm.sample(e, n) - dtm.sample(e, n) if len(e) else np.array([])
    valid = np.isfinite(ndsm) & (ndsm > 1.0)  # ignore cells at ground level (yards, gaps)

    if valid.sum() < 3:
        flags.append("no_lidar_height")
        massing = {"ground_z": round(ground_z, 2), "ground_z_min": round(ground_min, 2),
                   "eaves_height_m": DEFAULT_EAVES_M, "ridge_height_m": DEFAULT_EAVES_M, "storeys": 2,
                   "roof": {"type": "complex", "confidence": 0.0}}
        return massing, flags

    ridge = float(np.percentile(ndsm[valid], 95))
    core = inner.buffer(-1.5)
    in_band = np.ones(len(e), dtype=bool)
    if not core.is_empty:
        import shapely

        in_band = ~shapely.contains_xy(core, e, n)
    gx, gy = _window_gradient(dsm, rr[valid], cc[valid])
    roof = classify_roof(gx, gy)

    import shapely

    band = in_band[valid]
    if band.sum() < 3:
        band = np.ones(valid.sum(), dtype=bool)
    ev, nv = e[valid][band], n[valid][band]
    dist = shapely.distance(geom.exterior, shapely.points(ev, nv))
    grad = np.nan_to_num(np.hypot(gx[band], gy[band]))
    at_wall = ndsm[valid][band] - grad * dist
    eaves = float(np.percentile(at_wall, 25))
    if roof["type"] == "flat":
        eaves = ridge  # parapet/flat roof: top of wall is the roof line
    eaves = min(eaves, ridge)
    storeys = max(1, int(round((eaves - GROUND_FLOOR_OFFSET_M) / STOREY_M)))

    if roof["confidence"] < 0.5:
        flags.append("low_roof_confidence")
    if ridge > 60:
        flags.append("height_outlier")
    if ridge - eaves > 8:
        flags.append("tall_roof_check_mansard_or_tower")

    massing = {"ground_z": round(ground_z, 2), "ground_z_min": round(ground_min, 2),
               "eaves_height_m": round(eaves, 2), "ridge_height_m": round(ridge, 2), "storeys": storeys,
               "roof": {**roof, "material": "unknown"}}
    return massing, flags


RETAIL_TAGS = {"retail", "commercial", "supermarket", "kiosk"}
INDUSTRIAL_TAGS = {"industrial", "warehouse", "manufacture"}
CIVIC_TAGS = {"civic", "public", "government", "townhall", "hospital", "school", "college", "university", "fire_station", "police"}


def guess_archetype(area_m2: float, massing: dict, shared_walls: int, osm: dict) -> dict:
    """First-pass archetype from geometry + OSM tags. Stage D (facades) and QA refine it."""
    b = (osm or {}).get("building", "")
    storeys = massing["storeys"]
    roof = massing["roof"]["type"]
    height = massing.get("ridge_height_m") or massing["eaves_height_m"]
    basis = []

    def out(i, conf, why):
        basis.append(why)
        return {"id": i, "confidence": conf, "basis": basis}

    if b in ("church", "chapel", "cathedral") or osm.get("amenity") == "place_of_worship":
        return out("church", 0.85, "osm:place_of_worship")
    if b in CIVIC_TAGS or osm.get("amenity") in CIVIC_TAGS:
        return out("civic", 0.7, "osm:civic")
    if height > 22 and roof == "flat":
        return out("council_highrise", 0.7, "tall_flat")
    if b in INDUSTRIAL_TAGS or (area_m2 > 800 and storeys <= 2 and roof in ("flat", "complex")):
        if area_m2 > 1500 and storeys >= 3:
            return out("mill_brick", 0.55, "large_multistorey_industrial")
        return out("industrial_shed", 0.6, "large_low_or_osm_industrial")
    if area_m2 > 1500 and storeys >= 3:
        return out("mill_brick", 0.5, "large_multistorey")
    if b in RETAIL_TAGS or osm.get("shop"):
        if area_m2 > 600:
            return out("retail_modern", 0.65, "osm:retail_large")
        return out("shop_terrace", 0.65, "osm:shop")
    if roof == "flat" and 3 <= storeys <= 5 and area_m2 > 200:
        return out("council_1960s", 0.5, "flat_roof_midrise")
    if area_m2 < 250:
        if shared_walls >= 2:
            return out("terrace_redbrick", 0.6, "two_shared_walls")
        if shared_walls == 1:
            return out("semi_detached", 0.55, "one_shared_wall")
        if area_m2 < 25:
            return out("other", 0.4, "small_outbuilding")
        return out("detached", 0.5, "no_shared_walls")
    return out("other", 0.3, "no_rule_matched")


def build_records(footprints, dtm: Heightfield, dsm: Heightfield, zone: str, shared_walls: list[int]) -> list[dict]:
    """One building_facade.schema.json record per footprint (facades empty until Stage D)."""
    records = []
    for i, row in footprints.reset_index(drop=True).iterrows():
        g = row.geometry
        massing, flags = compute_massing(g, dtm, dsm)
        osm = row.get("osm") if isinstance(row.get("osm"), dict) else {}
        arch = guess_archetype(g.area, massing, shared_walls[i], osm)
        if arch["confidence"] < 0.5:
            flags.append("low_archetype_confidence")
        sources = list(row["sources"]) + ["ea_lidar_dtm_1m", "ea_lidar_dsm_1m"]
        rec = {
            "footprint_id": row["footprint_id"],
            "zone": zone,
            "sources": sources,
            "footprint": {"crs": "EPSG:27700", "outer": [[round(x, 2), round(y, 2)] for x, y in list(g.exterior.coords)[:-1]]},
            "massing": massing,
            "archetype": arch,
            "facades": [],
            "qa": {"reviewed": False, "flags": flags},
        }
        if row.get("plot_id"):
            rec["plot_id"] = row["plot_id"]
        if osm:
            rec["osm"] = osm
        records.append(rec)
    return records
