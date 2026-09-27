# Generated: Ashton-under-Lyne town centre zone (2 × 2 km)

Pipeline output for the Unreal importer. Regenerate with the commands in `docs/02_gis_ml_pipeline.md` and `docs/11_streetview.md`.
Do not edit by hand; put fixes in `Data/Overrides/`.

| File | What |
|---|---|
| `terrain/ashton_centre_x0_y0.png` | 16-bit heightmap tile, 2017 × 2017, 1 m. UE settings are in `terrain/terrain_manifest.json` |
| `terrain/ashton_centre_full.png` | Same data as one file (identical to the tile for this 1-tile zone) |
| `buildings_facades.jsonl` | 2,331 buildings, one JSON per line (`schemas/building_facade.schema.json`): footprint, heights, roof, type, units, facades |
| `gltf/*.glb` | **First look in Unreal:** terrain (4 m), buildings with facades and roofs, roads and pavements, LiDAR trees. Metres from the world origin (E 393000, N 398000, 0 m ODN). Import with File → Import Into Level |
| `roads.json` | Road graph: 1,156 nodes, 1,521 links with widths, lanes, speeds, pavements, draped polylines |

Coordinates are British National Grid metres (EPSG:27700). Convert to UE with `docs/06_coordinate_system.md`.

Attribution (show in game credits):
- Contains Environment Agency information © Environment Agency and/or database right.
- Contains OS data © Crown copyright and database right (OS OpenMap Local, OS Open Roads, OS Open UPRN).
- © OpenStreetMap contributors (ODbL).
- Facade attributes derived from Mapillary contributors' street-level images (CC BY-SA 4.0). No images are stored here.
