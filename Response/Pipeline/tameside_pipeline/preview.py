"""3D preview of pipeline output (terrain, buildings, roads, LiDAR trees) as a single HTML page (three.js).

This is a QA view of what the pipeline generated, not the game render. It lets us eyeball a location in a browser
without Unreal. Roofs are built from the massing record (eaves, ridge, roof type, ridge bearing) over the footprint's
ridge-aligned bounding rectangle, so L-shaped buildings get a simplified roof.
"""
from __future__ import annotations

import json
import math
from pathlib import Path

import numpy as np

from .geo import Heightfield

ROAD_LIFT_M = 0.05
OVERHANG_M = 0.3

WALL_COLOURS = {
    "terrace_redbrick": "#8b4a3a", "semi_detached": "#a0674f", "detached": "#b07a5e", "council_1960s": "#9c8f80",
    "council_highrise": "#a9a59c", "church": "#7d6f5f", "civic": "#8f7e6a", "shop_terrace": "#8b5a45",
    "retail_modern": "#b3b0a8", "industrial_shed": "#8d9196", "mill_brick": "#7a4536", "mill_stone": "#8d8570",
    "other": "#9a8f84",
}
ROOF_COLOUR = "#4a4d52"


class _Mesh:
    def __init__(self):
        self.pos: list[float] = []
        self.col: list[float] = []

    def tri(self, a, b, c, rgb):
        self.pos += [*a, *b, *c]
        self.col += rgb * 3

    def quad(self, a, b, c, d, rgb):
        self.tri(a, b, c, rgb)
        self.tri(a, c, d, rgb)

    def dump(self):
        return {"pos": [round(v, 2) for v in self.pos], "col": [round(v, 3) for v in self.col]}


def _rgb(hexs: str, shade: float = 1.0):
    return [int(hexs[i:i + 2], 16) / 255 * shade for i in (1, 3, 5)]


