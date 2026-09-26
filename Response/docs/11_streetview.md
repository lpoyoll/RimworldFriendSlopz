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

## Next
1. Download the matched images (full resolution only for matched views), rectify each facade crop using the known
   camera pose and footprint edge.
2. Segment: windows, doors, shopfronts, wall material/colour (SAM 2 for regions, plus a facade-parsing model).
3. Write facade records (`schemas/building_facade.schema.json` → `facades[]`) and run propagation.
