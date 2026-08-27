<p align="center">
  <img src=".github/assets/rimjob-banner.svg" alt="Rimjob - Shared world, separate colonies" width="100%" />
</p>

<p align="center">
  <strong>An independent multiplayer project for RimWorld, developed by Ignis &amp; Avis.</strong>
</p>

<p align="center">
  <img alt="RimWorld 1.6" src="https://img.shields.io/badge/RimWorld-1.6-8b6f47" />
  <img alt="Release" src="https://img.shields.io/badge/release-v0.1.26-d97738" />
  <img alt="Multiplayer" src="https://img.shields.io/badge/focus-multiplayer-40566b" />
</p>

# Rimjob

Rimjob is an independent multiplayer project with its own releases, branding and development direction.

The aim is simple: **play in the same RimWorld world without turning every player into one shared omniscient colony controller.**

## What Rimjob is building

- **Separate player colonies and ownership** - your pawns are yours, another player's pawns are theirs.
- **Shared world tiles** - multiple player settlements can occupy the same wider multiplayer area.
- **Larger shared colony spaces** - expanded maps suitable for several independently controlled settlements.
- **Player-specific diplomacy** - players can be allies, neutral or hostile independently.
- **Cooperation without shared control** - trade, support, defence and joint activity without direct control of another player's colony.
- **Dedicated hosting** - a standalone server build for persistent worlds.
- **Installer-first releases** - Windows builds are packaged as both ZIP and MSI, with the server as an optional host component.

## Current release: v0.1.26

v0.1.26 is the matched client/server release for live shared-tile play.

- Explicitly activates the auto-accepting client as the synchronous host, fixing the guest-to-host visibility path and restoring the host's live pawn stream to the guest.
- Routes Rimjob private action IDs before the inherited action-manager lookup, preventing the action 9022 exception/log storm.
- Requires the matching `RJ24` server protocol before private pawn or world-state replication begins.
- Keeps each player authoritative for their own pawns and uses owner-scoped mirror aliases so both players see the same pawns at their current positions.
- Mirrors host-authoritative building state to the joining player.
- Includes 500x500 settlement maps, same-tile joining, host time, Direct Connect, F9 diagnostics and `Update.exe`.
- Ships as an MSI and ZIP with an optional self-contained Windows server.

[Download Rimjob v0.1.26](https://github.com/lpoyoll/RimworldFriendSlopz/releases/tag/v0.1.26)

## Installation

Rimjob requires **Harmony**.

For Windows, use the [v0.1.26 MSI](https://github.com/lpoyoll/RimworldFriendSlopz/releases/download/v0.1.26/Rimjob-v0.1.26-x64.msi). It installs the client under the selected RimWorld installation at `Mods/Rimjob`; select the optional server component on the hosting PC.

Both clients and the server must use v0.1.26. `Update.exe` updates the client only, so the host must also replace or reinstall the server when moving between releases.

The manual ZIP contains the same client payload and standalone server executable.

> [!IMPORTANT]
> Disable the original RimWorld Together Workshop mod while testing Rimjob. Rimjob still retains some inherited internal identifiers for compatibility while the project is separated cleanly.

## Origins & credits

Rimjob began from the **RimWorld Together** codebase and credits the maintainers and contributors whose work formed that foundation.

Original project: [RimWorld Together](https://github.com/RimworldTogether/Rimworld-Together)

Rimjob is **not** an official RimWorld Together release or support channel. It is developed independently by **Ignis & Avis** with a different multiplayer direction.

Inherited copyright, licence information and contributor attribution are retained where applicable.

RimWorld is developed by Ludeon Studios. Rimjob is an unofficial community mod and is not affiliated with or endorsed by Ludeon Studios.

## Development direction

1. Reliable shared-tile starting settlement creation.
2. Multiple independently persisted settlements on one tile.
3. Strict pawn ownership and remote-control boundaries.
4. Player-to-player diplomacy and relationship state.
5. Larger maps for practical multi-colony settlement.
6. Better joining, reconnecting and world-state synchronisation.
7. Cleaner client/server installation and release packaging.

## A note on the name

Yes, it's called **Rimjob**.

We're keeping it.
