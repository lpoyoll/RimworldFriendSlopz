# 04 — Milestone plan

Estimates assume **1 full-time generalist developer (me directing, you producing) plus bought/free assets**,
and separately a **small team of 4–5** (2 engineers, 1 technical artist, 1 environment artist, part-time designer/writer).
These are honest ranges. A 1:1 borough plus deep systems is a very large game.

| # | Milestone | Content | Solo | Small team |
|---|---|---|---|---|
| M0 | Foundations *(in progress)* | Docs, schemas, repo, Stage A terrain, dispatch data model | 1 month | 2 weeks |
| M1 | **Vertical slice**: Ashton town centre | Full pipeline A–I on about 4 km²; on-foot Response PC + patrol car; AI dispatch with 10 call types; PNC/MDT; verbal, cuffs, PAVA, Taser; arrest to custody to charge; BWV + NDM debrief; day/night + rain | 9–12 months | 5–6 months |
| M2 | Ashton + Dukinfield + Audenshaw | Pipeline at scale, HLOD/streaming hardening, Mass crowd, 25 call types, Radio v2, M60/M67 hero | 6–8 months | 3–4 months |
| M3 | Emergent city | Crime emergence from world state, repeat offenders, vulnerable people, informants, weekday/weekend demand. Stalybridge, Hyde, Denton, Droylsden | 9–12 months | 5–6 months |
| M4 | Consequences + investigation | Complaints, PSD/IOPC, court cross-examination, trust meter, full paperwork/CPS, injuries and welfare, pursuits + TPAC | 8–10 months | 4–5 months |
| M5 | Career + specialisms | Student PC + tutor, Roads Policing, AFO/ARV, CID, Neighbourhood. Mossley, Longdendale, moorland edge | 10–14 months | 6–8 months |
| M6 | Organised crime + co-op | County lines / cuckooing network, ROCU/NCA storyline, player dispatcher co-op | 8–10 months | 4–6 months |
| M7 | Polish, performance, release | Optimisation to the 3070 budget, accessibility, localisation, mod tools, Early Access | 6 months | 3–4 months |
| | **Total** | | **about 5–6 years** | **about 2.5–3 years** |

Recommendation: plan for **Early Access after M3** (an Ashton-to-Hyde playable area, emergent city), funded by that,
with M4–M7 as updates. That is the realistic route for a solo or small team.

## M1 breakdown (vertical slice)

1. Stage A terrain in engine, with coordinate system verified against known survey points. *(pipeline done, import pending engine)*
2. Stage B–C footprints, roads, massing; Houdini kit v1 (terrace, retail, shopfront, council).
3. Stage D facade ML on your own capture of Ashton centre (Mapillary if S-07 is approved).
4. Stage G dressing; Stage I QA tool.
5. `ResponseCore` (clock, event log, IDs, save); dispatch loop playable from a debug MDT.
6. Player on foot + patrol car (Chaos); radio v1.
7. Checks/PNC generation + MDT UI.
8. Use of force GAS abilities + subject StateTree.
9. Arrest, custody sergeant, interview-lite, CPS decision.
10. BWV reconstruction + NDM debrief.
11. Weather/time of day; performance pass against the 3070 budget.
