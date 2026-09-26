import numpy as np
import pytest

from tameside_pipeline.coords import WorldOrigin, height_range_m, height_to_u16, u16_to_height

O = WorldOrigin(393000.0, 398000.0)


def test_origin_maps_to_zero():
    assert O.bng_to_ue(393000.0, 398000.0) == (0.0, 0.0, 0.0)


def test_east_is_plus_x_north_is_minus_y():
    x, y, z = O.bng_to_ue(393010.0, 398020.0, 5.0)
    assert (x, y, z) == (1000.0, -2000.0, 500.0)


def test_round_trip():
    e, n, h = O.ue_to_bng(*O.bng_to_ue(394123.25, 399876.5, 112.34))
    assert e == pytest.approx(394123.25) and n == pytest.approx(399876.5) and h == pytest.approx(112.34)


def test_height_encoding_is_one_cm_with_project_settings():
    # z_scale 128, location 300 m -> 1 unit per cm
    v = height_to_u16(np.array([100.0, 100.01]), 128.0, 30000.0)
    assert int(v[1]) - int(v[0]) == 1
    assert u16_to_height(v, 128.0, 30000.0) == pytest.approx([100.0, 100.01])


def test_height_range_covers_tameside():
    lo, hi = height_range_m(128.0, 30000.0)
    assert lo < 0 and hi > 600  # canal level ~ 90 m, moors ~ 550 m
