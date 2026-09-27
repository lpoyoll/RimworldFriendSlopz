# 00 — Repository structure and UE5 project setup

## Target layout

```
Response/
  docs/                      design + tech docs (Markdown, reviewed like code)
  schemas/                   JSON Schema (draft 2020-12) for all editable data
  Data/                      source-of-truth designer data (JSON), imported into UE DataTables
    CallTypes/  Offences/  NPCArchetypes/  Dialogue/  Kits/
  Pipeline/                  Python GIS/ML pipeline
    tameside_pipeline/       package: coords, terrain, footprints, roads, massing, facades...
    config/                  zone definitions, source registry (with licence per source)
    tests/
  Houdini/                   HDAs for building assembly, roads, PCG helpers (.hda in LFS)
  Game/                      UE5 project
    Response.uproject
    Config/
    Source/
      Response/              primary game module (game mode, player, glue)
      ResponseCore/          shared types, IDs, time, save system, data registry
      ResponseDispatch/      incident model, grading, queue, AI dispatcher   <- built now
      ResponseRadio/         Airwave-style talkgroups, call signs, radio UI
      ResponseChecks/        PNC/PND/ANPR/MOT/insurance record generation + queries
      ResponseForce/         GAS abilities for use of force, NDM scoring hooks
      ResponseCustody/       arrest, caution, custody booking, PEACE interview, CPS
      ResponseEvidence/      BWV, exhibits, statements, crime reports
      ResponseConsequence/   complaints, PSD/IOPC, court, community trust
      ResponseCity/          Mass Entity citizens, routines, crime emergence
      ResponseVehicles/      Chaos vehicle fleet, pursuits, TPAC
      ResponseCareer/        progression, tutor, specialisms
      ResponseWorld/         time of day, weather, zone streaming helpers
      ResponseEditor/        editor-only: pipeline importers, QA heat map, validators
    Plugins/                 third-party or isolated plugins
    Content/                 UE assets (Git LFS or Perforce, see below)
```

Each gameplay system is its own C++ module so dependencies stay one-directional (see `01_architecture.md`).
Modules are added when their milestone starts, not before. Only `Response` and `ResponseDispatch` exist today.

## UE5 project setup

- **Engine:** latest stable UE5 at project creation, then pinned. Engine upgrades happen only at milestone boundaries.
- **Template:** blank C++ project, no starter content.
- **Enabled plugins (target set; only EnhancedInput is enabled today, the rest are enabled when their milestone starts. Note: from UE 5.5, MassEntity is an engine module, not a plugin):** World Partition (default for new levels), PCG, MassEntity, MassGameplay, StateTree, ChaosVehicles,
  EnhancedInput, GameplayAbilities, GameplayTags, CommonUI, Water (canals, Portland Basin), Houdini Engine (editor only),
  MetaHuman (when characters start).
- **Rendering:** Lumen GI and reflections, Nanite for buildings and street furniture, Virtual Shadow Maps, TSR.
  A 3070-class card at 1440p will need Lumen on its software path at the "High" scalability tier; this is the main
  performance risk (see `01_architecture.md`, Performance).
- **World:** one World Partition level per zone set, with Large World Coordinates (on by default). The level origin is
  the fixed BNG point in `06_coordinate_system.md`. World Partition runtime grids: `Main` (128 m cells), `Landscape` (512 m), `HLOD`.
- **Units:** 1 UE unit = 1 cm, as UE expects.

## Version control: recommendation

**Recommendation: Perforce (Helix Core) for the UE project; Git for code, pipeline, data and docs.** Needs your sign-off (S-01).

Why:
- A 1:1 borough produces hundreds of GB of binary assets (landscape layers, meshes, textures, World Partition actor files).
  Git LFS works, but hosted LFS storage and bandwidth get expensive and slow at that size.
- `.uasset` and `.umap` files cannot be merged. Perforce exclusive checkout stops two people editing the same asset.
  Git LFS has locking, but it is opt-in and easy to forget.
- UE's One File Per Actor (World Partition) creates tens of thousands of small files. Perforce handles that well.
- Helix Core is free for up to 5 users and 20 workspaces. You would need a server: a small cloud VM (roughly £15–40/month)
  or a spare machine at home.

Alternative with no new tools: Git + Git LFS on GitHub. Fine while solo and while `Content/` stays under about 10 GB.
GitHub charges for LFS data packs above the free quota. `Game/.gitattributes` is already set up for LFS, so we can start
on Git and move to Perforce when the pipeline starts producing large content.

Either way, this folder should move out of `RimworldFriendSlopz` into its own repository (for example `response-game`)
before any content is committed (S-02).

## Branching and commits

- `main` always opens and builds. Features go on short branches.
- Small commits. Each commit that changes behaviour updates the relevant doc.
- Pipeline outputs (`Pipeline/build/`) are never committed. They are rebuilt from the source registry.
