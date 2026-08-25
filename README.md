<p align="center">
  <img src=".github/assets/rimjob-banner.svg" alt="Rimjob - Shared world, separate colonies" width="100%" />
</p>

<p align="center">
  <strong>An independent multiplayer project for RimWorld, developed by Ignis &amp; Avis.</strong>
</p>

<p align="center">
  <img alt="RimWorld 1.6" src="https://img.shields.io/badge/RimWorld-1.6-8b6f47" />
  <img alt="Status" src="https://img.shields.io/badge/status-active%20development-d97738" />
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

## Current release work

The current v0.1.10 line focuses on **shared-tile starting settlements**. A joining player should be able to select an already occupied multiplayer tile, create their own settlement there and retain separate colony ownership.

That includes work around:

- occupied starting-tile selection;
- multiple settlements persisted on one tile;
- strict pawn ownership boundaries;
- player diplomacy;
- client/server packaging;
- self-contained dedicated server builds.

## Installation

Rimjob requires **Harmony**.

For Windows, use the Rimjob MSI where available. It installs the client under the selected RimWorld installation at `Mods/Rimjob`. The dedicated server is offered separately as an optional host-only component.

Manual ZIP builds contain the same client payload plus the standalone server executable.

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
