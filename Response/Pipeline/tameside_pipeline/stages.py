"""File-level runners for Stages B and C (the CLI calls these; the logic lives in the stage modules)."""
from __future__ import annotations

import json
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
