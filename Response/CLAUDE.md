# RESPONSE (folder: PACE) — handover for local Claude Code sessions

UK policing simulator, UE 5.8, 1:1 Tameside. Fictional force: Greater Mancunia Police. The user is creative director;
Claude is technical director. Non-commercial project. Read `docs/README.md` first; decisions are in `docs/05_signoff_register.md`.

## Machine
- Windows PC, UE 5.8 at `C:\Program Files\Epic Games\UE_5.8`. Project: `Game\Response.uproject` (EngineAssociation 5.8; don't "switch version").
- Build: `& "C:\Program Files\Epic Games\UE_5.8\Engine\Build\BatchFiles\Build.bat" ResponseEditor Win64 Development -Project="<abs path>\Game\Response.uproject" -WaitMutex`
- Git: branch `claude/response-policing-simulator-m0vrn1` of lpoyoll/RimworldFriendSlopz (sparse checkout of `Response/`). Commit small; update docs in the same commit.
- PowerShell prints git progress as red "NativeCommandError" — not a real error.

## State (2026-09-27)
- C++ modules Response, ResponseCore, ResponseDispatch compile and the editor opens. Automation tests (`Response.*`) not yet run.
- Only EnhancedInput plugin enabled (MassEntity is an engine module in 5.5+; enable other plugins only when used).
- Pipeline (Python, `Pipeline/`): Stages A–D done for Ashton centre. Outputs committed in `Data/Generated/ashton_centre/`
  (terrain PNG + manifest, buildings_facades.jsonl, roads.json, gltf/*.glb). 60 pytest tests pass.
- Nothing is in the UE level yet.

## Immediate next step (user request)
Walk around Ashton as the UE5 mannequin in third person:
1. Add Third Person content pack (Content Browser → Add → Feature or Content Pack), or do it via script if possible.
2. Import `Data/Generated/ashton_centre/gltf/{terrain,buildings,roads,trees}.glb` into the level (Unreal Python:
   `unreal` module / Interchange, run via `UnrealEditor-Cmd.exe <uproject> -run=pythonscript -script=...` or Tools → Execute Python Script).
   Coordinates: metres from world origin E393000 N398000, 0 m ODN; glTF frame x=east, y=up, z=south. Check the importer's axis
   conversion and rotate if the town appears turned (UE target: X east, Y south, Z up — docs/06).
3. Collision: terrain/buildings/roads meshes → Use Complex Collision As Simple. Delete default floor (terrain is at 84–146 m).
4. GameMode override BP_ThirdPersonGameMode, Player Start near the market hall (BNG ≈ E393958 N399182).
Then: proper C++/editor importer (Landscape from terrain PNG, Nanite building kits), ground cover (Stage G).

## Secrets and data
- Mapillary token: env var `MAPILLARY_TOKEN` only. Never commit it. The client secret was posted in chat and must be rotated.
- Raw downloads (EA LiDAR WCS, OS OpenMap Local SJ+SD, OS Open Roads, OS Open UPRN, OSM API tiles, Mapillary images) are
  NOT in git; commands in docs/02 and docs/11. Tameside spans OS squares SJ and SD (split at northing 400000).
- Never use GTA mod assets (docs/05 S-15). Use DfT OGL sign images and CC0 materials (ambientCG, Poly Haven).

## Lessons already learned
- Check real data visually (screenshots / contact sheets); synthetic tests passed while real houses were sunk into the ground.
- Unreal new modules need `IMPLEMENT_MODULE` or they fail to initialise.
