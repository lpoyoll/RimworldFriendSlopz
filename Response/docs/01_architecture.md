# 01 — Technical architecture

## Principles

1. **Simulation first, presentation second.** Every system has a plain C++ model (structs + subsystems) that can run
   without actors. The actors, UI and audio read from that model. This lets us test the model headless and later
   run a player-dispatcher on the same data.
2. **Data-driven.** Content lives in `Data/*.json` and is validated against `schemas/`. At editor time it is imported
   into UE DataTables and DataAssets. Mods add JSON files. Code never hard-codes a call type, offence or marker.
3. **Stable IDs.** Every persistent entity (NPC, vehicle, address, incident, exhibit) has a 64-bit ID (`FResponseId`)
   generated from a seed. Records are generated lazily on first query and then persisted.
4. **One-way dependencies.** Lower layers never include higher ones. Cross-layer communication goes through
   GameplayTags and delegates on subsystems.
5. **Everything is logged.** One event bus (`UResponseEventLog`) gets every meaningful action. BWV, debrief scoring,
   complaints and court all read from it. It is the single source of truth for "what happened".

## Layers

```
 Layer 4  Career, Debrief/NDM scoring, Consequences (complaints, IOPC, court, trust)
 Layer 3  Custody, Evidence/Investigation, Use of Force, Vehicles/Pursuits
 Layer 2  Dispatch, Radio, Checks (PNC/PND/ANPR), City AI (Mass), Injury/Welfare
 Layer 1  ResponseCore: IDs, game clock, event log, data registry, save, RNG streams, GameplayTags
 Layer 0  Engine: World Partition, Mass, StateTree, GAS, Chaos, Enhanced Input, CommonUI
```

## Modules

| # | System | Module | Main types | Engine features | Milestone |
|---|---|---|---|---|---|
| — | Core | `ResponseCore` | `FResponseId`, `UResponseClockSubsystem` (shift time, day/night, weekday curves), `UResponseEventLog`, `UResponseDataRegistry`, seeded RNG streams | GameplayTags, SaveGame | M1 |
| 1 | Dispatch and control room | `ResponseDispatch` | `FIncident`, `FCallTypeDefinition`, `EIncidentGrade`, `UDispatchSubsystem`, `UDispatcherBrain` (AI dispatcher, swappable for a player later) | Subsystems, DataTables | M1 (model built now) |
| 1 | Radio | `ResponseRadio` | `FTalkgroup`, `FCallSign`, `URadioSubsystem`, message queue with priority and "urgent assistance" override | MetaSounds, CommonUI | M1 |
| 2 | Call mix / demand | `ResponseDispatch` + `ResponseCity` | `UDemandModel`: weighted call generation by hour, weekday, weather and area. Weights in `Data/Demand/` | — | M1 (weights), M3 (city-driven) |
| 3 | Checks / MDT | `ResponseChecks` | `FPersonRecord`, `FVehicleRecord`, `FPncMarker`, `UChecksSubsystem` (lazy seeded generation, persistence), ANPR camera actors | DataTables | M1 |
| 4 | Use of force | `ResponseForce` | GAS abilities per tactic (`GA_Presence`, `GA_Verbal`, `GA_Handcuff`, `GA_PAVA`, `GA_Baton`, `GA_Taser`), `FForceRecord` with stated justification, subject-resistance StateTree, positional-asphyxia timer | GAS, StateTree, Motion Matching | M1 (verbal, cuffs, PAVA, Taser) |
| 5 | Arrest and custody | `ResponseCustody` | `FArrest` (grounds, necessity from Code G, caution given, time), `USearchPowers` (s1, s32), custody sergeant AI that can refuse detention, `FPeaceInterview`, `UCpsDecisionModel` (evidential and public interest tests) | StateTree, dialogue DataTables | M1 |
| 6 | Investigation and paperwork | `ResponseEvidence` | `FExhibit`, `FStatement`, `FCrimeReport`, CCTV trawl, forensics requests with realistic delays, file quality score | CommonUI | M1 (minimum), M4 (full) |
| 7 | Consequences | `ResponseConsequence` | `FComplaint`, IOPC referral rules, misconduct outcomes, court cross-examination built from the event log, per-area `FCommunityTrust` | — | M1 (debrief only), M4 |
| — | BWV | `ResponseEvidence` | `UBwvRecorder`: records event log entries and camera transforms, not video. The replay is rebuilt from that data. Pre-event buffer toggle. | Replay-style reconstruction | M1 |
| 8 | Career | `ResponseCareer` | `FOfficerProfile`, competencies, tutor PC AI, specialism unlocks | — | M5 |
| 9 | Emergent city AI | `ResponseCity` | Mass fragments (routine, home, work, vulnerability, offending propensity), StateTree routines, `UCrimeEmergence` rules, organised crime network graph | Mass Entity, StateTree, ZoneGraph | M1 (ambient crowd), M3 (emergence) |
| 10 | Vehicles and pursuits | `ResponseVehicles` | fleet DataAssets, Battenburg livery material, lights/sirens, pursuit authorisation state machine (initial phase, tactical phase, TPAC) | Chaos Vehicles | M1 (patrol car), M4 (pursuits) |
| 11 | Injury and welfare | `ResponseWelfare` | persistent injuries, first aid actions, ambulance delay model, officer fatigue | GAS attributes | M4 |
| 12 | World | `ResponseWorld` | time of day, weather (rain-heavy), wet road friction, streaming helpers | Sky Atmosphere, Volumetric Clouds, Niagara | M1 |
| — | Debrief / NDM | `ResponseConsequence` | `UNdmScorer`: reads the event log per incident and scores the five NDM stages plus the Code of Ethics | — | M1 |
| — | Editor tools | `ResponseEditor` | JSON to DataTable importers, schema validation, building QA heat map, pipeline import actions | Editor Utility Widgets | M1 |

