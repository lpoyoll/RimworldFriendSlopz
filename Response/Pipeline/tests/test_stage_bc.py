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
    assert 7.0 < m["ridge_height_m"] <= 8.0 and m["eaves_height_m"] == pytest.approx(5.0, abs=0.5)
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


# ---------------------------------------------------------------- lessons from the first real run (Hurst Cross)

def test_3d_os_geometries_are_flattened():
    from shapely.geometry import Polygon as P3
    fp = merge_footprints(gdf([P3([(1010, 2010, 0), (1015, 2010, 0), (1015, 2020, 0), (1010, 2020, 0)])], ID=["a"]), os_id_field="ID")
    assert not fp.geometry.iloc[0].has_z


def test_empty_zone_does_not_crash():
    assert len(merge_footprints(gdf([], id=[]))) == 0


def test_semi_pair_merged_by_os_is_split_into_two_units():
    from tameside_pipeline.footprints import attach_address_counts, attach_osm_attributes, split_units
    from tameside_pipeline.massing import count_units

    pair = box(1020, 2040, 1036, 2048)  # OS: one polygon for both houses
    fp = merge_footprints(gdf([pair], id=["p"]))
    osm = gdf([box(1020, 2040, 1028, 2048), box(1028, 2040, 1036, 2048)], building=["semidetached_house"] * 2)
    fp = attach_osm_attributes(fp, osm)
    assert fp.osm_units[0] == 2 and fp.osm[0]["building"] == "semidetached_house"
    pts = gpd.GeoDataFrame(geometry=gpd.points_from_xy([1024, 1032, 1100], [2044, 2044, 2044]), crs=CRS)
    fp = attach_address_counts(fp, pts)
    assert fp.addresses[0] == 2
    assert count_units(2, 2, fp.osm[0], pair.area) == (2, "osm:units")
    halves = split_units(pair, 2)
    assert len(halves) == 2 and all(h.area == pytest.approx(64, rel=0.01) for h in halves)


def test_record_has_units_and_semi_archetype():
    from test_data_schemas import validator
    from tameside_pipeline.footprints import attach_address_counts

    pair = box(1020, 2040, 1036, 2046)
    fp = merge_footprints(gdf([pair], id=["p"]))
    fp["osm_units"] = 0
    fp = attach_address_counts(fp, gpd.GeoDataFrame(geometry=gpd.points_from_xy([1024, 1032], [2043, 2043]), crs=CRS))

    def pair_gable(e, n):
        inside = (e > 1020) & (e < 1036) & (n > 2040) & (n < 2046)
        return np.where(inside, 8.0 - np.abs(n - 2043.0), 0.0)
    rec = build_records(fp, flat_hf(), dsm_with(pair_gable), "t", [0])[0]
    assert rec["archetype"]["id"] == "semi_detached" and rec["units"]["count"] == 2 and len(rec["units"]["outlines"]) == 2
    assert not list(validator("building_facade.schema.json").iter_errors(json.loads(json.dumps(rec))))


def test_preview_scene_builds():
    from tameside_pipeline.preview import build_scene, write_html

    fp = merge_footprints(gdf([HOUSE], id=["h"]))
    fp["osm_units"] = 0
    recs = build_records(fp, flat_hf(), dsm_with(gable), "t", [0])
    roads = build_road_graph(gdf([LineString([(1010, 2030), (1090, 2030)])], id=["L"], road_function=["B Road"],
                                 form_of_way=["Single Carriageway"], name_1=["Old Street"]), None, flat_hf())
    scene = build_scene(recs, roads, flat_hf(), dsm_with(gable), (1050.0, 2050.0), 40.0, ["Old Street"])
    assert scene["walls"]["pos"] and scene["roofs"]["pos"] and scene["roads"]["pos"] and scene["labels"][0]["text"] == "Old Street"
    assert scene["terrain"]["nx"] == 41


def test_semi_with_rear_extension_is_not_sunk():
    """Regression (Hurst Cross): hipped 1930s semi pair whose OS outline includes a single-storey rear extension."""
    def semi(e, n):
        main = (e > 1020) & (e < 1036) & (n > 2040) & (n < 2048)
        ext = (e > 1022) & (e < 1034) & (n > 2036) & (n <= 2040)  # 4 m deep flat-roof extension, 3 m high
        d = np.minimum.reduce([n - 2040, 2048 - n, e - 1020, 1036 - e])
        roof = 5.2 + np.minimum(d, 4.0) * 0.75  # 37 deg hip, eaves 5.2, ridge 8.2
        return np.where(main, roof, np.where(ext, 3.0, 0.0))
    outline = Polygon([(1020, 2040), (1022, 2040), (1022, 2036), (1034, 2036), (1034, 2040), (1036, 2040), (1036, 2048), (1020, 2048)])
    m, flags = compute_massing(outline, flat_hf(), dsm_with(semi))
    assert m["roof"]["type"] == "hip"
    assert m["eaves_height_m"] == pytest.approx(5.2, abs=0.6)
    assert m["storeys"] == 2


# ---------------------------------------------------------------- lessons from the Ashton town centre run

M2 = {"storeys": 2, "eaves_height_m": 5.2, "ridge_height_m": 8.5, "roof": {"type": "gable"}}


def test_merged_victorian_terrace_is_not_flats():
    # 10 houses of ~50 m2 merged into one 500 m2 OS outline, 10 addresses
    assert guess_archetype(500, M2, 0, {}, units=10)["id"] == "terrace_redbrick"


def test_flats_block_has_more_units_than_houses_could_fit():
    assert guess_archetype(300, {**M2, "storeys": 3}, 0, {}, units=12)["id"] == "council_1960s"


def test_market_and_shop_points():
    assert guess_archetype(2000, M2, 0, {"building": "retail", "amenity": "marketplace"}, units=40)["id"] == "civic"
    assert guess_archetype(120, M2, 2, {}, units=3, shops=2)["id"] == "shop_terrace"


def test_big_irregular_block_gets_complex_roof():
    fn = lambda e, n: np.where((e > 1010) & (e < 1050) & (n > 2010) & (n < 2040), 9.0 - 0.3 * np.abs(n - 2025), 0.0)
    m, _ = compute_massing(box(1010, 2010, 1050, 2040), flat_hf(), dsm_with(fn))  # 1200 m2
    assert m["roof"]["type"] == "complex"


def test_terrace_row_with_rear_outriggers_is_gable():
    """Row of 6 gable-ended terraced houses (ridge E-W) with pitched rear outriggers running N-S."""
    def row(e, n):
        main = (e > 1010) & (e < 1040) & (n > 2040) & (n < 2048)
        roof = 8.5 - 0.8 * np.abs(n - 2044)  # ridge along the row
        out = np.zeros_like(e)
        for k in range(6):
            x0 = 1011 + 5 * k
            o = (e > x0) & (e < x0 + 3) & (n > 2034) & (n <= 2040)
            out = np.where(o, 6.0 - 0.9 * np.abs(e - (x0 + 1.5)), out)  # outrigger ridge N-S
        return np.where(main, roof, out)
    from shapely.ops import unary_union
    outline = unary_union([box(1010, 2040, 1040, 2048)] + [box(1011 + 5 * k, 2034, 1014 + 5 * k, 2040) for k in range(6)])
    m, _ = compute_massing(outline, flat_hf(), dsm_with(row))
    assert m["roof"]["type"] == "gable", m["roof"]
