"""British National Grid (EPSG:27700) <-> Unreal Engine world coordinates.

Convention (see docs/06_coordinate_system.md):
  UE X (cm) =  (E - origin_e) * 100      east is +X
  UE Y (cm) = -(N - origin_n) * 100      north is -Y (UE is left-handed, Z up)
  UE Z (cm) =  H_odn * 100               height above Ordnance Datum Newlyn
"""
from __future__ import annotations

from dataclasses import dataclass

CM_PER_M = 100.0


@dataclass(frozen=True)
class WorldOrigin:
    e: float
    n: float

    def bng_to_ue(self, e: float, n: float, h: float = 0.0) -> tuple[float, float, float]:
        return ((e - self.e) * CM_PER_M, -(n - self.n) * CM_PER_M, h * CM_PER_M)

    def ue_to_bng(self, x: float, y: float, z: float = 0.0) -> tuple[float, float, float]:
        return (x / CM_PER_M + self.e, -y / CM_PER_M + self.n, z / CM_PER_M)


def height_to_u16(h_m, z_scale: float, location_z_cm: float):
    """Height (m, ODN) -> UE landscape 16-bit value.

    UE: world_z_cm = location_z_cm + (v - 32768) * z_scale / 128
    Works on floats or numpy arrays. Values are clamped to 0..65535.
    """
    import numpy as np

    v = (np.asarray(h_m, dtype=np.float64) * CM_PER_M - location_z_cm) * 128.0 / z_scale + 32768.0
    return np.clip(np.rint(v), 0, 65535).astype(np.uint16)


def u16_to_height(v, z_scale: float, location_z_cm: float):
    import numpy as np

    return (location_z_cm + (np.asarray(v, dtype=np.float64) - 32768.0) * z_scale / 128.0) / CM_PER_M


def height_range_m(z_scale: float, location_z_cm: float) -> tuple[float, float]:
    lo = (location_z_cm + (0 - 32768.0) * z_scale / 128.0) / CM_PER_M
    hi = (location_z_cm + (65535 - 32768.0) * z_scale / 128.0) / CM_PER_M
    return lo, hi
