"""File-level runners for Stages B and C (the CLI calls these; the logic lives in the stage modules)."""
from __future__ import annotations

import json
import math
import time
from collections import Counter, defaultdict
from pathlib import Path

from .geo import Heightfield, read_vector, read_vectors, zone_bounds

MARGIN_M = 50.0  # read a little beyond the zone so buildings and roads on the edge are complete


def _raster_files(path: Path) -> list[Path]:
    files = sorted(path.glob("*")) if path.is_dir() else [path]
    return [f for f in files if f.suffix.lower() in (".asc", ".tif", ".tiff")]


def _layer(path: Path, wanted: str | None, contains: str) -> str | None:
    if wanted:
        return wanted
    import pyogrio

    layers = [name for name, _ in pyogrio.list_layers(path)]
    for name in layers:
        if contains in name.lower():
            return name
    return layers[0] if layers else None


def _dedupe_osm(gdf):
    """Features repeated across OSM tiles (ways crossing tile edges) keep one copy."""
    keys = [c for c in ("osm_id", "osm_way_id") if c in gdf.columns]
    if not len(gdf) or not keys:
        return gdf
    return gdf.drop_duplicates(subset=keys).reset_index(drop=True)


def run_footprints(cfg, a) -> int:
    from .footprints import attach_plots, merge_footprints

    zone = cfg.zone(a.zone)
    b = zone_bounds(cfg, zone, MARGIN_M)
    cfg.require_source("os_openmap_local")
    os_gdf = read_vectors(a.os, b, lambda p: _layer(p, a.os_layer, "build"))
    if not len(os_gdf):
        raise SystemExit(f"No OS buildings inside zone '{a.zone}'. Check the 100 km squares supplied (Tameside spans SJ and SD).")
    ms = osm = plots = None
    if a.ms:
        cfg.require_source("ms_building_footprints")
        ms = read_vector(a.ms, b)
    if a.osm:
        cfg.require_source("osm")
        osm = _dedupe_osm(read_vectors(a.osm, b, lambda p: "multipolygons"))
    id_field = next((c for c in ("id", "ID", "fid") if c in os_gdf.columns), None)
    fp = merge_footprints(os_gdf, ms, osm, os_id_field=id_field)
    if a.osm:
        from .footprints import attach_osm_pois

        fp = attach_osm_pois(fp, _dedupe_osm(read_vectors(a.osm, b, lambda p: "points")))
    if a.uprn:
        from .footprints import attach_address_counts, read_uprn_csv

        cfg.require_source("os_open_uprn")
        fp = attach_address_counts(fp, read_uprn_csv(a.uprn, b))
    if a.inspire:
        cfg.require_source("hmlr_inspire")
        plots = read_vector(a.inspire, b)
        fp = attach_plots(fp, plots)
    a.out.mkdir(parents=True, exist_ok=True)
    out = fp.copy()
    out["sources"] = out["sources"].apply(json.dumps)
    out["osm"] = out["osm"].apply(json.dumps)
    if "osm_poi_names" in out.columns:
        out["osm_poi_names"] = out["osm_poi_names"].apply(json.dumps)
    out.to_file(a.out / "footprints.gpkg", layer="footprints", driver="GPKG")
    n_ms = sum("ms_building_footprints" in s for s in fp["sources"])
    print(f"{len(fp)} footprints ({n_ms} gap-filled from MS) -> {a.out / 'footprints.gpkg'}")
    return 0


def run_roads(cfg, a) -> int:
    from .roads import build_road_graph, write_road_graph

    zone = cfg.zone(a.zone)
    b = zone_bounds(cfg, zone, MARGIN_M)
    cfg.require_source("os_open_roads")
    links = read_vectors(a.os_roads, b, lambda p: _layer(p, a.os_layer, "link"))
    osm = None
    if a.osm:
        cfg.require_source("osm")
        osm = _dedupe_osm(read_vectors(a.osm, b, lambda p: "lines"))
    hf = None
    if a.dtm:
        cfg.require_source("ea_lidar_dtm_1m")
        hf = Heightfield.from_files(_raster_files(a.dtm), b, cfg.landscape.resolution_m)
    g = build_road_graph(links, osm, hf)
    g["zone"] = a.zone
    write_road_graph(g, a.out / "roads.json")
    print(f"{len(g['edges'])} road links, {len(g['nodes'])} nodes -> {a.out / 'roads.json'}")
    return 0


