# 05 — Sign-off register

Nothing here is used until you mark it **Approved**. Licence readings are my interpretation, not legal advice.
For anything shipped commercially, a short review by a solicitor who handles IP/data licensing is worth the money (see S-12).

| ID | Decision | My recommendation | Cost | Status |
|---|---|---|---|---|
| S-01 | Version control: Perforce vs Git LFS | Git + LFS now; Perforce (free ≤5 users) when Content passes about 10 GB | £0 now; £15–40/month server later | Pending |
| S-02 | Move `Response/` into its own repository | Yes, before any UE content is committed | £0 | Pending |
| S-03 | Houdini Indie + Houdini Engine | Yes, for Stage E | about £230/year (Indie, revenue < $100k) | Pending |
| S-04 | Unreal Engine licence | Standard EULA: 5% royalty on gross revenue above $1M lifetime per product | Royalty only | For awareness |
| S-05 | OpenStreetMap + Microsoft footprints (ODbL) | Use them for attributes and gap fill. Credit in-game. Do not publish our derived *database* (the shipped game is a "produced work") | £0 | Pending |
| S-06 | HM Land Registry INSPIRE polygons | Use only if the licence permits shipping derived geometry. Fallback: derive plots procedurally | £0 | Pending, needs licence check |
| S-07 | Mapillary imagery for facade analysis | Derive attributes only. Never ship pixels. Credit contributors | £0 | Pending |
| S-08 | Your own 360° capture drives | Yes. Blur faces and plates on ingest. Don't publish raw imagery. Follow the ICO guidance on filming in public | 360° camera about £400–600 | Pending |
| S-09 | Aerial orthophoto (Getmapping / Bluesky) | Get quotes that include **game distribution rights**. Otherwise use the vector fallback | Likely £2k–10k+ (quote needed) | Pending |
| S-10 | Facade-parsing training datasets (CMP, ECP, etc.) | Check each for commercial use. Many are research-only. Fallback: label our own Tameside images | £0 or labelling time | Pending |
| S-11 | Generated UK number plates | Filter against known real-plate patterns. Add a disclaimer | £0 | Pending |
| S-12 | Legal review (licences, trademarks, depiction of real places, police procedure) | Before Early Access | about £1–3k | Pending |
| S-13 | Real street names | Default "real", with a config toggle to "altered" | £0 | Pending |
| S-14 | MetaHuman use | Allowed in UE projects under the MetaHuman licence | £0 | For awareness |
