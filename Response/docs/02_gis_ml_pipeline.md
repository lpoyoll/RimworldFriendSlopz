# 02 — Tameside world pipeline (GIS / ML)

Goal: generate Tameside automatically (target 70–80%), then fix the rest by hand. Every run is reproducible from
`Pipeline/config/*.json`. Every source has a licence entry, and the pipeline refuses sources marked `blocked`.

> **The project is non-commercial**, so non-commercial-only sources are allowed. The `commercial_ok` flag
> on each source tracks what would need replacing if that changes.
>
> Licence notes below are my reading of the published terms. They are not legal advice. Items marked **SIGN-OFF**
> are in `05_signoff_register.md`, and I will not use them until you approve.

## Source registry

| Source | Used for | Licence | Cost | Commercial game use | Status |
|---|---|---|---|---|---|
| Environment Agency LiDAR Composite DTM / DSM, 1 m (and National LiDAR Programme point clouds) | Terrain, building heights, roof type | Open Government Licence v3 | Free | Yes, with attribution | OK |
| OS OpenMap – Local | Building footprints, water, woodland | OGL v3 (includes OS attribution) | Free | Yes, with attribution | OK |
| OS Open Roads | Road centrelines and hierarchy | OGL v3 | Free | Yes, with attribution | OK |
| OS Open Greenspace, OS Open Rivers | Parks, water | OGL v3 | Free | Yes | OK |
| OpenStreetMap | Road detail (lanes, one-way, crossings), POIs, shop types | ODbL 1.0 | Free | Yes, with attribution. Share-alike applies if we publicly distribute a *derived database* | Approved |
| Microsoft Global ML Building Footprints | Fill gaps in footprints | ODbL 1.0 | Free | As OSM | Approved |
| HM Land Registry INSPIRE Index Polygons | Plots, gardens, boundaries | INSPIRE / OGL-style terms with OS rights reserved | Free | Unclear for shipping derived geometry | Approved (non-commercial) |
| Mapillary street imagery | Facade analysis only (never shipped) | Images CC BY-SA 4.0; API terms | Free | Deriving facts is likely fine. Shipping images or textures is not | Approved (non-commercial) |
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

## Stage B — Footprints and roads (implemented: `footprints.py`, `roads.py`)

**Buildings** (`cli footprints`):
- OS OpenMap Local buildings are the base geometry. Each is repaired (`make_valid`), split into single polygons,
  simplified 0.2 m and oriented, and anything under 6 m² is dropped.
- Microsoft ML footprints are added only where no OS footprint overlaps them (IoU < 0.1 and < 30% of the MS polygon covered).
- OSM (read straight from the Geofabrik `.osm.pbf` through GDAL; no osmium needed) contributes **attributes only**:
  building type, levels, height, roof shape, name, shop/amenity, address. It is matched by largest overlap, which must cover ≥ 50% of the footprint.
- INSPIRE plots: each footprint gets the plot containing its representative point.
- Stable IDs: `fp_` + hash(source, source ID[, part]). Microsoft footprints have no ID, so their rounded centroid is used.
  Every footprint lists its `sources`.
- Output: `footprints.gpkg`.

**Roads** (`cli roads`):
- OS Open Roads `road_link` is the network. `road_function` sets the hierarchy and default lanes, lane width, speed and
  pavements. `form_of_way` handles dual carriageways, slip roads and roundabouts.
- The OSM way with the most length inside an 8 m buffer supplies lanes, one-way (direction corrected against the OS
  digitising direction), maxspeed, width and sidewalks. Any value still at its UK default is listed in `assumed`, so QA
  can tell measured from guessed. Roundabout and slip-road direction without OSM is marked `forward_unverified`.
- Polylines are densified to 5 m and draped on the DTM (z in metres ODN). UK kerb upstand is 125 mm; default pavement is 2 m.
- Street names: `name` (real) and `name_altered` (a deterministic fictional first word, same suffix) are both exported.
  The game setting chooses which one to show (S-13).
- Output: `roads.json`, a node/edge graph. Nodes are marked junction, through or dead-end. The Houdini road HDA
  (kerbs, markings, TSRGD junction layouts) and the ZoneGraph for AI traffic read this file.

