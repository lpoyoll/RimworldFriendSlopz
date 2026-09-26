"""Stage B (roads): OS Open Roads centrelines + OSM detail -> road graph JSON for the Houdini road HDA and ZoneGraph.

OS Open Roads gives the network and hierarchy; OSM adds lanes, one-way, speed, width and sidewalks.
Defaults are UK urban norms and are marked as defaults so QA can tell measured from assumed.
"""
from __future__ import annotations

import hashlib
import json
from pathlib import Path

import numpy as np

from .footprints import parse_other_tags
from .geo import stable_id

# road_function -> (hierarchy, lanes total, lane width m, speed mph, pavement both sides)
FUNCTION_DEFAULTS = {
    "Motorway": ("motorway", 6, 3.65, 70, False),
    "A Road": ("a_road", 2, 3.65, 30, True),
    "B Road": ("b_road", 2, 3.3, 30, True),
    "Minor Road": ("minor", 2, 3.0, 30, True),
    "Local Road": ("local", 2, 2.75, 30, True),
    "Local Access Road": ("access", 2, 2.5, 20, True),
    "Restricted Local Access Road": ("access_restricted", 1, 3.0, 10, False),
    "Secondary Access Road": ("service", 1, 3.0, 10, False),
}
PAVEMENT_WIDTH_M = 2.0
KERB_UPSTAND_M = 0.125  # UK standard kerb face
MATCH_BUFFER_M = 8.0

# Field names differ between the OS GML/Shapefile and GeoPackage releases.
FIELD_ALIASES = {
    "id": ("id", "identifier", "gml_id"),
    "function": ("road_function", "function"),
    "form": ("form_of_way", "formOfWay", "formofway"),
    "number": ("road_classification_number", "roadNumber", "roadnumber"),
    "name": ("name_1", "name1"),
    "start": ("start_node", "startNode", "startnode"),
    "end": ("end_node", "endNode", "endnode"),
}

ALTERED_PREFIXES = ["Alder", "Birch", "Carr", "Dean", "Edge", "Fold", "Gorse", "Heys", "Ivy", "Kiln", "Lark", "Moss",
                    "Nook", "Oak", "Pit", "Quarry", "Rake", "Slack", "Tenter", "Weir", "Clough", "Holt", "Brook", "Mill"]


def _field(row, key, default=None):
    for name in FIELD_ALIASES[key]:
        if name in row and row[name] is not None and row[name] == row[name]:
            return row[name]
    return default


def alter_street_name(name: str | None) -> str | None:
    """Deterministic fictional variant for the 'altered' street-name setting: swap the first word, keep the suffix."""
    if not name:
        return name
    parts = name.split()
    if len(parts) < 2:
        return name
    i = int(hashlib.sha1(name.encode()).hexdigest(), 16) % len(ALTERED_PREFIXES)
    new = ALTERED_PREFIXES[i]
    if new == parts[0]:
        new = ALTERED_PREFIXES[(i + 1) % len(ALTERED_PREFIXES)]
    return " ".join([new] + parts[1:])


def _parse_speed(v) -> int | None:
    if not v:
        return None
    s = str(v).strip().lower().replace("mph", "").strip()
    try:
        return int(float(s))
    except ValueError:
        return None


def _parse_float(v) -> float | None:
    try:
        return float(str(v).split()[0])
    except (TypeError, ValueError, IndexError):
        return None


def match_osm(link, osm_lines, tree):
    """Best OSM way for an OS link: the one with most length inside the link's buffer. Returns (index, reversed) or None."""
    buf = link.buffer(MATCH_BUFFER_M, cap_style="flat")
    best, best_len = None, 0.0
    for j in tree.query(buf, predicate="intersects"):
        inside = osm_lines.geometry.iloc[j].intersection(buf)
        if inside.length > best_len:
            best, best_len = j, inside.length
    if best is None or best_len < 0.5 * min(link.length, osm_lines.geometry.iloc[best].length):
        return None
    g = osm_lines.geometry.iloc[best].intersection(buf)
    coords = list(g.coords) if g.geom_type == "LineString" else [c for part in g.geoms for c in part.coords]
    from shapely.geometry import Point

    reversed_ = link.project(Point(coords[-1])) < link.project(Point(coords[0]))
    return best, reversed_


