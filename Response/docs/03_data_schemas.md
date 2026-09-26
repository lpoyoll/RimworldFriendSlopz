# 03 — Data schemas

All schemas are JSON Schema draft 2020-12 in `/schemas`. Every file under `/Data` is validated in CI
(`Pipeline/tests/test_data_schemas.py`), so a designer or modder gets a clear error before the game ever loads bad data.

| Schema | Used by | Example |
|---|---|---|
| `call_type.schema.json` | Designers: what calls exist, how often, how they are graded | `Data/CallTypes/core_call_types.json` (the 10 M1 call types) |
| `incident.schema.json` | Runtime Storm-style log. Saved, exported for debrief, complaints, court | `Data/Examples/incident.example.json` |
| `npc_profile.schema.json` | Persistent citizen: identity, household, routine, traits, vulnerabilities, OCG role, memory of police contact | `Data/Examples/npc_profile.example.json` |
| `pnc_record.schema.json` | PNC-style person or vehicle record: warning signals, wanted/missing, disqualification, convictions, orders, PND intel, insurance/MOT/tax | `Data/Examples/pnc_person.example.json` |
| `building_facade.schema.json` | Pipeline output per footprint (Stages B–D), input to Houdini (Stage E) | `Data/Examples/building_facade.example.json` |
| `common.schema.json` | Shared: IDs, BNG position, game time, grade, sensitivity | — |

## Design notes

- **IDs** are `prefix_` plus 16 hex digits (`npc_`, `veh_`, `adr_`, `inc_`, `pnc_`). They come from a seeded 64-bit hash,
  so the same seed makes the same city. Records are generated lazily the first time a player checks them, then saved.
- **Positions** in data are British National Grid metres, never UE units. That keeps data valid if the world origin
  ever moves, and lets the pipeline and the game share it.
- **Call grading** is data: an ordered list of `{if_any: [flags], grade}` rules per call type. The caller's reported flags
  are rolled from `detail_flags` probabilities. The AI dispatcher and a future human dispatcher use the same rules.
- **Demand** = `base_weight × hourly_modifier[hour] × weekend_modifier × rain_modifier`. Weekend means Friday 18:00 to Sunday.
  Starting numbers are my estimates. We should tune them against published police demand profiles (for example, College of
  Policing and HMICFRS demand reports) before M1 ships.
- **PNC warning-signal codes** are modelled on the real categories, but all records, formats and PNC IDs are fictional.
  Generated plates use current UK format. Before release we should filter out plates that exist in the real world (flagged S-11).
- **Sensitivity** is on both call types and incidents, so the content settings can filter or summarise them.
- **Facade records** list their `sources`, so every building can be traced back for licence audit and attribution.
