"""Stage D part 1: find which street-level images see which facade (Mapillary; images are never shipped).

For every street-facing footprint edge we pick the images that see it best:
  * camera 4-40 m from the facade midpoint, in front of the facade (outward normal faces the camera)
  * facade midpoint inside the camera's horizontal field of view (panoramas see everything)
  * viewing angle to the facade normal under 78 deg (Ashton imagery is forward-facing dashcam)
  * line of sight not blocked by another building
Score favours close, square-on views. Output: one record per facade edge with its best images.

The Mapillary token is read from the MAPILLARY_TOKEN environment variable (never from files in the repo).
"""
from __future__ import annotations

import json
import math
import os
import time
import urllib.parse
import urllib.request
from pathlib import Path

import numpy as np

GRAPH = "https://graph.mapillary.com/images"
FIELDS = "id,captured_at,compass_angle,computed_compass_angle,is_pano,camera_type,sequence,geometry,computed_geometry,height,width"
CELL_M = 250.0
MIN_DIST_M, MAX_DIST_M = 4.0, 40.0
MAX_OBLIQUE_DEG = 78.0  # Ashton imagery is forward dashcam: facades are seen obliquely
DEFAULT_HFOV_DEG = 100.0  # 16:9 action cameras (3840x2160) dominate Ashton coverage
STREET_EDGE_M = 20.0  # edge counts as street-facing if a road is within this distance in front of it
MIN_EDGE_M = 2.5


def token() -> str:
    t = os.environ.get("MAPILLARY_TOKEN", "")
    if not t:
        raise SystemExit("Set MAPILLARY_TOKEN (see docs/11_streetview.md). It must never be committed.")
    return t


def fetch_metadata(bounds, cache: Path, tok: str | None = None) -> list[dict]:
    """All image metadata in BNG bounds, fetched in 250 m cells (API limit), cached as JSON."""
    from pyproj import Transformer

    cache.mkdir(parents=True, exist_ok=True)
    out_file = cache / "images.json"
    if out_file.exists():
        return json.loads(out_file.read_text())
    tok = tok or token()
    to_ll = Transformer.from_crs(27700, 4326, always_xy=True)
    to_bng = Transformer.from_crs(4326, 27700, always_xy=True)
    min_e, min_n, max_e, max_n = bounds
    seen, images = set(), []
    for e0 in np.arange(min_e, max_e, CELL_M):
        for n0 in np.arange(min_n, max_n, CELL_M):
            a, b = to_ll.transform(e0, n0), to_ll.transform(e0 + CELL_M, n0 + CELL_M)
            q = urllib.parse.urlencode({"access_token": tok, "fields": FIELDS, "limit": 2000,
                                        "bbox": f"{a[0]:.6f},{a[1]:.6f},{b[0]:.6f},{b[1]:.6f}"})
            for attempt in range(4):
                try:
                    data = json.load(urllib.request.urlopen(f"{GRAPH}?{q}", timeout=90))["data"]
                    break
                except Exception:
                    time.sleep(2 ** attempt)
            else:
                data = []
            for d in data:
                if d["id"] in seen:
                    continue
                seen.add(d["id"])
                geom = d.get("computed_geometry") or d["geometry"]
                lon, lat = geom["coordinates"]
                e, n = to_bng.transform(lon, lat)
                images.append({
                    "id": d["id"], "e": round(e, 2), "n": round(n, 2),
                    "heading": d.get("computed_compass_angle", d.get("compass_angle")),
                    "pano": bool(d.get("is_pano")) or d.get("camera_type") in ("spherical", "equirectangular"),
                    "captured_at": d.get("captured_at"), "sequence": d.get("sequence"),
                    "width": d.get("width"), "height": d.get("height"),
                })
    out_file.write_text(json.dumps(images))
    return images


