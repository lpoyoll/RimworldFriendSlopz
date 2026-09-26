# 02 — Tameside world pipeline (GIS / ML)

Goal: generate Tameside automatically (target 70–80%), then fix the rest by hand. Every run is reproducible from
`Pipeline/config/*.json`. Every source has a licence entry, and the pipeline refuses sources marked `blocked`.

> Licence notes below are my reading of the published terms. They are not legal advice. Items marked **SIGN-OFF**
> are in `05_signoff_register.md`, and I will not use them until you approve.

## Source registry

| Source | Used for | Licence | Cost | Commercial game use | Status |
|---|---|---|---|---|---|
| Environment Agency LiDAR Composite DTM / DSM, 1 m (and National LiDAR Programme point clouds) | Terrain, building heights, roof type | Open Government Licence v3 | Free | Yes, with attribution | OK |
| OS OpenMap – Local | Building footprints, water, woodland | OGL v3 (includes OS attribution) | Free | Yes, with attribution | OK |
| OS Open Roads | Road centrelines and hierarchy | OGL v3 | Free | Yes, with attribution | OK |
| OS Open Greenspace, OS Open Rivers | Parks, water | OGL v3 | Free | Yes | OK |
| OpenStreetMap | Road detail (lanes, one-way, crossings), POIs, shop types | ODbL 1.0 | Free | Yes, with attribution. Share-alike applies if we publicly distribute a *derived database* | **SIGN-OFF** S-05 |
| Microsoft Global ML Building Footprints | Fill gaps in footprints | ODbL 1.0 | Free | As OSM | **SIGN-OFF** S-05 |
| HM Land Registry INSPIRE Index Polygons | Plots, gardens, boundaries | INSPIRE / OGL-style terms with OS rights reserved | Free | Unclear for shipping derived geometry | **SIGN-OFF** S-06 |
| Mapillary street imagery | Facade analysis only (never shipped) | Images CC BY-SA 4.0; API terms | Free | Deriving facts is likely fine. Shipping images or textures is not | **SIGN-OFF** S-07 |
| Your own 360° capture drives | Facades, textures, hero reference | Yours (see privacy note) | Camera ~£400–600 | Yes, after face and plate blurring | **SIGN-OFF** S-08 |
| Getmapping or Bluesky aerial orthophoto (12.5–25 cm) | Ground detail, in-game map | Commercial licence, quote needed | Likely £ thousands for about 103 km² with game-distribution rights | Only with explicit distribution rights | **SIGN-OFF** S-09 |
| Google Maps / 3D Tiles / Street View | Human reference only | Google ToS | — | **Never** in assets, never scraped by pipeline | Blocked |

The free fallback for S-09 is OS OpenMap Local plus procedural ground materials. The in-game map would be drawn from
vector data, which also looks more like a real MDT map.

## Stage A — Terrain (implemented, see `Pipeline/tameside_pipeline/terrain.py`)

- **Input:** EA LiDAR Composite DTM 1 m GeoTIFF / ASCII grid tiles (EPSG:27700), from the DEFRA Data Services Platform.
- **Libraries:** `rasterio` (GDAL) to read and mosaic, `numpy`, `pyproj` for any non-BNG inputs.
- **Steps:** mosaic, clip to zone bbox (snapped to landscape tile size), fill nodata (EA tiles have holes over water and
  at edges) by iterative neighbour averaging, then quantise to UE 16-bit and split into World Partition tiles.
  Output: `x{i}_y{j}.png` (16-bit grayscale) plus `terrain_manifest.json` (scale, location, origin, per-tile extents).
- **UE import:** Landscape mode, Import from File, tiled heightmap; or via the `ResponseEditor` import action (M1).
  Settings come from the manifest: X/Y scale 100 (1 m), Z scale 128, location Z = +30 000 cm.
- **Coordinates:** `06_coordinate_system.md`.

## Stage B — Footprints and roads

- **Footprints:** OS OpenMap Local buildings as the base. Microsoft ML footprints only where OS has nothing (IoU < 0.1
  against any OS footprint). OSM only for attributes (building type, levels, shop names to fictionalise).
  Libraries: `geopandas`, `shapely` 2, `pyogrio`, `osmium` for the OSM extract (Geofabrik Greater Manchester).
  Merge key: stable `footprint_id` = hash of source + source ID.
