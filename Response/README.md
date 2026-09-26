# RESPONSE — UK Policing Simulator

A grounded UK frontline policing simulator set in a 1:1 recreation of Tameside, Greater Manchester, built in Unreal Engine 5.
Players serve with the fictional **Greater Mancunia Police**.

> This folder is self-contained. It sits in the `RimworldFriendSlopz` repository for now because that is the repository this
> work session was given. It should move to its own repository before any UE content is committed (see `docs/00_repo_and_setup.md`).

| Folder | What it holds |
|---|---|
| `docs/` | Design and technical docs. Start with `docs/README.md`. |
| `schemas/` | JSON Schemas for all data the designers edit (calls, NPCs, PNC, facades). |
| `Data/` | Data that designers and modders edit: call types, examples. |
| `Pipeline/` | Python GIS/ML pipeline (Stages A–C implemented: terrain, footprints, roads, massing). |
| `Game/` | The UE5 C++ project (`Response.uproject`). |

## Quick start (pipeline)

```bash
cd Response/Pipeline
python -m venv .venv && . .venv/bin/activate
pip install -r requirements.txt
pytest
python -m tameside_pipeline.cli coords --e 393900 --n 399000   # BNG -> UE
python -m tameside_pipeline.cli terrain --config config/tameside.json --zone ashton_centre --input /path/to/ea_dtm_tiles --out build/terrain
```
