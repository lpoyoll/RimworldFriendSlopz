import json

import geopandas as gpd
import numpy as np
import pytest
from shapely.geometry import LineString, Polygon, box

from tameside_pipeline.footprints import attach_plots, merge_footprints, parse_other_tags, shared_wall_counts
from tameside_pipeline.geo import Heightfield, stable_id
from tameside_pipeline.massing import build_records, classify_roof, compute_massing, guess_archetype
from tameside_pipeline.roads import alter_street_name, build_road_graph
from tameside_pipeline.terrain import Raster

CRS = "EPSG:27700"
E0, N0 = 1000.0, 2100.0  # raster NW edge; 100 x 100 m


def gdf(geoms, **cols):
    return gpd.GeoDataFrame(cols, geometry=geoms, crs=CRS)


def flat_hf(z=100.0):
    return Heightfield(Raster(np.full((100, 100), z), E0, N0, 1.0))


def dsm_with(fn):
    """DSM = 100 + fn(e, n) on cell centres (fn returns 0 for ground)."""
    rr, cc = np.mgrid[0:100, 0:100]
    e, n = E0 + cc + 0.5, N0 - rr - 0.5
    return Heightfield(Raster(100.0 + fn(e, n), E0, N0, 1.0))


# ---------------------------------------------------------------- footprints

def test_parse_other_tags():
    assert parse_other_tags('"building:levels"=>"3","shop"=>"bakery"') == {"building:levels": "3", "shop": "bakery"}
    assert parse_other_tags(None) == {}


def test_merge_keeps_os_gapfills_ms_and_copies_osm():
    os_ = gdf([box(1010, 2010, 1015, 2020), box(1015, 2010, 1020, 2020)], id=["osA", "osB"])
    ms = gdf([box(1010.3, 2010.2, 1015.2, 2019.8),  # duplicate of osA -> dropped
              box(1050, 2050, 1060, 2058)])           # new -> kept
    osm = gdf([box(1009.8, 2009.8, 1015.1, 2020.1)], building=["retail"], other_tags=['"building:levels"=>"2","shop"=>"bakery"'])
    fp = merge_footprints(os_, ms, osm)
    assert len(fp) == 3
    a = fp[fp.footprint_id == stable_id("fp", "os_openmap_local", "osA")].iloc[0]
    assert a.osm == {"building": "retail", "building:levels": "2", "shop": "bakery"}
    assert a.sources == ["os_openmap_local", "osm"]
    assert sum("ms_building_footprints" in s for s in fp.sources) == 1
    # stable IDs: same input, same IDs
    assert list(merge_footprints(os_, ms, osm).footprint_id) == list(fp.footprint_id)


def test_tiny_and_invalid_polygons_are_dropped():
    bow = Polygon([(0, 0), (10, 10), (10, 0), (0, 10)])  # self-intersecting, repaired into two triangles
    fp = merge_footprints(gdf([box(0, 0, 2, 2), bow], id=["tiny", "bow"]))
    assert len(fp) == 2 and all(g.is_valid and g.area >= 6 for g in fp.geometry)


def test_shared_walls_terrace_semi_detached():
    row = [box(1000 + 5 * i, 2000, 1005 + 5 * i, 2009) for i in range(3)]
    fp = merge_footprints(gdf(row + [box(1100, 2000, 1108, 2008)], id=list("abcd")))
    counts = dict(zip(fp.footprint_id, shared_wall_counts(fp)))
    ids = {k: stable_id("fp", "os_openmap_local", k) for k in "abcd"}
    assert [counts[ids[k]] for k in "abcd"] == [1, 2, 1, 0]


def test_attach_plots():
    fp = merge_footprints(gdf([box(1010, 2010, 1015, 2020)], id=["a"]))
    fp = attach_plots(fp, gdf([box(1000, 2000, 1030, 2030)], INSPIREID=[42]))
    assert fp.plot_id[0] == stable_id("plt", "hmlr_inspire", 42) and "hmlr_inspire" in fp.sources[0]


# ---------------------------------------------------------------- roads