def run_massing(cfg, a) -> int:
    import geopandas as gpd

    from .footprints import shared_wall_counts
    from .massing import build_records

    zone = cfg.zone(a.zone)
    b = zone_bounds(cfg, zone, MARGIN_M)
    cfg.require_source("ea_lidar_dtm_1m")
    cfg.require_source("ea_lidar_dsm_1m")
    fp = gpd.read_file(a.footprints, layer="footprints")
    fp["sources"] = fp["sources"].apply(json.loads)
    if "osm_poi_names" in fp.columns:
        fp["osm_poi_names"] = fp["osm_poi_names"].apply(lambda s: json.loads(s) if isinstance(s, str) else [])
    for col in ("addresses", "osm_units", "osm_shops"):
        if col not in fp.columns:
            fp[col] = 0
    if "osm" in fp.columns:
        fp["osm"] = fp["osm"].apply(lambda s: json.loads(s) if isinstance(s, str) else {})
    dtm = Heightfield.from_files(_raster_files(a.dtm), b, cfg.landscape.resolution_m)
    dsm = Heightfield.from_files(_raster_files(a.dsm), b, cfg.landscape.resolution_m)
    records = build_records(fp, dtm, dsm, a.zone, shared_wall_counts(fp))
    a.out.mkdir(parents=True, exist_ok=True)
    with open(a.out / "buildings.jsonl", "w", encoding="utf-8") as f:
        for r in records:
            f.write(json.dumps(r) + "\n")
    flagged = sum(1 for r in records if r["qa"]["flags"])
    roofs: dict[str, int] = {}
    for r in records:
        roofs[r["massing"]["roof"]["type"]] = roofs.get(r["massing"]["roof"]["type"], 0) + 1
    print(f"{len(records)} buildings -> {a.out / 'buildings.jsonl'}; roofs {roofs}; {flagged} flagged for QA")
    return 0


def run_preview(cfg, a) -> int:
    from .preview import build_scene, write_html

    r = a.radius + 20
    b = (a.e - r, a.n - r, a.e + r, a.n + r)
    buildings = [json.loads(line) for line in open(a.buildings, encoding="utf-8")]
    roads = json.loads(Path(a.roads).read_text(encoding="utf-8"))
    dtm = Heightfield.from_files(_raster_files(a.dtm), b, cfg.landscape.resolution_m)
    dsm = Heightfield.from_files(_raster_files(a.dsm), b, cfg.landscape.resolution_m) if a.dsm else None
    scene = build_scene(buildings, roads, dtm, dsm, (a.e, a.n), a.radius, a.label)
    cam = a.camera or [-90.0, 70.0, 110.0, 0.0, 0.0, 0.0]
    write_html(scene, a.out, a.title, {"pos": cam[:3], "target": cam[3:], "fov": 45})
    print(f"Preview: {len(scene['trees'])} trees, {len(scene['walls']['pos']) // 9} wall triangles -> {a.out}")
    return 0


def run_streetview(cfg, a) -> int:
    from shapely.geometry import LineString, Polygon

    from .streetview import facade_edges, fetch_metadata, match_views

    cfg.require_source("mapillary")
    zone = cfg.zone(a.zone)
    b = zone_bounds(cfg, zone, MARGIN_M)
    buildings = [json.loads(line) for line in open(a.buildings, encoding="utf-8")]
    roads = json.loads(Path(a.roads).read_text(encoding="utf-8"))
    road_lines = [LineString([(p[0], p[1]) for p in e["polyline"]]) for e in roads["edges"] if len(e["polyline"]) >= 2]
    images = fetch_metadata(b, a.cache)
    edges = facade_edges(buildings, road_lines)
    obstacles = {"geoms": [Polygon(x["footprint"]["outer"]) for x in buildings], "ids": [x["footprint_id"] for x in buildings]}
    matches = match_views(edges, images, obstacles)
    a.out.mkdir(parents=True, exist_ok=True)
    with open(a.out / "facade_views.jsonl", "w", encoding="utf-8") as f:
        for m in matches:
            f.write(json.dumps(m) + "\n")
    seen_edges = sum(1 for m in matches if m["views"])
    seen_bldg = len({m["footprint_id"] for m in matches if m["views"]})
    street_bldg = len({m["footprint_id"] for m in matches})
    print(f"{len(images)} images; {len(matches)} street-facing facades, {seen_edges} seen "
          f"({100 * seen_edges / max(len(matches), 1):.0f}%); buildings with a seen facade: {seen_bldg}/{street_bldg}")
    return 0