## Stage C — Massing (implemented: `massing.py`)

- **Ground:** `ground_z` is the median DTM inside the footprint. `ground_z_min` is the 5th percentile of the ground in a
  0.5–2 m ring outside it, so buildings on Tameside's slopes can be based at their lowest doorstep and never float.
- **Ridge:** 95th percentile of nDSM (DSM − DTM) inside the footprint eroded by 0.5 m.
- **Eaves:** each cell in the 1.5 m band inside the eroded edge is projected out to the wall along its own slope
  (`nDSM − slope × distance`). The 25th percentile of those is the eaves height. Tested exact on a synthetic 45° gable.
- **Storeys:** round((eaves − 0.3) / 2.7).
- **Roof type (raster method):** cells steeper than 12° are pitched, and fewer than 30% pitched means flat. The downhill
  directions are reduced to one fall axis (double-angle mean), which also gives the ridge bearing. Slopes on both sides
  of the axis make a gable; add slopes along the axis and it is a hip; one side only is mono-pitch; anything else is complex.
  Confidence is based on how dominant the pattern is and how many cells there are.
  *Upgrade path:* RANSAC plane fitting on the EA LAZ point cloud (`laspy`, `open3d`) for buildings whose raster
  confidence is below 0.5.
- **Archetype (first guess):** from OSM tags, area, height, roof type and shared-wall count (2 = terrace, 1 = semi,
  0 = detached): church, civic, high-rise, mill, industrial shed, retail, shop terrace, 1960s council, terrace, semi, detached.
  Each guess lists its `basis`. Stage D will refine materials (for example pebbledash or render).
- **QA flags:** `no_lidar_height`, `low_roof_confidence`, `low_archetype_confidence`, `height_outlier`,
  `tall_roof_check_mansard_or_tower`. These drive the Stage I heat map.
- Output: `buildings.jsonl`, one `building_facade.schema.json` record per line, with `facades` left empty until Stage D.

### Running Stages B and C for Ashton

Downloads (all free): OS OpenMap Local (GeoPackage, tile SJ99 plus neighbours), OS Open Roads (GeoPackage),
Geofabrik `greater-manchester-latest.osm.pbf`, Microsoft footprints (UK GeoJSON), HMLR INSPIRE (Tameside),
and EA LiDAR Composite **DTM and DSM** 1 m for the zone.

```bash
python -m tameside_pipeline.cli footprints --zone ashton_centre --os data/opmplc_gb.gpkg --ms data/ms_uk.geojson \
    --osm data/greater-manchester-latest.osm.pbf --inspire data/Tameside_INSPIRE.gml --out build/ashton
python -m tameside_pipeline.cli massing --zone ashton_centre --footprints build/ashton/footprints.gpkg \
    --dtm data/dtm --dsm data/dsm --out build/ashton
python -m tameside_pipeline.cli roads --zone ashton_centre --os-roads data/oproad_gb.gpkg \
    --osm data/greater-manchester-latest.osm.pbf --dtm data/dtm --out build/ashton
```

## Stage D — Facades (ML)

- **Imagery:** Mapillary (S-07) plus your own 360° drives (S-08). Images are processed and then discarded. Only the
  derived JSON is kept.
- **Matching:** project the camera pose onto footprint edges and pick the facade edge each image sees best (angle and distance).
- **Models:** Segment Anything 2 (Apache 2.0) for region proposals, plus a facade-parsing segmenter fine-tuned on
  CMP Facade / ECP style labels (research licences are fine while non-commercial, S-10 approved) for
  classes: wall, window, door, shopfront, balcony, roof. Colour and material: k-means on wall pixels, plus a small
  classifier (brick red, brick buff, pebbledash, render, stone, cladding).
- **Anonymisation:** faces and plates blurred before anything is stored (`EgoBlur`-style model; check its licence).
- **Output:** one record per building (`schemas/building_facade.schema.json`).

## Stage E — Procedural build (Houdini)

- **Houdini Apprentice** (free, non-commercial; approved S-03), with Houdini Engine for UE. Files are `.hipnc`/`.hdanc`.
  Keep HDAs simple and well documented: if the project ever goes commercial, they must be rebuilt in Indie/FX.
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