def build_scene(buildings: list[dict], roads: dict, dtm: Heightfield, dsm: Heightfield | None,
                centre: tuple[float, float], radius: float = 200.0, labels: list[str] | None = None,
                frame_origin: tuple[float, float, float] | None = None, terrain_step: float = 2.0) -> dict:
    """frame_origin=(E, N, H): put coordinates relative to this point (e.g. the world origin for Unreal export)
    instead of relative to the view centre."""
    ce, cn = centre
    base = float(np.nanmedian(dtm.sample([ce], [cn])))
    oe, on, ob = frame_origin if frame_origin else (ce, cn, base)

    def P(e, n, h):  # BNG + ODN -> right-handed Y-up (x east, y up, z south), as three.js and glTF use
        return (e - oe, h - ob, -(n - on))

    # ---- terrain
    step = terrain_step
    es = np.arange(ce - radius, ce + radius + step, step)
    ns = np.arange(cn + radius, cn - radius - step, -step)
    E, N = np.meshgrid(es, ns)
    H = dtm.sample(E.ravel(), N.ravel()).reshape(E.shape)
    H = np.where(np.isnan(H), np.nanmin(H), H)
    terrain = {"nx": len(es), "nz": len(ns), "x0": float(es[0] - oe), "z0": float(-(ns[0] - on)), "step": step,
               "h": [round(float(v - ob), 2) for v in H.ravel()]}

    # ---- buildings
    walls, roofs, openings = _Mesh(), _Mesh(), _Mesh()
    import shapely
    from shapely.geometry import Polygon, box

    view = box(ce - radius, cn - radius, ce + radius, cn + radius)
    kept = []
    for b in buildings:
        poly = Polygon(b["footprint"]["outer"])
        if not poly.intersects(view):
            continue
        kept.append(poly)
        m = b["massing"]
        g0 = m.get("ground_z_min", m["ground_z"]) - 0.2
        he = m["ground_z"] + m["eaves_height_m"]
        hr = m["ground_z"] + max(m.get("ridge_height_m") or m["eaves_height_m"], m["eaves_height_m"])
        wc = _building_wall_colour(b) or WALL_COLOURS.get(b["archetype"]["id"], WALL_COLOURS["other"])
        ring = list(poly.exterior.coords)
        for (x1, y1), (x2, y2) in zip(ring[:-1], ring[1:]):
            # shade walls by facing so the massing reads in a flat-lit render
            nx, ny = (y2 - y1), -(x2 - x1)
            shade = 0.78 + 0.22 * ((nx * 0.5 - ny * 0.8) / (math.hypot(nx, ny) or 1))
            walls.quad(P(x1, y1, g0), P(x2, y2, g0), P(x2, y2, he), P(x1, y1, he), _rgb(wc, shade))
        if b.get("facades"):
            _facade_openings(openings, poly, b, g0, m, P)
        if m["roof"].get("type") == "complex" and dsm is not None:
            _lidar_roof(roofs, poly, dsm, he, P, hr)
        else:
            _roof(roofs, poly, m["roof"], he, hr, P, wc)
    # Party-wall lines at eaves level, so semi pairs and terrace rows read as separate homes
    lines = []
    for b in buildings:
        outs = b.get("units", {}).get("outlines") or []
        if len(outs) < 2:
            continue
        m = b["massing"]
        he = m["ground_z"] + m["eaves_height_m"]
        for a, c in zip(outs[:-1], outs[1:]):
            inter = Polygon(a).buffer(0.05).intersection(Polygon(c).buffer(0.05))
            if inter.is_empty:
                continue
            mrr = inter.minimum_rotated_rectangle
            xs = list(mrr.exterior.coords)[:4]
            e1 = max(((xs[k], xs[(k + 1) % 4]) for k in range(4)), key=lambda q: math.dist(*q))
            (x1, y1), (x2, y2) = e1
            g0 = m.get("ground_z_min", m["ground_z"])
            lines.append([*P(x1, y1, he + 0.05), *P(x2, y2, he + 0.05)])
            # vertical seams where the party wall meets each facade
            lines.append([*P(x1, y1, g0), *P(x1, y1, he + 0.05)])
            lines.append([*P(x2, y2, g0), *P(x2, y2, he + 0.05)])

    # ---- roads
    road_mesh, pave_mesh, marks = _Mesh(), _Mesh(), []
    label_pts: dict[str, list] = {}
    for e in roads["edges"]:
        pts = [p for p in e["polyline"] if len(p) == 3]
        if len(pts) < 2:
            continue
        if not any(view.contains(shapely.geometry.Point(p[0], p[1])) for p in pts):
            continue
        w = e["width_m"] / 2
        pw = e["pavement_width_m"]
        up = e["kerb_upstand_m"]
        _ribbon(road_mesh, pts, -w, w, ROAD_LIFT_M, _rgb("#3b3d40"), P)
        if pw > 0:
            _ribbon(pave_mesh, pts, w, w + pw, ROAD_LIFT_M + up, _rgb("#9c9a94"), P)
            _ribbon(pave_mesh, pts, -w - pw, -w, ROAD_LIFT_M + up, _rgb("#9c9a94"), P)
        if e["hierarchy"] not in ("access", "access_restricted", "service"):
            marks.append([list(P(x, y, z + ROAD_LIFT_M + 0.02)) for x, y, z in pts])
        if e.get("name"):
            label_pts.setdefault(e["name"], []).extend(pts)

    # ---- trees from LiDAR (nDSM > 2.5 m, away from buildings, local maxima in a 7 m window)
    trees = []
    if dsm is not None:
        ndsm = dsm.sample(E.ravel(), N.ravel()).reshape(E.shape) - H
        ndsm = np.nan_to_num(ndsm)
        mask = ndsm > 2.5
        if kept:
            grown = shapely.union_all([p.buffer(1.5) for p in kept])
            mask &= ~shapely.contains_xy(grown, E, N)
        road_lines = [shapely.geometry.LineString([(p[0], p[1]) for p in e["polyline"]]).buffer(e["width_m"] / 2 + e["pavement_width_m"])
                      for e in roads["edges"] if len(e["polyline"]) >= 2]
        if road_lines:
            mask &= ~shapely.contains_xy(shapely.union_all(road_lines), E, N)
        from numpy.lib.stride_tricks import sliding_window_view

        k = 3
        padded = np.pad(np.where(mask, ndsm, 0), k, constant_values=0)
        local_max = sliding_window_view(padded, (2 * k + 1, 2 * k + 1)).max(axis=(2, 3))
        peaks = mask & (ndsm >= local_max) & (ndsm > 3.0)
        for r, c in zip(*np.nonzero(peaks)):
            h = float(ndsm[r, c])
            trees.append([round(float(E[r, c] - oe), 1), round(float(H[r, c] - ob), 1), round(float(-(N[r, c] - on)), 1), round(h, 1)])

    labels_out = []
    for name in labels or []:
        if name in label_pts:
            # the point on the named road about 35 m from the centre, so labels sit near the view focus
            x, y, z = min(label_pts[name], key=lambda q: abs(math.hypot(q[0] - ce, q[1] - cn) - 35.0))
            labels_out.append({"text": name, "pos": list(P(x, y, z + 4))})

    return {"terrain": terrain, "walls": walls.dump(), "roofs": roofs.dump(), "openings": openings.dump(), "party": lines, "roads": road_mesh.dump(),
            "pavements": pave_mesh.dump(), "marks": marks, "trees": trees, "labels": labels_out,
            "centre_bng": [ce, cn], "base_odn": base}


