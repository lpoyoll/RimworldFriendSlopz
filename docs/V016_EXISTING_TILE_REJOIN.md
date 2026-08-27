# Rimjob v0.1.16 existing shared-tile rejoin

v0.1.15 fixes fresh same-tile map hand-offs. v0.1.16 also repairs colonies that were already registered on one tile before that fix was installed.

When a shared-tile member connects, the server now sends the tile, member count and canonical map-host username. After RimWorld finishes loading a saved game, a non-host member automatically requests the canonical host map using the same authenticated synchronous path. The host never tries to join a guest, and no world-marker ordering is involved.

Both clients and the server must run v0.1.16. Load the canonical host first, then load the other member's save.
