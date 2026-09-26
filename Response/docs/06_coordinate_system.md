# 06 — Coordinate system: British National Grid to Unreal

All source data is in **EPSG:27700, OSGB36 / British National Grid** (eastings and northings in metres).
Heights are metres above **Ordnance Datum Newlyn (ODN)**. EA LiDAR is already in both. Anything in WGS84
(OSM, Mapillary) is reprojected with `pyproj`, which uses the OSTN15 grid shift for about 0.1 m accuracy.

## Mapping

Fixed world origin **O = (E 393000, N 398000)**, near Ashton town centre (`Pipeline/config/tameside.json`).

```
UE X (cm) =  (E - 393000) * 100     east  -> +X
UE Y (cm) = -(N - 398000) * 100     north -> -Y
UE Z (cm) =   H_ODN * 100           height above sea level
```

UE is left-handed with Z up, and BNG with height is right-handed, so one horizontal axis has to flip. Flipping Y keeps
"east = +X" and makes north point toward -Y, which is "up" in the UE top-down viewport. The whole borough
(about 18 × 18 km) fits within ±12 km of the origin, well inside UE5 Large World Coordinates precision.

Conversion helpers: `tameside_pipeline.coords.WorldOrigin` (Python). The same maths goes in `ResponseCore` (C++) in M1.
`python -m tameside_pipeline.cli coords --e 393900 --n 399000` gives `x=90000, y=-100000`.

**The origin must never change after content exists.** Data files store BNG, not UE units, so they are safe either way.

## Landscape heights

UE stores landscape heights as 16-bit values: `world_z_cm = LocationZ + (v - 32768) * ZScale / 128`.
We use **ZScale = 128** and **LocationZ = 30000 cm (300 m)**, which gives:
- exactly **1 cm per height step**
- a range of **-27.68 m to +627.67 m ODN** (Tameside runs from about 70 m in the Tame valley to about 540 m on the moors)

## Grid alignment

EA 1 m tiles have cell edges on whole metres, so cell centres fall on .5 m. Each landscape vertex sits on a cell
centre. The zone's north-west vertex is `(min_e + 0.5, max_n - 0.5)` and becomes the landscape Location X/Y.
Raster row 0 is north, which is also UE's minimum Y, so heightmaps need no vertical flip.

## Tiles

Each tile is `tile_quads + 1` = 2017 vertices square (2016 m). Neighbouring tiles **share** their edge row/column,
matching how landscape components share border vertices. Files are named `<zone>_x{i}_y{j}.png` for UE's tiled
heightmap import. A single `<zone>_full.png` is also written. For the Ashton slice (1 tile) that is all UE needs.

**To verify in engine (M1 task 1):** that the tiled import expects shared edges with this UE version, and that
imported heights match three EA survey spot heights to within 5 cm.
