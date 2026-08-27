# Rimjob v0.1.23 sync architecture

v0.1.22 proved the shared-session handshake, but exposed two transport problems:

1. A newer client could emit private Rimjob synchronous action IDs (9022/9023) into an older RTServer build. Older servers route every action through the stock RWT action-manager lookup and throw when the private action ID is unknown.
2. Pawn position updates at ~10 Hz can flood logs/queues if the server rejects them.

v0.1.23 therefore makes private Rimjob traffic an explicitly versioned transport:

- Server advertises protocol `RJ23` through `[RWT_SHARED]|BUILD|0.1.23|RJ23`.
- Clients do not emit Rimjob private actions until that exact protocol is observed.
- Server intercepts private action IDs before stock synchronous routing and relays only to the paired peer.
- Pawn position traffic is rate-limited to 4 Hz client-side and 8 Hz server-side; manifest traffic is limited to one per 10 seconds.
- Private packets are not echoed back to the sender.
- The host remains canonical for shared map/world state; each player remains authoritative only for their own pawns.

This prevents a client/server version mismatch from creating packet storms or disconnecting the host.
