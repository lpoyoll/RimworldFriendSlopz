"""Pipeline configuration loading and source licence gate."""
from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path


class SourceNotApproved(RuntimeError):
    """Raised when a stage tries to use a source that is not signed off."""


@dataclass(frozen=True)
class LandscapeSettings:
    resolution_m: float
    z_scale: float
    location_z_cm: float
    tile_quads: int


@dataclass(frozen=True)
class Zone:
    name: str
    min_e: float
    max_n: float
    tiles_x: int
    tiles_y: int


@dataclass(frozen=True)
class PipelineConfig:
    origin_e: float
    origin_n: float
    landscape: LandscapeSettings
    zones: dict[str, Zone]
    sources: dict[str, dict]

    def zone(self, name: str) -> Zone:
        if name not in self.zones:
            raise KeyError(f"Unknown zone '{name}'. Known: {', '.join(sorted(self.zones))}")
        return self.zones[name]

    def require_source(self, source_id: str) -> dict:
        """Licence gate: every stage calls this before reading a source."""
        src = self.sources.get(source_id)
        if src is None:
            raise SourceNotApproved(f"Source '{source_id}' is not in the registry.")
        if src.get("status") != "approved":
            raise SourceNotApproved(f"Source '{source_id}' has status '{src.get('status')}', not 'approved'.")
        return src


def load_config(path: str | Path) -> PipelineConfig:
    raw = json.loads(Path(path).read_text(encoding="utf-8"))
    ls = raw["landscape"]
    zones = {
        name: Zone(name, float(z["min_e"]), float(z["max_n"]), int(z["tiles_x"]), int(z["tiles_y"]))
        for name, z in raw["zones"].items()
    }
    return PipelineConfig(
        origin_e=float(raw["origin"]["e"]),
        origin_n=float(raw["origin"]["n"]),
        landscape=LandscapeSettings(
            float(ls["resolution_m"]), float(ls["z_scale"]), float(ls["location_z_cm"]), int(ls["tile_quads"])
        ),
        zones=zones,
        sources=raw.get("sources", {}),
    )
