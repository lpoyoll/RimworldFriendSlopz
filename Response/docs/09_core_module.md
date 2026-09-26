# 09 — ResponseCore (shared foundation)

Code: `Game/Source/ResponseCore`. Every gameplay module depends on it, and it depends on nothing of ours.

| Piece | Type | What it does |
|---|---|---|
| Settings | `UResponseCoreSettings` | World origin (must match `Pipeline/config/tameside.json`), lat/lon for the sun, world seed, default time scale, street-name mode (real/altered). |
| IDs | `FResponseId` | `kind_` + 16 hex, e.g. `npc_…`. `Make(kind, seed, index)` is deterministic, so a record is regenerated identically on first lookup. |
| Randomness | `FResponseRandom` | xoshiro256** with one named stream per system (`ForSystem(seed, "Dispatch")`), so changes in one system never reshuffle another. |
| Geo | `ResponseGeo` | BNG ↔ UE, identical to the Python pipeline (`docs/06`). |
| Time | `ResponseTime` | UK civil time (GMT/BST switch at 01:00 UTC on the last Sundays of March/October), NOAA sunrise/sunset and sun position for Ashton, and the weekend-demand window (Fri 18:00 to Sun). |
| Clock | `UResponseClockSubsystem` | The one game clock (UK local time). Time scale and pause, hourly and daylight events, and shift lookup (earlies 07–17, lates 14–00, nights 22–07). Feeds the sun for lighting. |
| Event log | `UResponseEventLog` | Append-only record of everything meaningful: type (GameplayTag), actor, subject, incident, location, text, data. Query by type/incident/actor/time, export JSON (`schemas/event.schema.json`). |
| Saves | `UResponseSaveSubsystem` + `IResponseSaveParticipant` | Systems register and write their own JSON block, all in one file `Saved/SaveGames/<slot>.json`. Readable, diffable, moddable, versioned. |

## Integration done
- `UDispatchSubsystem` now takes time from the clock, and mirrors every Storm log line into the event log as
  `Event.Dispatch.*`. It saves and loads incidents, units and sequence counters.
- Clock, event log and dispatch all register with the save subsystem.

## Tests (UE automation, `Response.Core.*`)
IDs (determinism, parse), RNG (stream independence, weighted choice), geo (same numbers as the Python tests),
time (BST boundaries, Manchester sunrise/sunset within 3 min, solar-noon elevation and azimuth), event JSON round trip.

**Not yet compiled:** there is no Unreal Engine in the cloud environment where this was written. The first build on the
dev PC is the real check. Expect a few small compile fixes.