def build_road_graph(os_links, osm_lines=None, heightfield=None, densify_m: float = 5.0) -> dict:
    """Return {'nodes': [...], 'edges': [...]} in BNG metres (z = ODN metres if a DTM is given)."""
    import shapely

    osm = None
    tree = None
    if osm_lines is not None and len(osm_lines):
        osm = osm_lines[osm_lines.geometry.geom_type == "LineString"].reset_index(drop=True)
        if "highway" in osm.columns:
            osm = osm[osm["highway"].notna()].reset_index(drop=True)
        tree = osm.sindex

    nodes: dict[str, dict] = {}
    edges = []

    def node_for(raw_id, xy):
        key = raw_id if raw_id else f"{xy[0]:.1f},{xy[1]:.1f}"
        nid = stable_id("rn", "os_open_roads", key)
        if nid not in nodes:
            nodes[nid] = {"id": nid, "bng": [round(xy[0], 2), round(xy[1], 2)], "degree": 0}
        nodes[nid]["degree"] += 1
        return nid

    for _, row in os_links.iterrows():
        geom = row.geometry
        if geom.geom_type == "MultiLineString":
            geom = shapely.line_merge(geom)
            if geom.geom_type != "LineString":
                continue
        function = _field(row, "function", "Local Road")
        form = _field(row, "form", "Single Carriageway")
        hierarchy, lanes, lane_w, speed, pavements = FUNCTION_DEFAULTS.get(function, FUNCTION_DEFAULTS["Local Road"])
        assumed = {"lanes", "width_m", "speed_mph", "pavements"}
        oneway = None
        if form in ("Dual Carriageway", "Collapsed Dual Carriageway") and hierarchy != "motorway":
            lanes = 4
        if form == "Slip Road":
            lanes, oneway = 1, "forward_unverified"
        if form == "Roundabout":
            oneway = "forward_unverified"  # OS digitisation direction is not guaranteed to follow traffic
        name = _field(row, "name")
        sources = ["os_open_roads"]
        width = None

        if osm is not None:
            m = match_osm(geom, osm, tree)
            if m is not None:
                j, rev = m
                o = osm.iloc[j]
                t = parse_other_tags(o.get("other_tags"))
                sources.append("osm")
                if t.get("lanes", "").isdigit():
                    lanes = int(t["lanes"]); assumed.discard("lanes")
                ow = t.get("oneway", o.get("oneway") if "oneway" in o else None)
                if ow in ("yes", "true", "1"):
                    oneway = "backward" if rev else "forward"
                elif ow == "-1":
                    oneway = "forward" if rev else "backward"
                elif ow == "no" and oneway and oneway.endswith("unverified"):
                    oneway = None
                sp = _parse_speed(t.get("maxspeed"))
                if sp:
                    speed = sp; assumed.discard("speed_mph")
                sw = t.get("sidewalk")
                if sw:
                    pavements = sw != "no"; assumed.discard("pavements")
                if not name and isinstance(o.get("name"), str):
                    name = o.get("name")
                width = _parse_float(t.get("width"))
        if width:
            assumed.discard("width_m")
        else:
            width = round(lanes * lane_w + (1.0 if lanes >= 4 else 0.0), 2)  # +central reservation for duals

        dense = geom.segmentize(densify_m) if densify_m else geom
        pts = np.asarray(dense.coords)[:, :2]
        z = heightfield.sample(pts[:, 0], pts[:, 1]) if heightfield is not None else np.full(len(pts), np.nan)
        polyline = [[round(float(x), 2), round(float(y), 2)] + ([round(float(h), 2)] if h == h else []) for (x, y), h in zip(pts, z)]

        start = node_for(_field(row, "start"), pts[0])
        end = node_for(_field(row, "end"), pts[-1])
        edges.append({
            "id": stable_id("re", "os_open_roads", _field(row, "id", f"{pts[0][0]:.1f},{pts[0][1]:.1f},{pts[-1][0]:.1f},{pts[-1][1]:.1f}")),
            "from": start, "to": end,
            "hierarchy": hierarchy, "function": function, "form_of_way": form,
            "road_number": _field(row, "number"),
            "name": name, "name_altered": alter_street_name(name),
            "lanes": int(lanes), "width_m": width, "oneway": oneway, "speed_mph": int(speed),
            "pavements": bool(pavements), "pavement_width_m": PAVEMENT_WIDTH_M if pavements else 0.0,
            "kerb_upstand_m": KERB_UPSTAND_M if pavements else 0.0,
            "length_m": round(geom.length, 2),
            "assumed": sorted(assumed),
            "sources": sources,
            "polyline": polyline,
        })

    for n in nodes.values():
        n["kind"] = "dead_end" if n["degree"] == 1 else ("junction" if n["degree"] >= 3 else "through")
    return {"crs": "EPSG:27700", "nodes": sorted(nodes.values(), key=lambda n: n["id"]), "edges": edges}


def write_road_graph(graph: dict, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(graph, indent=1), encoding="utf-8")