def run_facades(cfg, a) -> int:
    """Stage D part 2: detect facade elements in matched images, then infer unseen facades."""
    import concurrent.futures as cf

    import cv2
    import shapely
    from pyproj import Transformer
    from shapely.geometry import LineString, Point, Polygon
    from shapely.strtree import STRtree

    from . import facades as F
    from .streetview import facade_edges, token

    cfg.require_source("mapillary")
    zone = cfg.zone(a.zone)
    b = zone_bounds(cfg, zone, MARGIN_M)
    records = {}
    for line in open(a.buildings, encoding="utf-8"):
        r = json.loads(line)
        records[r["footprint_id"]] = r
    roads = json.loads(Path(a.roads).read_text(encoding="utf-8"))
    views = [json.loads(line) for line in open(a.views, encoding="utf-8")]
    dtm = Heightfield.from_files(_raster_files(a.dtm), b, cfg.landscape.resolution_m)
    to_bng = Transformer.from_crs(4326, 27700, always_xy=True)

    seen = [v for v in views if v["views"]]
    if a.limit:
        seen = seen[: a.limit]
    ids = sorted({x["image_id"] for v in seen for x in v["views"]})
    tok = token()
    details = F.fetch_image_details(ids, a.cache, tok)
    with cf.ThreadPoolExecutor(8) as ex:
        list(ex.map(lambda i: F.download_thumb(details[i], a.cache) if i in details else None, ids))
    print(f"{len(seen)} seen facades, {len(ids)} images downloaded")

    models = F.FacadeModels()
    crops_dir = a.cache / "crops"
    crops_dir.mkdir(parents=True, exist_ok=True)
    observed = {}
    rejected = Counter()
    t0 = time.time()
    for n_done, v in enumerate(seen, 1):
        rec = records[v["footprint_id"]]
        m = rec["massing"]
        poly = shapely.geometry.polygon.orient(Polygon(rec["footprint"]["outer"]), 1.0)
        pts = list(poly.exterior.coords)
        ea, eb = pts[v["edge_index"]], pts[v["edge_index"] + 1]
        z0 = m.get("ground_z_min", m["ground_z"])
        z1 = m["ground_z"] + m["eaves_height_m"]
        # 1) rectify every candidate view, 2) CLIP: which crop really shows the facade, 3) detect on the best only
        cands = []
        for view in v["views"]:
            d = details.get(view["image_id"])
            path = a.cache / "img" / f"{view['image_id']}.jpg"
            geom = (d or {}).get("computed_geometry") or {}
            if not d or not path.exists() or not geom:
                continue
            ce, cn = to_bng.transform(*geom["coordinates"])
            cz = float(dtm.sample([ce], [cn])[0]) + F.CAMERA_HEIGHT_M
            img = cv2.cvtColor(cv2.imread(str(path)), cv2.COLOR_BGR2RGB)
            crop, vis, src_ppm = F.rectify(img, d, (ce, cn, cz), ea, eb, z0, z1)
            if crop is not None and vis >= F.MIN_VISIBLE:
                cands.append((view, crop, vis, src_ppm, img, d, (ce, cn, cz)))
        if not cands:
            continue
        probs = models.facade_probability([c[1] for c in cands])
        scored = sorted(((p_ * c[2] * min(1.0, c[3] / 30.0) * c[0]["score"], p_, c) for p_, c in zip(probs, cands)), key=lambda t: -t[0])
        _, fprob, (view, crop, vis, src_ppm, img, d, cam) = scored[0]
        correction, refined_p = models.refine_pose(img, d, cam, ea, eb, z0, z1)
        if correction != (0.0, 0.0) and refined_p > fprob:
            crop2, vis2, ppm2 = F.rectify(img, d, cam, ea, eb, z0, z1, correction)
            if crop2 is not None and vis2 >= F.MIN_VISIBLE:
                crop, vis, src_ppm, fprob = crop2, vis2, ppm2, max(fprob, refined_p)
        if fprob < F.MIN_FACADE_PROB:
            rejected["not_a_facade"] += 1
            continue
        width_m, height_m = math.hypot(eb[0] - ea[0], eb[1] - ea[1]), z1 - z0
        dets = models.detect(crop)
        occ = F.occlusion(dets, crop.shape)
        if occ > F.MAX_OCCLUSION:
            rejected["occluded"] += 1
            continue
        bays_ok = src_ppm >= F.MIN_SRC_PX_PER_M_FOR_BAYS
        parsed = F.parse_facade(dets, width_m, height_m, m.get("storeys", 2))
        patches, wall_col = F.wall_patches(crop, dets)
        mat, mat_p = models.material(patches, wall_col)
        n_el = sum(parsed["counts"].values())
        conf = round(min(1.0, fprob * vis * (1 - occ) * min(1.0, src_ppm / 30.0) * min(1.0, 0.5 + 0.1 * n_el) * (0.5 + mat_p) * min(1.0, width_m / 4.0)), 2)
        dc = F.door_colour(crop, dets) if bays_ok else None
        fac = {"edge_index": v["edge_index"], "street_facing": True, "wall_material": mat,
               **({"wall_colour_srgb": wall_col} if wall_col else {}),
               **({"door_colour_srgb": dc} if dc else {}),
               "bays": parsed["bays"] if (bays_ok and parsed["bays"]) else F.synth_bays(width_m, None, rec["archetype"]["id"], m.get("storeys", 2)),
               "bays_source": "observed" if (bays_ok and parsed["bays"]) else "inferred:archetype",
               "storeys_seen": parsed["storeys_seen"], "element_counts": parsed["counts"],
               "observations": 1, "confidence": conf, "basis": "observed",
               "view": {"image_id": view["image_id"], "captured_at": view.get("captured_at"), "visible_fraction": round(vis, 2),
                        "facade_probability": round(fprob, 2), "occlusion": round(occ, 2), "pose_correction_deg": list(correction), "source_px_per_m": round(src_ppm, 1)}}
        observed[(v["footprint_id"], v["edge_index"])] = fac
        qa = cv2.cvtColor(crop, cv2.COLOR_RGB2BGR)
        colours = {"window": (255, 200, 0), "door": (0, 0, 255), "shopfront": (255, 0, 255), "garage": (0, 255, 255), "occluder": (128, 128, 128)}
        for dd in dets:
            x0, y0, x1, y1 = [int(q) for q in dd["box"]]
            cv2.rectangle(qa, (x0, y0), (x1, y1), colours[dd["kind"]], 2)
        cv2.imwrite(str(crops_dir / f"{v['footprint_id']}_{v['edge_index']}.jpg"), qa)
        if n_done % 25 == 0:
            print(f"  {n_done}/{len(seen)} facades, {len(observed)} observed, {time.time() - t0:.0f}s", flush=True)

    # propagation to unseen street-facing facades
    road_lines = [LineString([(p[0], p[1]) for p in e["polyline"]]) for e in roads["edges"] if len(e["polyline"]) >= 2]
    road_names = [e.get("name") for e in roads["edges"] if len(e["polyline"]) >= 2]
    edges = facade_edges(list(records.values()), road_lines)
    rtree = STRtree(road_lines)
    street_of = {}
    for fid, r in records.items():
        c = Polygon(r["footprint"]["outer"]).centroid
        j = rtree.nearest(c)
        street_of[fid] = road_names[j] if j is not None else None
    polys = {fid: Polygon(r["footprint"]["outer"]) for fid, r in records.items()}
    fids = list(polys)
    ptree = STRtree([polys[f] for f in fids])
    neighbours = {f: [fids[j] for j in ptree.query(polys[f].buffer(0.5), predicate="intersects") if fids[j] != f] for f in fids}
    facades = F.propagate(records, observed, edges, neighbours, street_of)

    per_bldg = defaultdict(list)
    for (fid, _), fac in facades.items():
        per_bldg[fid].append(fac)
    a.out.mkdir(parents=True, exist_ok=True)
    with open(a.out / "buildings_facades.jsonl", "w", encoding="utf-8") as f:
        for fid, r in records.items():
            r = {**r, "facades": sorted(per_bldg.get(fid, []), key=lambda x: x["edge_index"])}
            f.write(json.dumps(r) + "\n")
    bases = Counter(x["basis"] for x in facades.values())
    mats = Counter(x["wall_material"] for x in observed.values())
    print(f"facades: {dict(bases)}")
    print(f"observed materials: {dict(mats)}; rejected views: {dict(rejected)}")
    return 0
