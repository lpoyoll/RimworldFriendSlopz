import json

import numpy as np
import pytest

from tameside_pipeline.config import PipelineConfig, LandscapeSettings, SourceNotApproved, Zone
from tameside_pipeline.terrain import (
    Raster, build_terrain, fill_nodata, mosaic, read_asc, read_png16, write_png16,
)


def make_cfg(status="approved", q=4, tiles=(2, 1)):
    return PipelineConfig(
        origin_e=1000.0, origin_n=2000.0,
        landscape=LandscapeSettings(1.0, 128.0, 30000.0, q),
        zones={"z": Zone("z", 1000.0, 2000.0, tiles[0], tiles[1])},
        sources={"ea_lidar_dtm_1m": {"status": status, "attribution": "EA"}},
    )


def write_asc(path, data, xll, yll, nodata=-9999):
    rows = "\n".join(" ".join(f"{v:g}" for v in r) for r in data)
    path.write_text(f"ncols {data.shape[1]}\nnrows {data.shape[0]}\nxllcorner {xll}\nyllcorner {yll}\ncellsize 1\nNODATA_value {nodata}\n{rows}\n")


def test_read_asc_and_nodata(tmp_path):
    d = np.array([[1, 2], [3, -9999]], dtype=float)
    write_asc(tmp_path / "a.asc", d, 1000, 1998)
    r = read_asc(tmp_path / "a.asc")
    assert r.min_e == 1000 and r.max_n == 2000
    assert np.isnan(r.data[1, 1]) and r.data[0, 1] == 2


def test_mosaic_places_tiles_and_rejects_misalignment():
    a = Raster(np.ones((2, 2)), 1000.0, 2000.0, 1.0)
    b = Raster(np.full((2, 2), 5.0), 1002.0, 2000.0, 1.0)
    m = mosaic([a, b], 1000.0, 2000.0, 3, 5, 1.0)
    assert m[0, 0] == 1 and m[0, 3] == 5 and np.isnan(m[2, 4])
    with pytest.raises(ValueError):
        mosaic([Raster(np.ones((2, 2)), 1000.5, 2000.0, 1.0)], 1000.0, 2000.0, 3, 5, 1.0)


def test_fill_nodata_interpolates_hole():
    a = np.full((5, 5), 10.0)
    a[2, 2] = np.nan
    f, n = fill_nodata(a)
    assert n == 1 and f[2, 2] == pytest.approx(10.0)


def test_png16_round_trip(tmp_path):
    img = (np.arange(12, dtype=np.uint16).reshape(3, 4) * 5000).astype(np.uint16)
    write_png16(tmp_path / "t.png", img)
    assert np.array_equal(read_png16(tmp_path / "t.png"), img)


def test_build_terrain_end_to_end(tmp_path):
    cfg = make_cfg()
    # zone: 2x1 tiles of 4 quads -> 9 x 5 vertices; supply 10x6 m of data with a slope and a hole
    data = np.add.outer(np.arange(6) * -0.5, np.arange(10) * 1.0) + 100.0
    data[1, 1] = -9999
    write_asc(tmp_path / "dtm.asc", data, 1000, 1994)
    out = tmp_path / "out"
    m = build_terrain(cfg, "z", [tmp_path / "dtm.asc"], out)
    assert m["vertices"] == [9, 5]
    assert [t["file"] for t in m["tiles"]] == ["z_x0_y0.png", "z_x1_y0.png"]
    t0, t1 = read_png16(out / "z_x0_y0.png"), read_png16(out / "z_x1_y0.png")
    assert t0.shape == (5, 5) and np.array_equal(t0[:, -1], t1[:, 0])  # shared edge
    # first vertex is cell centre (1000.5, 1999.5) -> UE (50, 50): south of origin is +Y
    assert m["ue_landscape"]["location_cm"][:2] == [50.0, 50.0]
    assert m["filled_nodata_cells"] == 1
    assert json.loads((out / "terrain_manifest.json").read_text())["zone"] == "z"
    # 100 m -> value 32768 + (10000 - 30000) = 12768
    assert int(read_png16(out / "z_full.png")[0, 0]) == 12768


def test_licence_gate_blocks_unapproved_source(tmp_path):
    with pytest.raises(SourceNotApproved):
        build_terrain(make_cfg(status="pending_signoff"), "z", [], tmp_path)


def test_commercial_mode_blocks_non_commercial_sources(tmp_path):
    cfg = make_cfg()
    cfg.sources["ea_lidar_dtm_1m"]["commercial_ok"] = False
    from dataclasses import replace
    with pytest.raises(SourceNotApproved):
        build_terrain(replace(cfg, project_mode="commercial"), "z", [], tmp_path)
