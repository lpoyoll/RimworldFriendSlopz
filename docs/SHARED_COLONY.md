# Shared Colony prototype

This branch extends RimWorld Together without replacing its existing world,
save or synchronous-action formats. It deliberately uses a small companion
Harmony assembly because this repository contains the server source but only a
compiled copy of `RTClient.dll`.

## Behaviour

- A world tile may hold up to four settlements owned by four different server
  usernames.
- Shared-colony maps are 500x500 cells. This is four times the area of the
  usual 250x250 map, not four times its width.
- The first player to settle a tile becomes its stable map host. Only that
  player's map upload is accepted once the tile has more than one member,
  preventing last-write-wins map corruption.
- Every remote username receives a separate runtime RimWorld faction. Remote
  pawns and settlement markers no longer collapse into one global ally/enemy
  faction.
- Remote-pawn gizmos are removed. The extra draft guard only permits a remote
  draft change while RimWorld Together's network bypass flag is active.
- Synchronous requests are targeted by username before the stock request is
  sent. This makes several overlapping settlement markers on the same world
  tile unambiguous without changing `RTNetwork.dll`.
- Diplomacy is bilateral and persistent:
  - `neutral`: neither side has committed.
  - `support`: one side is willing to assist; vanilla faction behaviour remains
    neutral.
  - `ally`: becomes an effective alliance only when both players choose it.
  - `hostile`: unilateral and immediately effective.

The server blocks friendly visits between hostile factions and hostile visits
between effective allies. Neutral and support relationships still require the
target player to accept the normal synchronous prompt.

## Build

Requirements:

- RimWorld 1.6 installed on Windows.
- .NET 8 SDK (builds the server and can also build the net472 companion).
- Harmony installed for RimWorld.

Build the server:

```powershell
dotnet publish .\Source\Server\RTServer.csproj -c Release -r win-x64 --self-contained true
```

Build the client companion from the repository root:

```powershell
.\Scripts\BuildSharedColony.ps1 `
  -RimWorldDir "D:\SteamLibrary\steamapps\common\RimWorld" `
  -HarmonyDll "D:\SteamLibrary\steamapps\workshop\content\294100\2009463077\Current\Assemblies\0Harmony.dll"
```

The script writes `1.6\Assemblies\RWTSharedColony.dll`. Include that DLL in the
RimWorld Together mod folder alongside the normal `RTClient.dll`. Every player
connecting to the shared-colony server must use the same companion build.

## Server settings

The first run adds these values to `Configs/ServerConfig.json`:

```json
{
  "EnableSharedColonyTiles": true,
  "SharedColonyTileCapacity": 4,
  "SharedColonyMapSize": 500,
  "EnforceSharedColonyMapSize": true,
  "SharedColonyHostOnlyMapSaves": true,
  "EnforceSharedColonyDiplomacy": true
}
```

Existing 250x250 settlements remain readable, but they cannot accept a second
settler while map-size enforcement is enabled. Create a fresh server world for
the cleanest test.

## Player commands

```text
/colony status
/colony relation @player neutral
/colony relation @player support
/colony relation @player ally
/colony relation @player hostile
```

`ally` must be chosen by both players. `hostile` does not require consent.

## First multiplayer test

1. Start a fresh server and connect player A with the companion DLL enabled.
2. Create player A's first colony and verify the local map is 500x500.
3. Connect player B, travel to A's world tile, enter the existing local map and
   use RimWorld's settle-in-existing-map action.
4. Run `/colony status` from both clients. The tile should show `2/4`, with A as
   map host.
5. On both clients run `/colony relation @other ally` and re-check status. The
   effective relationship should be `Ally`.
6. Start a normal RimWorld Together online visit from either overlapping
   settlement marker. Accept it on the other client.
7. Confirm both player groups are on the 500x500 map, display as distinct
   factions, and that selecting a remote pawn exposes no order/draft gizmos.
8. Change one side to `hostile`; a friendly visit should be rejected and a
   hostile interaction should still require the target's normal acceptance.
9. Save from A, then cause B to upload. The server should accept A's canonical
   map and tell B that its upload was ignored.

## Prototype boundary

RimWorld Together's current synchronous layer mirrors jobs, drafting, health,
mental states and weather between two simulations. This branch preserves that
model. It does not yet turn RimWorld into a four-client deterministic lockstep
simulation, so the first release should be tested with two simultaneously
active players before expanding a tile to three or four. The server data model
and faction registry already support four owners; broad construction, fire,
power-network and item-stack replication are the next technical milestone.

This work is maintained in the fork only. The upstream repository's current
contribution policy says it does not accept AI-assisted pull requests.