GLASS = "#2c3642"
SHOP_GLASS = "#46525e"
FRAME = "#e9e6df"
STOREY_M = 2.7


def _building_wall_colour(b: dict) -> str | None:
    cols = [f["wall_colour_srgb"] for f in b.get("facades", []) if f.get("wall_colour_srgb")]
    if not cols:
        return None
    arr = np.array([[int(c[i:i + 2], 16) for i in (1, 3, 5)] for c in cols])
    return "#{:02x}{:02x}{:02x}".format(*np.median(arr, axis=0).astype(int))


def _facade_openings(mesh: _Mesh, poly, b: dict, g0: float, m: dict, P):
    """Lay each facade's bays along its footprint edge: windows, doors, shopfronts as slightly proud quads."""
    import shapely

    ring = list(shapely.geometry.polygon.orient(poly, 1.0).exterior.coords)
    ground = m["ground_z"]
    eaves = ground + m["eaves_height_m"]
    for f in b["facades"]:
        i = f["edge_index"]
        if i + 1 >= len(ring):
            continue
        (x1, y1), (x2, y2) = ring[i], ring[i + 1]
        L = math.hypot(x2 - x1, y2 - y1)
        if L < 1.0:
            continue
        ux, uy = (x2 - x1) / L, (y2 - y1) / L
        nx, ny = uy, -ux  # outward normal of a CCW ring
        off = 0.04

        def rect(s0, s1, z0, z1, col):
            ax, ay = x1 + ux * s0 + nx * off, y1 + uy * s0 + ny * off
            bx, by = x1 + ux * s1 + nx * off, y1 + uy * s1 + ny * off
            mesh.quad(P(ax, ay, z0), P(bx, by, z0), P(bx, by, z1), P(ax, ay, z1), _rgb(col))

        door_col = f.get("door_colour_srgb", "#3a2f2a")
        bays = f.get("bays") or []
        total = sum(bb["width_m"] for bb in bays) or L
        scale = L / total
        s = 0.0
        for bay in bays:
            w = bay["width_m"] * scale
            c = s + w / 2
            g = bay.get("ground", "window")
            if g == "door":
                rect(c - 0.5, c + 0.5, ground, ground + 2.1, door_col)
                rect(c - 0.5, c + 0.5, ground + 2.15, ground + 2.45, GLASS)  # fanlight
            elif g == "shopfront":
                rect(s + 0.2, s + w - 0.2, ground + 0.3, ground + 2.9, SHOP_GLASS)
                rect(s + 0.1, s + w - 0.1, ground + 3.0, ground + 3.6, "#1f2a33")  # fascia sign band
            elif g == "garage":
                rect(c - 1.2, c + 1.2, ground, ground + 2.1, "#d8d6d0")
            elif g == "bay_window":
                rect(c - 1.0, c + 1.0, ground + 0.6, ground + 2.2, FRAME)
                rect(c - 0.9, c + 0.9, ground + 0.7, ground + 2.1, GLASS)
            elif g == "window":
                rect(c - 0.65, c + 0.65, ground + 0.9, ground + 2.2, FRAME)
                rect(c - 0.58, c + 0.58, ground + 0.97, ground + 2.13, GLASS)
            storey_h = max((eaves - ground) / max(m.get("storeys", 1), 1), 2.2)  # this building's own storey height
            win_h = min(1.3, storey_h - 1.0)
            for k, up in enumerate(bay.get("upper", []), start=1):
                sill = ground + storey_h * k + 0.75
                if up == "window" and sill + win_h <= eaves - 0.15:
                    rect(c - 0.55, c + 0.55, sill, sill + win_h, FRAME)
                    rect(c - 0.48, c + 0.48, sill + 0.07, sill + win_h - 0.07, GLASS)
            s += w


