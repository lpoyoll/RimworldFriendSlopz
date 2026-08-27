# Rimjob v0.1.15 shared-map visibility fix

v0.1.14 waited for the joining settlement to be registered, but the host still required the joining player's duplicate `RTSettlement` world marker before it would auto-accept the host-authoritative map transfer. The marker broadcast and synchronous request use separate client connections, so the request could arrive while that UI marker was absent or still being added. The stock visit handler then ran and both clients kept independent maps.

v0.1.15 recognises the server-authenticated synchronous request itself: the server supplies the requester's username and settlement tile, the request must be an `Ask`, and its source and destination must be the same valid tile. The host's live map must also be on that tile before the transfer is accepted. No asynchronously rendered world marker is required.

Both clients and the server must run v0.1.15 for this handshake.
