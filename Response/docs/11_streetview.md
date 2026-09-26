# 11 — Street-level imagery (Stage D input)

## Decisions (2026-09-26)
- **Mapillary is the only street-level source.** The creative director will not do capture drives (S-08 declined).
- **Token handling:** the Mapillary token lives only in `MAPILLARY_TOKEN` (for example `~/.config/response/secrets.env`,
  chmod 600). It is never written to the repo; `.gitignore` blocks `*.env`, `secrets*` and `imagery/`. Only the
  client **access token** is used. The client secret is not needed by the pipeline and should be rotated in the
  Mapillary dashboard because it was shared in chat.
- Images are analysed and then discarded. Only derived facade attributes are kept (licence: CC BY-SA 4.0; contributors credited in-game).

## Coverage: Ashton centre zone (2 × 2 km)
4,148 images, mostly 2025–2026, **no panoramas**. Nearly all are forward-facing dashcam or action-cam frames
(3840×2160 dominant). Dense through the town centre, patchy in the south-east (Dukinfield side).

## Matching (`cli streetview`, `streetview.py`)
For each **street-facing** footprint edge (a road lies within 20 m in front of it), keep the best 3 images that:
are 4–40 m away, sit in front of the facade, have it inside the camera's field of view (100°), view it at less than 78°
from square-on, and have a clear line of sight (no other footprint in between).

| Settings | Facades seen | Buildings with a seen facade |
|---|---|---|
| 65° oblique, 70° FOV (first try) | 479 / 4,034 (12%) | 349 |
| **78°, 100° (default, fits dashcam)** | 803 / 4,034 (20%) | 513 |
| 80°, 120° | 863 (21%) | 531 |

## Consequence: observed vs inferred facades
Direct views cover about 20% of street-facing facades. Ashton's streets are very repetitive (terrace rows and semi
streets share one style), so the facade stage will produce:
- **observed** attributes for seen facades (from segmentation of the matched images), and
- **inferred** attributes for the rest, propagated from observed neighbours in the same row, then the same street,
  then the same archetype in the area, each with a lower confidence and `basis` recorded.
QA (Stage I) shows observed/inferred on the heat map.

## Facade extraction (`cli facades`, `facades.py`)
Per seen facade (up to 3 candidate images):
1. **Rectify.** Project the facade quad (footprint edge × ground..eaves from Stage C) into each image using the
   Mapillary SfM pose (`computed_rotation`, `camera_parameters`, camera 1.4 m above the DTM). Warp it to a front-on
   crop at 40 px/m.
2. **Is it really the facade?** CLIP zero-shot: "front of a house / shop front / facade with windows" against
   car, road, sky, tree, garden wall, hedge. The best candidate must reach 0.5 or the facade stays unobserved.
3. **Pose refinement.** Mapillary rotations drift a few degrees, so small yaw (±4°) and pitch (±2°) corrections are
   tried, and the most facade-like crop wins, with a penalty for larger corrections.
4. **Detect** (OWLv2 open-vocabulary): window, door, shop front, garage door, plus car, van, tree as occluders.
   Element boxes inside an occluder are dropped (a car windscreen is not a window). Views more than 40% occluded are rejected.
5. **Parse:** storeys from window rows, bays from element columns (door / window / bay window / shopfront /
   garage on the ground floor, windows or blank above), door colour, wall colour (median of wall-only pixels away from
   crop edges), and wall material from CLIP **weighted by a colour prior** (saturated red → brick, pale → render or
   painted, mid-grey → pebbledash). CLIP alone confused brick and render on dashcam crops.
6. Views with under 15 real image px per metre give colour and material only. Their bay layout is marked inferred.

**Unseen facades** are filled in order: same building → shared-wall neighbours of the same type → same street and
type → area average for the type → type default. Door colours are varied along a street by sampling observed doors.
Every facade records `basis` and `confidence`.

### Tuning history (first 40–60 facades, checked by eye on contact sheets)
| Change | Effect |
|---|---|
| Naive: first view, no checks | Garden walls, cars and road accepted as facades |
| + CLIP facade check, occlusion gate | Garden-wall and road crops rejected |
| + pose refinement (±6°/±4°, weak penalty) | Some fixed, but some tilted up to include roofs (CLIP prefers "whole house") |
| + tighter refinement (±4°/±2°, stronger penalty), colour-weighted material, edge-trimmed wall sampling | About 6 of 8 checked facades correct on material; doors and storeys sensible |

Known limits: pose error is the main source of misalignment. Tiny corner walls can still be picked, so confidence
now scales with facade width. Material classes are coarse (no sandstone versus gritstone split yet).
