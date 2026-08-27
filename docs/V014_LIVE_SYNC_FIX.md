# Rimjob v0.1.14 live shared-map handshake

v0.1.13 proved that multiple players can select the same world tile, but the live-map handover still depended on packet ordering during `Game.InitNewGame`.

v0.1.14 makes settlement registration explicit: the server acknowledges the joining player's settlement before the client sends the same-tile synchronous request. This removes the race where the synchronous request could arrive while the server still had no settlement record for the requester.

The client also has a fallback target resolver that recovers the existing `RTSettlement` owner from the selected/starting tile when `FirstSelectedObject` is no longer populated by the time `Page_SelectStartingSite.DoNext` runs.
