import math

import numpy as np
import pytest

from tameside_pipeline import facades as F


def box_m(x0, y_top, x1, y_bottom, kind, score=0.5):
    """Detection box given in metres (y measured down from the top of the crop)."""
    return {"kind": kind, "score": score, "box": [x0 * F.PX_PER_M, y_top * F.PX_PER_M, x1 * F.PX_PER_M, y_bottom * F.PX_PER_M]}


def test_parse_two_up_two_down_terrace():
    # 5 m wide, 5.5 m high facade: door + window on the ground, two windows above
    H = 5.5
    dets = [box_m(0.5, H - 2.1, 1.4, H - 0.05, "door"), box_m(2.5, H - 2.0, 4.2, H - 0.9, "window"),
            box_m(0.6, H - 4.8, 1.4, H - 3.4, "window"), box_m(2.6, H - 4.8, 4.1, H - 3.4, "window")]
    p = F.parse_facade(dets, 5.0, H, massing_storeys=2)
    assert p["storeys_seen"] == 2
    assert [b["ground"] for b in p["bays"]] == ["door", "window"]
    assert all(b["upper"] == ["window"] for b in p["bays"])
    assert sum(b["width_m"] for b in p["bays"]) == pytest.approx(5.0)


def test_car_windscreen_is_not_a_window():
    dets = [{"kind": "occluder", "score": 0.6, "box": [100, 100, 300, 200]},
            {"kind": "window", "score": 0.7, "box": [150, 110, 250, 160]},
            {"kind": "window", "score": 0.5, "box": [400, 20, 460, 80]}]
    kept = F.nms(dets)
    assert [d["kind"] for d in kept].count("window") == 1 and any(d["box"][0] == 400 for d in kept)


def test_colour_prior_favours_brick_for_red_and_render_for_pale():
    mats = list(F.MATERIAL_PROMPTS)
    assert mats[int(np.argmax(F.colour_prior("#8b4a3a")))] == "brick_red"
    assert mats[int(np.argmax(F.colour_prior("#eeeade")))] in ("render", "brick_painted")
    assert mats[int(np.argmax(F.colour_prior("#8a8c88")))] == "pebbledash"


def test_synth_bays_repeats_template_to_fit():
    tmpl = [{"width_m": 1.2, "ground": "door", "upper": ["window"]}, {"width_m": 3.8, "ground": "window", "upper": ["window"]}]
    bays = F.synth_bays(20.0, tmpl, "terrace_redbrick", 2)
    assert len(bays) == 8 and sum(b["width_m"] for b in bays) == pytest.approx(20.0, abs=0.05)
    assert [b["ground"] for b in bays[:2]] == ["door", "window"]


def test_propagation_prefers_same_building_then_neighbours_then_street():
    rec = lambda arch: {"archetype": {"id": arch}, "massing": {"storeys": 2}}
    records = {"a": rec("terrace_redbrick"), "b": rec("terrace_redbrick"), "c": rec("terrace_redbrick"), "d": rec("semi_detached")}
    obs = {("a", 0): {"edge_index": 0, "wall_material": "brick_red", "wall_colour_srgb": "#8b4a3a", "door_colour_srgb": "#1a3d7a",
                      "bays": [{"width_m": 5.0, "ground": "door", "upper": ["window"]}], "confidence": 0.8, "basis": "observed"}}
    edges = [{"footprint_id": f, "edge_index": i, "length": 5.0} for f, i in (("a", 0), ("a", 2), ("b", 0), ("c", 0), ("d", 0))]
    out = F.propagate(records, obs, edges, neighbours={"b": ["a"]}, street_of={"a": "X St", "b": "Y St", "c": "X St", "d": "Z St"})
    assert out[("a", 0)]["basis"] == "observed"
    assert out[("a", 2)]["basis"] == "inferred:same_building"
    assert out[("b", 0)]["basis"] == "inferred:neighbours" and out[("b", 0)]["wall_material"] == "brick_red"
    assert out[("c", 0)]["basis"] == "inferred:same_street_and_type"
    assert out[("d", 0)]["basis"] == "inferred:archetype_default"  # no observed semis anywhere
    assert out[("a", 0)]["confidence"] > out[("a", 2)]["confidence"] > out[("b", 0)]["confidence"] > out[("c", 0)]["confidence"]


def test_projection_and_delta_rotation():
    d = {"computed_compass_angle": 0.0}  # looking north, level
    R = F.rotation_matrix(d)
    uv = F.project(np.array([[0.0, 10.0, 0.0], [1.0, 10.0, 0.0], [0.0, -10.0, 0.0]]), R, 1000.0, 2000, 1000, {})
    assert uv[0] == pytest.approx([1000, 500])        # straight ahead -> image centre
    assert uv[1][0] > 1000                             # east of the axis -> right of centre
    assert np.isnan(uv[2]).all()                       # behind the camera
    uv2 = F.project(np.array([[0.0, 10.0, 0.0]]), F.delta_rotation(5, 0) @ R, 1000.0, 2000, 1000, {})
    assert abs(uv2[0][0] - 1000) == pytest.approx(1000 * math.tan(math.radians(5)), rel=0.01)
