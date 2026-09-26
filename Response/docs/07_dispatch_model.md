# 07 — Dispatch data model (M0/M1)

Code: `Game/Source/ResponseDispatch`. Data: `Data/CallTypes/*.json`.

## Types (`DispatchTypes.h`)
- `FCallTypeDefinition` (DataTable row): mirrors `call_type.schema.json`.
- `FIncident`: mirrors `incident.schema.json`. Reference, grade + grade history, status, target time, location, flags,
  assigned units, and a full timestamped log.
- `FDispatchUnit`: a call sign (for example `TA21`), status, location, skills, and whether it is the player.

## Rules (`DispatchRules.h`, pure functions, automation-tested)
- `GradeCall`: the first grading rule whose flags match wins; otherwise the call type's default grade.
- `TargetAttendBy`: G1 15 min urban / 20 min rural, G2 60 min, G3 and G4 no live target (all in project settings).
- `CallWeight`: demand weight by hour, weekend and rain.
- `MakeReference`: Storm-style `NNNN-ddMMyy` log number, resetting daily.
- `QueueLess`: grade first, then soonest target, then oldest.

## Subsystem (`UDispatchSubsystem`)
- Loads every `*.json` in `Data/CallTypes` at start. Later files override earlier ones with the same id, so mods can
  replace or add call types without code.
- `CreateIncident` grades, sets the target, queues, and logs. G4 is resolved immediately without deployment.
- `Regrade`, `AddLog`, `SetIncidentStatus`, `CloseIncident`, `AssignUnit`, `SelfDeploy`, `Divert`: every change is written
  to the incident log with author and game time. The debrief, BWV and complaints read from that log.
- Breach check: when a G1/G2 target passes with nobody at scene, it is logged and `OnTargetBreached` fires.
- **AI dispatcher** (`bAutoDispatch`): walks the queue in priority order. It first offers the job to the player if they
  are available and within `PlayerOfferRadiusMetres` (the offer expires after `PlayerOfferSeconds`, or the player can
  decline). Otherwise it sends the nearest available AI unit that has the required skills. G3 is left for appointment booking.
  Because the dispatcher only uses the public API, a human player-dispatcher can replace it later.

## Next (M1)
- Move the game clock into `ResponseCore`, and write incident log entries to the shared event log as well.
- Travel-time estimate from the road graph instead of straight-line distance.
- Unit resourcing by shift pattern (earlies/lates/nights), refreshment breaks, and cross-border assistance.
- Save/load of incidents; JSON export matching `incident.schema.json`.