def _ribbon(mesh: _Mesh, pts, off_a: float, off_b: float, lift: float, rgb, P):
    """Strip between two lateral offsets of a polyline (left negative)."""
    arr = np.asarray(pts, dtype=float)
    d = np.gradient(arr[:, :2], axis=0)
    d /= np.linalg.norm(d, axis=1, keepdims=True) + 1e-9
    nrm = np.stack([-d[:, 1], d[:, 0]], axis=1)  # left normal
    A = arr[:, :2] - nrm * off_a
    B = arr[:, :2] - nrm * off_b
    for i in range(len(arr) - 1):
        z0, z1 = arr[i, 2] + lift, arr[i + 1, 2] + lift
        mesh.quad(P(*A[i], z0), P(*B[i], z0), P(*B[i + 1], z1), P(*A[i + 1], z1), rgb)


def _lidar_roof(mesh: _Mesh, poly, dsm: Heightfield, he: float, P, hr: float | None = None):
    """Roof surface straight from the 1 m DSM, clipped to the footprint (never below the wall top)."""
    import shapely

    rc = _rgb(ROOF_COLOUR)
    min_e, min_n, max_e, max_n = poly.bounds
    es = np.arange(np.floor(min_e), np.ceil(max_e) + 1.0)
    ns = np.arange(np.floor(min_n), np.ceil(max_n) + 1.0)
    E, N = np.meshgrid(es, ns)
    Z = dsm.sample(E.ravel(), N.ravel()).reshape(E.shape)
    inside = shapely.contains_xy(poly.buffer(-0.3), E, N)
    # 3x3 median (ignoring cells outside the footprint) removes LiDAR spikes, aerials and edge drop-offs
    from numpy.lib.stride_tricks import sliding_window_view

    Zm = np.where(inside, Z, np.nan)
    Zm = np.nanmedian(sliding_window_view(np.pad(Zm, 1, constant_values=np.nan), (3, 3)), axis=(2, 3))
    top = hr + 0.5 if hr is not None else np.nanmax(Zm)
    Z = np.clip(np.where(np.isfinite(Zm), Zm, he), he, top)
    for j in range(len(ns) - 1):
        for i in range(len(es) - 1):
            if inside[j, i] and inside[j, i + 1] and inside[j + 1, i] and inside[j + 1, i + 1]:
                a = P(E[j, i], N[j, i], Z[j, i]); b = P(E[j, i + 1], N[j, i + 1], Z[j, i + 1])
                c = P(E[j + 1, i + 1], N[j + 1, i + 1], Z[j + 1, i + 1]); d = P(E[j + 1, i], N[j + 1, i], Z[j + 1, i])
                mesh.quad(a, b, c, d, rc)
    # flat cap at wall top underneath, so thin edges never show a hole
    try:
        for tri in shapely.constrained_delaunay_triangles(poly).geoms:
            x, y, z = list(tri.exterior.coords)[:3]
            mesh.tri(P(*x, he), P(*y, he), P(*z, he), _rgb(ROOF_COLOUR, 0.85))
    except Exception:
        pass