## Key flows

**Call to deployment:** `UDemandModel` or `UCrimeEmergence` creates a call, then `UDispatchSubsystem::CreateIncident`.
The call is graded (by rules from the call type, or by a human dispatcher later), queued, and assigned by `UDispatcherBrain`.
The radio announces it, the player accepts or self-deploys, and status updates flow back as `FIncidentLogEntry`.

**Force to debrief:** a GAS ability fires, `ResponseForce` writes `FForceRecord` (tactic, subject behaviour, officer's stated
reason) to the event log, BWV captures it, and `UNdmScorer` scores necessity and proportionality at end of shift.
Complaints are rolled from the same record.

**Arrest to charge:** `FArrest` (grounds + necessity + caution) leads to transport, then custody booking (the sergeant
checks grounds and necessity and can refuse), PACE clock starts, PEACE interview, evidence file quality, CPS decision:
charge, NFA, or released under investigation.

## Realism toggles

Where realism may hurt fun we keep the realistic default and expose a toggle in `UResponseGameSettings`:
travel-time scaling, paperwork depth, PACE clock speed, ambulance delay, sensitive-content filters (domestic abuse,
child protection, suicide, sexual offences: show / summarise / exclude), street name mode (real / altered).

## Performance budget (60 fps, RTX 3070-class, 1440p)

Frame budget 16.6 ms. Starting allocation: GPU ~14 ms (Lumen ~4, Nanite and base pass ~4, shadows ~2.5, post ~1.5, rest).
Game thread under 8 ms: Mass crowd under 2 ms, vehicles under 1 ms, gameplay under 2 ms.

Risks, flagged now:
- **Lumen at 1440p on a 3070** is borderline. Plan for TSR upscaling from about 1080p internal. Hardware ray tracing off by default.
- **Dense 1:1 terrace streets** mean many unique building meshes. We must build from a modular kit with Nanite and ISM/HISM
  instancing, not unique mesh per building. HLODs are required.
- **1 m landscape over roughly 100 km²** needs World Partition landscape streaming. Only the Ashton zone ships at 1 m in M1.
  Moorland can drop to 2 m.
- **Rain**: wet surfaces and puddles through material parameter collections, not translucent overdraw.
- **Mass crowd**: target 300–500 visible agents in a busy town centre, with LOD down to Mass representation.
