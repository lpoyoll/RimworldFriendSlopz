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
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
