# 05 — Sign-off register

**Project status: non-commercial (decided 2026-09-26).** Non-commercial-only sources and tools are allowed. Each source
in `Pipeline/config/tameside.json` has a `commercial_ok` flag. Setting `project_mode` to `commercial` makes the pipeline
refuse anything non-commercial, which shows exactly what would need replacing if that ever changes.

Nothing here is used until you mark it **Approved**. Licence readings are my interpretation, not legal advice.
For anything shipped commercially, a short review by a solicitor who handles IP/data licensing is worth the money (see S-12).

| ID | Decision | My recommendation | Cost | Status |
|---|---|---|---|---|
| S-01 | Version control: Perforce vs Git LFS | Git + LFS now; Perforce (free ≤5 users) when Content passes about 10 GB | £0 now; £15–40/month server later | Pending |
| S-02 | Move `Response/` into its own repository | Yes, before any UE content is committed | £0 | Pending |
| S-03 | Houdini for Stage E | **Houdini Apprentice** (free, non-commercial). Limits: `.hipnc`/`.hdanc` files, 1280×720 render cap, watermarks on renders. Houdini Engine for UE works with it. Apprentice files cannot be upgraded to Indie/FX later, so HDAs would need rebuilding if we go commercial | £0 | **Approved** |
| S-04 | Unreal Engine licence | Standard EULA: 5% royalty on gross revenue above $1M lifetime per product | Royalty only | For awareness |
| S-05 | OpenStreetMap + Microsoft footprints (ODbL) | Use, with in-game credit | £0 | **Approved** |
| S-06 | HM Land Registry INSPIRE polygons | Use for plots (non-commercial) | £0 | **Approved** (non-commercial only) |
| S-07 | Mapillary imagery for facade analysis | Derive attributes only; credit contributors | £0 | **Approved** (flagged non-commercial for safety) |
| S-08 | Your own 360° capture drives | Declined by the creative director. Mapillary only, with propagation for unseen facades (docs/11) | £0 | **Declined** |
| S-09 | Aerial orthophoto (Getmapping / Bluesky) | Get quotes that include **game distribution rights**. Otherwise use the vector fallback | Likely £2k–10k+ (quote needed) | Pending |
| S-10 | Facade-parsing training datasets (CMP, ECP, etc.) | Research/non-commercial datasets allowed | £0 | **Approved** (non-commercial only) |
| S-11 | Generated UK number plates | Filter against known real-plate patterns. Add a disclaimer | £0 | Pending |
| S-12 | Legal review (licences, trademarks, depiction of real places, police procedure) | Before Early Access | about £1–3k | Pending |
| S-13 | Real street names | Default "real", with a config toggle to "altered" | £0 | Pending |
| S-14 | MetaHuman use | Allowed in UE projects under the MetaHuman licence | £0 | For awareness |
| S-15 | GTA V mod assets (UK Road Signs; Project London Remastered; Roads of Europe) | **Not used.** UK Road Signs is by Razor792/Albo1125 (not NotchApple) and re-textures Rockstar models. Project London is a multi-author team pack (NotchApple can only grant their own share) and contains real brands, which breaks the fictional-business rule. Roads of Europe is by another author and Berlin-style. Rockstar's terms forbid using GTA assets outside GTA. Pieces verifiably made solely by NotchApple (e.g. an original texture) can be used as reference on request | £0 | Decided |
| S-16 | Replacement sources | UK signs from **DfT traffic sign images** (Open Government Licence, TSRGD-numbered, credit "Crown copyright"). Road, brick, render, pebbledash, slate materials from **ambientCG / Poly Haven (CC0)**. Free Fab packs checked per licence | £0 | Proposed |
