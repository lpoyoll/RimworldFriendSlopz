"""File-level runners for Stages B and C (the CLI calls these; the logic lives in the stage modules)."""
from __future__ import annotations

import json
from pathlib import Path

from .geo import Heightfield, read_vector, zone_bounds

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


def run_footprints(cfg, a) -> int:
    from .footprints import attach_plots, merge_footprints

    zone = cfg.zone(a.zone)
    b = zone_bounds(cfg, zone, MARGIN_M)
    cfg.require_source("os_openmap_local")
    os_gdf = read_vector(a.os, b, _layer(a.os, a.os_layer, "build"))
    ms = osm = plots = None
    if a.ms:
        cfg.require_source("ms_building_footprints")
        ms = read_vector(a.ms, b)
    if a.osm:
        cfg.require_source("osm")
        osm = read_vector(a.osm, b, "multipolygons")
    fp = merge_footprints(os_gdf, ms, osm, os_id_field="id" if "id" in os_gdf.columns else "fid")
    if a.inspire:
        cfg.require_source("hmlr_inspire")
        plots = read_vector(a.inspire, b)
        fp = attach_plots(fp, plots)
    a.out.mkdir(parents=True, exist_ok=True)
    out = fp.copy()
    out["sources"] = out["sources"].apply(json.dumps)
    out["osm"] = out["osm"].apply(json.dumps)
    out.to_file(a.out / "footprints.gpkg", layer="footprints", driver="GPKG")
    n_ms = sum("ms_building_footprints" in s for s in fp["sources"])
    print(f"{len(fp)} footprints ({n_ms} gap-filled from MS) -> {a.out / 'footprints.gpkg'}")
    return 0


def run_roads(cfg, a) -> int:
    from .roads import build_road_graph, write_road_graph

    zone = cfg.zone(a.zone)
    b = zone_bounds(cfg, zone, MARGIN_M)
    cfg.require_source("os_open_roads")
    links = read_vector(a.os_roads, b, _layer(a.os_roads, a.os_layer, "link"))
    osm = None
    if a.osm:
        cfg.require_source("osm")
        osm = read_vector(a.osm, b, "lines")
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