def facade_edges(buildings: list[dict], road_lines) -> list[dict]:
    """Street-facing edges of every footprint: id, edge index, endpoints, midpoint, outward normal, length."""
    import shapely
    from shapely.geometry import Polygon

    roads = shapely.union_all(road_lines) if road_lines else None
    edges = []
    for b in buildings:
        poly = shapely.geometry.polygon.orient(Polygon(b["footprint"]["outer"]), 1.0)  # CCW: outward normal is (dy, -dx)
        pts = list(poly.exterior.coords)
        for i, ((x1, y1), (x2, y2)) in enumerate(zip(pts[:-1], pts[1:])):
            L = math.hypot(x2 - x1, y2 - y1)
            if L < MIN_EDGE_M:
                continue
            nx, ny = (y2 - y1) / L, -(x2 - x1) / L
            mx, my = (x1 + x2) / 2, (y1 + y2) / 2
            street = True
            if roads is not None:
                probe = shapely.geometry.LineString([(mx + nx * 0.5, my + ny * 0.5), (mx + nx * STREET_EDGE_M, my + ny * STREET_EDGE_M)])
                street = probe.intersects(roads)
            if street:
                edges.append({"footprint_id": b["footprint_id"], "edge_index": i, "a": (x1, y1), "b": (x2, y2),
                              "mid": (mx, my), "normal": (nx, ny), "length": L})
    return edges


def match_views(edges: list[dict], images: list[dict], obstacles, top_k: int = 3, hfov_deg: float = DEFAULT_HFOV_DEG) -> list[dict]:
    """Best images per facade edge. `obstacles` is a list of footprint polygons (with .footprint_id attached via index)."""
    import shapely
    from shapely.geometry import LineString
    from shapely.strtree import STRtree

    if not images:
        return [{**_edge_out(e), "views": []} for e in edges]
    cam = np.array([[im["e"], im["n"]] for im in images])
    cam_tree = STRtree(shapely.points(cam))
    obs_tree = STRtree(obstacles["geoms"]) if obstacles["geoms"] else None
    out = []
    for e in edges:
        mx, my = e["mid"]
        nx, ny = e["normal"]
        cand = cam_tree.query(shapely.Point(mx, my).buffer(MAX_DIST_M))
        views = []
        for j in cand:
            im = images[j]
            dx, dy = im["e"] - mx, im["n"] - my
            d = math.hypot(dx, dy)
            if not (MIN_DIST_M <= d <= MAX_DIST_M):
                continue
            cos_view = (dx * nx + dy * ny) / d  # >0: camera is in front of the facade
            if cos_view <= math.cos(math.radians(MAX_OBLIQUE_DEG)):
                continue
            if not im["pano"]:
                if im["heading"] is None:
                    continue
                bearing_to_facade = math.degrees(math.atan2(-dx, -dy)) % 360
                off = abs((bearing_to_facade - im["heading"] + 180) % 360 - 180)
                if off > hfov_deg / 2:
                    continue
            if obs_tree is not None:
                sight = LineString([(im["e"], im["n"]), (mx + nx * 0.3, my + ny * 0.3)])
                blocked = False
                for k in obs_tree.query(sight, predicate="intersects"):
                    if obstacles["ids"][k] != e["footprint_id"]:
                        blocked = True
                        break
                if blocked:
                    continue
            # close + square-on is best; slight bonus for recent imagery
            score = cos_view * (1.0 - 0.6 * (d - MIN_DIST_M) / (MAX_DIST_M - MIN_DIST_M))
            views.append({"image_id": im["id"], "distance_m": round(d, 1), "cos_view": round(cos_view, 3),
                          "score": round(score, 3), "pano": im["pano"], "captured_at": im.get("captured_at")})
        views.sort(key=lambda v: (-v["score"], -(v["captured_at"] or 0)))
        out.append({**_edge_out(e), "views": views[:top_k]})
    return out


def _edge_out(e: dict) -> dict:
    return {"footprint_id": e["footprint_id"], "edge_index": e["edge_index"], "length_m": round(e["length"], 2),
            "mid": [round(e["mid"][0], 2), round(e["mid"][1], 2)], "normal": [round(e["normal"][0], 3), round(e["normal"][1], 3)]}