def test_road_graph_osm_enrichment_and_heights():
    links = gdf([LineString([(1010, 2050), (1090, 2050)]), LineString([(1090, 2050), (1090, 2010)])],
                id=["L1", "L2"], road_function=["A Road", "Local Road"], form_of_way=["Single Carriageway"] * 2,
                name_1=["Stamford Street", None], start_node=["N1", "N2"], end_node=["N2", "N3"])
    # OSM way drawn the opposite direction, one-way, 20 mph
    osm = gdf([LineString([(1091, 2051), (1009, 2051)])], highway=["primary"], name=["Stamford Street"],
              other_tags=['"oneway"=>"yes","maxspeed"=>"20 mph","lanes"=>"1"'])
    hf = Heightfield(Raster(np.add.outer(np.zeros(100), np.arange(100.0)) + 100, E0, N0, 1.0))  # rises 1 m per m east
    g = build_road_graph(links, osm, hf)
    a, b = g["edges"]
    assert a["oneway"] == "backward" and a["speed_mph"] == 20 and a["lanes"] == 1 and "osm" in a["sources"]
    assert "speed_mph" not in a["assumed"] and "width_m" in a["assumed"]
    assert b["oneway"] is None and b["speed_mph"] == 30 and b["sources"] == ["os_open_roads"]
    assert a["polyline"][0][2] == pytest.approx(100 + 9.5) and len(a["polyline"]) == 17  # densified to 5 m
    assert a["to"] == b["from"]
    kinds = {n["id"]: n["kind"] for n in g["nodes"]}
    assert kinds[a["to"]] == "through" and kinds[a["from"]] == "dead_end"
    assert a["kerb_upstand_m"] == 0.125


def test_altered_street_names_are_deterministic():
    assert alter_street_name("Stamford Street") == alter_street_name("Stamford Street")
    assert alter_street_name("Stamford Street").endswith(" Street") and alter_street_name("Stamford Street") != "Stamford Street"
    assert alter_street_name(None) is None


# ---------------------------------------------------------------- massing

HOUSE = box(1020, 2040, 1030, 2046)  # 10 m E-W, 6 m N-S; ridge runs E-W along n = 2043


def gable(e, n):
    inside = (e > 1020) & (e < 1030) & (n > 2040) & (n < 2046)
    return np.where(inside, 8.0 - np.abs(n - 2043.0), 0.0)  # 45 deg pitch, eaves 5 m, ridge 8 m


def test_gable_roof():
    m, flags = compute_massing(HOUSE, flat_hf(), dsm_with(gable))
    assert m["roof"]["type"] == "gable"
    assert m["roof"]["ridge_bearing_deg"] == pytest.approx(90, abs=10)
    assert 7.0 < m["ridge_height_m"] <= 8.0 and m["eaves_height_m"] == pytest.approx(5.0, abs=0.3)
    assert m["storeys"] == 2 and m["ground_z"] == 100.0


def test_flat_roof():
    fn = lambda e, n: np.where((e > 1020) & (e < 1040) & (n > 2020) & (n < 2040), 12.0, 0.0)
    m, _ = compute_massing(box(1020, 2020, 1040, 2040), flat_hf(), dsm_with(fn))
    assert m["roof"]["type"] == "flat" and m["eaves_height_m"] == pytest.approx(12.0) and m["storeys"] == 4


def test_hip_roof():
    def hip(e, n):
        inside = (e > 1020) & (e < 1034) & (n > 2040) & (n < 2048)
        d = np.minimum.reduce([n - 2040, 2048 - n, e - 1020, 1034 - e])
        return np.where(inside, 5.0 + np.minimum(d, 4.0) * 0.75, 0.0)
    m, _ = compute_massing(box(1020, 2040, 1034, 2048), flat_hf(), dsm_with(hip))
    assert m["roof"]["type"] == "hip"


def test_mono_pitch_from_gradients():
    gx = np.zeros(40); gy = np.full(40, 0.6)  # every cell falls south
    assert classify_roof(gx, gy)["type"] == "mono_pitch"


def test_missing_lidar_is_flagged():
    m, flags = compute_massing(HOUSE, flat_hf(), flat_hf())  # DSM == DTM
    assert "no_lidar_height" in flags and m["storeys"] == 2


