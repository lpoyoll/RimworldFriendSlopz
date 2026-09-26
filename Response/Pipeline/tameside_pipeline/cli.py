"""Command line entry: python -m tameside_pipeline.cli <command> ..."""
from __future__ import annotations

import argparse
import json
from pathlib import Path

from .config import load_config
from .coords import WorldOrigin
from .terrain import build_terrain

DEFAULT_CONFIG = Path(__file__).resolve().parent.parent / "config" / "tameside.json"


def main(argv: list[str] | None = None) -> int:
    p = argparse.ArgumentParser(prog="tameside_pipeline")
    p.add_argument("--config", type=Path, default=DEFAULT_CONFIG)
    sub = p.add_subparsers(dest="cmd", required=True)

    c = sub.add_parser("coords", help="Convert BNG <-> UE coordinates")
    c.add_argument("--e", type=float); c.add_argument("--n", type=float); c.add_argument("--h", type=float, default=0.0)
    c.add_argument("--ue", type=float, nargs=3, metavar=("X", "Y", "Z"))

    t = sub.add_parser("terrain", help="Stage A: EA DTM tiles -> UE heightmap tiles")
    t.add_argument("--zone", required=True)
    t.add_argument("--input", type=Path, required=True, help="Folder of .asc/.tif DTM tiles, or a single file")
    t.add_argument("--out", type=Path, required=True)

    f = sub.add_parser("footprints", help="Stage B: merge building footprints (OS + MS + OSM attrs + INSPIRE plots)")
    f.add_argument("--zone", required=True)
    f.add_argument("--os", type=Path, nargs="+", required=True, help="OS OpenMap Local building files (one per 100 km square, e.g. SJ and SD)")
    f.add_argument("--os-layer", default=None, help="Layer name, default: first layer containing 'building'")
    f.add_argument("--ms", type=Path, help="Microsoft ML building footprints (GeoJSON)")
    f.add_argument("--osm", type=Path, nargs="+", help="OSM extract(s) (.osm.pbf or .osm; several tiles are merged)")
    f.add_argument("--uprn", type=Path, help="OS Open UPRN CSV (address points, for unit counts)")
    f.add_argument("--inspire", type=Path, help="HMLR INSPIRE index polygons (GML/GPKG)")
    f.add_argument("--out", type=Path, required=True)

    r = sub.add_parser("roads", help="Stage B: road graph from OS Open Roads + OSM")
    r.add_argument("--zone", required=True)
    r.add_argument("--os-roads", type=Path, nargs="+", required=True, help="OS Open Roads RoadLink files (one per 100 km square)")
    r.add_argument("--os-layer", default=None, help="Layer name, default: first layer containing 'link'")
    r.add_argument("--osm", type=Path, nargs="+", help="OSM extract(s) (.osm.pbf or .osm; several tiles are merged)")
    r.add_argument("--dtm", type=Path, help="Folder of DTM tiles for road heights")
    r.add_argument("--out", type=Path, required=True)

    m = sub.add_parser("massing", help="Stage C: heights, roof type and archetype per footprint")
    m.add_argument("--zone", required=True)
    m.add_argument("--footprints", type=Path, required=True, help="footprints.gpkg from the footprints command")
    m.add_argument("--dtm", type=Path, required=True)
    m.add_argument("--dsm", type=Path, required=True)
    m.add_argument("--out", type=Path, required=True)

    v = sub.add_parser("preview", help="Stage I: 3D HTML preview of generated output around a point")
    v.add_argument("--buildings", type=Path, required=True)
    v.add_argument("--roads", type=Path, required=True)
    v.add_argument("--dtm", type=Path, required=True)
    v.add_argument("--dsm", type=Path, help="Adds LiDAR-detected trees")
    v.add_argument("--e", type=float, required=True); v.add_argument("--n", type=float, required=True)
    v.add_argument("--radius", type=float, default=200.0)
    v.add_argument("--title", default="Preview")
    v.add_argument("--label", action="append", default=[], help="Road name to label (repeatable)")
    v.add_argument("--camera", type=float, nargs=6, metavar=("X", "Y", "Z", "TX", "TY", "TZ"),
                   help="Camera and target in local metres (x east, y up, z south of the centre)")
    v.add_argument("--out", type=Path, required=True)

    sv = sub.add_parser("streetview", help="Stage D part 1: match Mapillary images to street-facing facades")
    sv.add_argument("--zone", required=True)
    sv.add_argument("--buildings", type=Path, required=True)
    sv.add_argument("--roads", type=Path, required=True)
    sv.add_argument("--cache", type=Path, required=True, help="Folder for cached image metadata (and later, images)")
    sv.add_argument("--out", type=Path, required=True)

    fc = sub.add_parser("facades", help="Stage D part 2: facade elements from images, then infer unseen facades")
    fc.add_argument("--zone", required=True)
    fc.add_argument("--buildings", type=Path, required=True)
    fc.add_argument("--roads", type=Path, required=True)
    fc.add_argument("--views", type=Path, required=True, help="facade_views.jsonl from the streetview command")
    fc.add_argument("--dtm", type=Path, required=True)
    fc.add_argument("--cache", type=Path, required=True)
    fc.add_argument("--limit", type=int, default=0, help="Only process the first N seen facades (for QA)")
    fc.add_argument("--out", type=Path, required=True)

    a = p.parse_args(argv)
    cfg = load_config(a.config)
    origin = WorldOrigin(cfg.origin_e, cfg.origin_n)

    if a.cmd == "coords":
        if a.ue:
            e, n, h = origin.ue_to_bng(*a.ue)
            print(json.dumps({"e": e, "n": n, "h_m": h}))
        else:
            if a.e is None or a.n is None:
                p.error("coords needs --e and --n, or --ue X Y Z")
            x, y, z = origin.bng_to_ue(a.e, a.n, a.h)
            print(json.dumps({"x_cm": x, "y_cm": y, "z_cm": z}))
        return 0

    if a.cmd == "terrain":
        inputs = sorted(a.input.glob("*")) if a.input.is_dir() else [a.input]
        inputs = [f for f in inputs if f.suffix.lower() in (".asc", ".tif", ".tiff")]
        if not inputs:
            p.error(f"No .asc/.tif files found in {a.input}")
        m = build_terrain(cfg, a.zone, inputs, a.out)
        print(f"Wrote {len(m['tiles'])} tile(s) to {a.out}; heights {m['height_m']['min']:.1f}..{m['height_m']['max']:.1f} m; filled {m['filled_nodata_cells']} nodata cells")
        return 0

    if a.cmd in ("footprints", "roads", "massing", "preview", "streetview", "facades"):
        from . import stages
        return getattr(stages, f"run_{a.cmd}")(cfg, a)
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