def _roof(mesh: _Mesh, poly, roof: dict, he: float, hr: float, P, wall_colour: str = "#8b5a45"):
    rc = _rgb(ROOF_COLOUR)
    rc_dark = _rgb(ROOF_COLOUR, 0.8)
    t = roof.get("type", "flat")
    if t in ("flat", "complex") or hr - he < 0.4 or "ridge_bearing_deg" not in roof:
        import shapely

        h = he if t == "flat" else (he + hr) / 2
        try:
            tris = shapely.constrained_delaunay_triangles(poly)
            for tri in tris.geoms:
                a, b, c = list(tri.exterior.coords)[:3]
                mesh.tri(P(*a, h), P(*b, h), P(*c, h), rc)
        except Exception:
            pass
        return
    th = math.radians(roof["ridge_bearing_deg"])
    d = np.array([math.sin(th), math.cos(th)])  # along ridge (E, N)
    p = np.array([math.cos(th), -math.sin(th)])  # across ridge
    xy = np.asarray(poly.exterior.coords)
    s, q = xy @ d, xy @ p
    s0, s1 = s.min() - OVERHANG_M, s.max() + OVERHANG_M
    q0, q1 = q.min() - OVERHANG_M, q.max() + OVERHANG_M
    qc = (q0 + q1) / 2

    def W(si, qi, h):
        e, n = d * si + p * qi
        return P(e, n, h)

    he_o = he - OVERHANG_M * (hr - he) / max((q1 - q0) / 2, 0.1)  # eaves drop by the overhang along the pitch
    if t == "mono_pitch":
        mesh.quad(W(s0, q0, hr), W(s1, q0, hr), W(s1, q1, he_o), W(s0, q1, he_o), rc)
        return
    if t == "hip":
        half = (q1 - q0) / 2
        r0, r1 = s0 + half, s1 - half
        if r0 > r1:
            r0 = r1 = (s0 + s1) / 2
    else:  # gable
        r0, r1 = s0, s1
    mesh.quad(W(s0, q0, he_o), W(s1, q0, he_o), W(r1, qc, hr), W(r0, qc, hr), rc)
    mesh.quad(W(s1, q1, he_o), W(s0, q1, he_o), W(r0, qc, hr), W(r1, qc, hr), rc_dark)
    if t == "hip":
        mesh.tri(W(s0, q1, he_o), W(s0, q0, he_o), W(r0, qc, hr), _rgb(ROOF_COLOUR, 0.9))
        mesh.tri(W(s1, q0, he_o), W(s1, q1, he_o), W(r1, qc, hr), _rgb(ROOF_COLOUR, 0.9))
    else:
        gc = _rgb(wall_colour, 0.9)  # gable end wall
        mesh.tri(W(s0, q1, he_o), W(s0, q0, he_o), W(s0, qc, hr), gc)
        mesh.tri(W(s1, q0, he_o), W(s1, q1, he_o), W(s1, qc, hr), gc)


def write_html(scene: dict, path: Path, title: str, camera: dict) -> None:
    template = (Path(__file__).parent / "preview_template.html").read_text(encoding="utf-8")
    html = (template.replace("__TITLE__", title)
            .replace("__SCENE__", json.dumps(scene, separators=(",", ":")))
            .replace("__CAMERA__", json.dumps(camera)))
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(html, encoding="utf-8")