def test_archetype_rules():
    m = {"storeys": 2, "eaves_height_m": 5.5, "ridge_height_m": 8, "roof": {"type": "gable"}}
    assert guess_archetype(45, m, 2, {})["id"] == "terrace_redbrick"
    assert guess_archetype(70, m, 1, {})["id"] == "semi_detached"
    assert guess_archetype(90, m, 0, {})["id"] == "detached"
    assert guess_archetype(90, m, 2, {"shop": "bakery"})["id"] == "shop_terrace"
    assert guess_archetype(300, m, 0, {"building": "church"})["id"] == "church"
    tall = {"storeys": 12, "eaves_height_m": 35, "ridge_height_m": 35, "roof": {"type": "flat"}}
    assert guess_archetype(400, tall, 0, {})["id"] == "council_highrise"


def test_records_validate_against_schema():
    from test_data_schemas import validator

    fp = merge_footprints(gdf([HOUSE, box(1030, 2040, 1040, 2046)], id=["h1", "h2"]))
    recs = build_records(fp, flat_hf(), dsm_with(gable), "test", shared_wall_counts(fp))
    v = validator("building_facade.schema.json")
    for r in recs:
        errors = list(v.iter_errors(json.loads(json.dumps(r))))
        assert not errors, errors
    assert recs[0]["archetype"]["id"] == "semi_detached"


def test_cli_end_to_end_on_files(tmp_path):
    """footprints -> massing -> roads through the real CLI, file formats included."""
    from tameside_pipeline.cli import main
    from test_terrain import write_asc

    cfg = {
        "origin": {"e": 1000.0, "n": 2000.0},
        "landscape": {"resolution_m": 1.0, "z_scale": 128.0, "location_z_cm": 30000.0, "tile_quads": 99},
        "zones": {"t": {"min_e": 1000.0, "max_n": 2100.0, "tiles_x": 1, "tiles_y": 1}},
        "sources": {k: {"status": "approved"} for k in ("os_openmap_local", "os_open_roads", "ea_lidar_dtm_1m", "ea_lidar_dsm_1m")},
    }
    (tmp_path / "cfg.json").write_text(json.dumps(cfg))
    # rasters cover the zone + margin
    rr, cc = np.mgrid[0:300, 0:300]
    e, n = 900 + cc + 0.5, 2200 - rr - 0.5
    (tmp_path / "dtm").mkdir(); (tmp_path / "dsm").mkdir()
    write_asc(tmp_path / "dtm" / "dtm.asc", np.full((300, 300), 100.0), 900, 1900)
    write_asc(tmp_path / "dsm" / "dsm.asc", 100.0 + gable(e, n), 900, 1900)
    gdf([HOUSE], id=["h1"]).to_file(tmp_path / "os.gpkg", layer="Building", driver="GPKG")
    gdf([LineString([(1010, 2030), (1090, 2030)])], id=["L1"], road_function=["B Road"],
        form_of_way=["Single Carriageway"], name_1=["Old Street"], start_node=["a"], end_node=["b"]
        ).to_file(tmp_path / "roads.gpkg", layer="road_link", driver="GPKG")

    c = ["--config", str(tmp_path / "cfg.json")]
    out = tmp_path / "out"
    assert main(c + ["footprints", "--zone", "t", "--os", str(tmp_path / "os.gpkg"), "--out", str(out)]) == 0
    assert main(c + ["massing", "--zone", "t", "--footprints", str(out / "footprints.gpkg"),
                     "--dtm", str(tmp_path / "dtm"), "--dsm", str(tmp_path / "dsm"), "--out", str(out)]) == 0
    assert main(c + ["roads", "--zone", "t", "--os-roads", str(tmp_path / "roads.gpkg"), "--dtm", str(tmp_path / "dtm"), "--out", str(out)]) == 0
    rec = json.loads((out / "buildings.jsonl").read_text().splitlines()[0])
    assert rec["massing"]["roof"]["type"] == "gable" and rec["archetype"]["id"] == "detached"
    road = json.loads((out / "roads.json").read_text())["edges"][0]
    assert road["hierarchy"] == "b_road" and road["polyline"][0][2] == pytest.approx(100.0)
