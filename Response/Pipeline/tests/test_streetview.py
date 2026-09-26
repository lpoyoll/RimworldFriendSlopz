from shapely.geometry import LineString, Polygon, box

from tameside_pipeline.streetview import facade_edges, match_views


def bld(fid, poly):
    return {"footprint_id": fid, "footprint": {"outer": [list(c) for c in list(poly.exterior.coords)[:-1]]}}


HOUSE = bld("fp_a", box(0, 10, 10, 20))  # south facade on y=10, street along y=0
ROAD = [LineString([(-50, 0), (50, 0)])]


def img(i, e, n, heading=None, pano=False):
    return {"id": str(i), "e": e, "n": n, "heading": heading, "pano": pano, "captured_at": 1}


def run(images, extra=()):
    blds = [HOUSE, *extra]
    edges = facade_edges(blds, ROAD)
    obs = {"geoms": [Polygon(b["footprint"]["outer"]) for b in blds], "ids": [b["footprint_id"] for b in blds]}
    return {(m["footprint_id"], tuple(m["normal"])): m for m in match_views(edges, images, obs)}


def south(res):
    return res[("fp_a", (0.0, -1.0))]


def test_only_street_facing_edges():
    res = run([])
    assert list(res) == [("fp_a", (0.0, -1.0))]  # north/east/west walls do not face the road


def test_camera_in_front_looking_at_facade():
    res = run([img(1, 5, 0, heading=0)])  # on the road, looking north
    assert [v["image_id"] for v in south(res)["views"]] == ["1"]


def test_camera_looking_away_or_behind_or_far():
    assert not south(run([img(1, 5, 0, heading=180)]))["views"]   # looking south
    assert not south(run([img(1, 5, 30, heading=180)]))["views"]  # behind the house
    assert not south(run([img(1, 5, -45, heading=0)]))["views"]   # 55 m away


def test_panorama_sees_regardless_of_heading():
    assert south(run([img(1, 5, 0, heading=180, pano=True)]))["views"]


def test_blocked_by_another_building():
    shed = bld("fp_b", box(3, 4, 7, 6))  # between road and facade
    assert not south(run([img(1, 5, 0, heading=0)], extra=[shed]))["views"]


def test_best_view_ranked_first():
    res = run([img("far", 5, -20, heading=0), img("near", 5, 2, heading=0), img("oblique", 25, 2, heading=315)])
    assert south(res)["views"][0]["image_id"] == "near"