- **Plots:** INSPIRE polygons if S-06 is approved. Otherwise derive plots by Voronoi split of block polygons between road
  casings and footprints.
- **Roads:** OS Open Roads centrelines (class: motorway, A, B, minor, local, access) enriched from OSM (lanes, one-way,
  width, sidewalk, crossings). Output: road graph JSON, then Houdini road HDA creates splines, UK kerbs (125 mm upstand),
  pavements, dropped kerbs, junction markings (TSRGD layouts), and a ZoneGraph for AI traffic.

## Stage C — Massing

- Height = percentile 90 of (DSM − DTM) within each footprint, eroded 0.5 m to avoid wall-edge pixels.
  Storey estimate = round((eaves height − 0.3) / 2.7).
- **Roof type:** from the EA point cloud (LAZ via `laspy` + `lazrs`). RANSAC plane fitting (`open3d`) per footprint:
  1 near-horizontal plane = flat, 2 opposed planes = gable, 3–4 = hip, more = complex. Output confidence per building.
  Low-confidence items go to Stage I review.

## Stage D — Facades (ML)

- **Imagery:** Mapillary (S-07) plus your own 360° drives (S-08). Images are processed and then discarded. Only the
  derived JSON is kept.
- **Matching:** project the camera pose onto footprint edges and pick the facade edge each image sees best (angle and distance).
- **Models:** Segment Anything 2 (Apache 2.0) for region proposals, plus a facade-parsing segmenter fine-tuned on
  CMP Facade / ECP style labels (check each dataset's licence for commercial fine-tuning; **SIGN-OFF** S-10) for
  classes: wall, window, door, shopfront, balcony, roof. Colour and material: k-means on wall pixels, plus a small
  classifier (brick red, brick buff, pebbledash, render, stone, cladding).
- **Anonymisation:** faces and plates blurred before anything is stored (`EgoBlur`-style model; check its licence).
- **Output:** one record per building (`schemas/building_facade.schema.json`).

## Stage E — Procedural build (Houdini)

- **Houdini Indie** (about £230/year, revenue cap about $100k) or Houdini FX if above that. Houdini Engine for UE is
  included with Indie. **SIGN-OFF** S-03 (paid tool).
- The HDA reads footprint + height + roof + facade JSON and picks a kit archetype (red-brick terrace, semi, pebbledash,
  render, 1960s council, stone/brick mill, modern retail, shopfront). It lays out bays, places windows and doors from the
  facade record (or the archetype default), and outputs Nanite meshes as instanced kit parts. LODs are automatic through
  Nanite; HLODs through World Partition.
- Alternative with no paid tool: UE PCG + Geometry Script. It is more work for the same result, so I recommend Houdini.

## Stage F — Ground and map

Licensed orthophoto (S-09) projected as a ground detail layer and the MDT map, or the vector fallback described above.

## Stage G — Dressing

UE PCG graphs driven by road splines and plots: lamp columns at UK spacing, bins, bollards, bus stops, wheelie bins by
tenure, parked cars by street width and time of day, vegetation from OS greenspace, litter by deprivation index
(IMD 2019, OGL).

## Stage H — Hero locations

RealityCapture (free under $1M revenue) or Gaussian-splat capture as *reference only*, then retopologised into game meshes.
Locations: Ashton market and market hall, Ashton town centre, Portland Basin, fictional police station and custody suite,
M60/M67 junctions, Hyde and Stalybridge centres. Photographing in public is lawful in the UK, but any recognisable
branding must be fictionalised.

## Stage I — Manual QA

- Editor Utility Widget heat map: per-building confidence (roof, height, facade match), shown as a coloured overlay in the level.
- One-click actions: "reclassify archetype", "override height", "mark reviewed". Overrides are saved in
  `Data/Overrides/*.json` so they survive regeneration.

## Automation estimate

| Stage | Automated | Manual |
|---|---|---|
| A Terrain | ~98% | canal edges, road cuttings |
| B Footprints/roads | ~85% | complex junctions, the M60/M67 |
| C Massing | ~80% | mills, churches, complex roofs |
| D/E Facades + build | ~60–70% | shopfronts, landmarks |
| G Dressing | ~85% | hero streets |
| **Overall** | **~75%** | |
