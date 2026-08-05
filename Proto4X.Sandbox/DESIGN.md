# Sandbox Game Design — "Void Command" (working title)

A slim, space-skinned Age of Empires. Built in `Proto4X.Sandbox` to exercise
`MonoGameLibrary` under a real RTS workload. Deliberately small: one resource,
two unit types, one building, one enemy AI, one win condition.

## The loop

1. **Gather** — Miner ships fly to the nearest asteroid, mine for a few seconds,
   fly back to their station, deposit minerals. Fully automatic once built.
2. **Spend** — the station converts minerals into more Miners (economy) or
   Fighters (military), one at a time, on a build timer.
3. **Fight** — Fighters auto-engage the nearest enemy inside weapon range.
   Stations shoot back. The player directs Fighters with select + move orders.
4. **Win** — destroy the enemy station. Lose if yours dies.

That's the AoE skeleton — villagers, town centre, military, raze the enemy base —
with the 4X/factory ambitions of `Proto4X` proper deliberately left out.

## Controls

| Input | Effect |
|---|---|
| Left click | Select one of your units |
| Left drag | Box-select your units |
| Shift + click/drag | Add to selection |
| Right click | Move order for everything selected (fans out into a ring) |
| `1` | Queue a Miner at your station |
| `2` | Queue a Fighter at your station |
| Arrow keys / `Z` `X` / scroll | Pan / rotate / zoom (engine `Camera`) |
| `Esc` | Quit |

HUD is the window title bar — see "no text rendering" in `ENGINE_REQUESTS.md`.

## Entity shapes

| Entity | Components |
|---|---|
| Miner | Position, Motion, MoveOrder, Faction, Health, Weapon(unarmed), Selectable, Miner, Drawable |
| Fighter | Position, Motion, MoveOrder, Faction, Health, Weapon, Selectable, Drawable |
| Station | Position, Faction, Health, Timer, Weapon, Selectable, Station, Drawable |
| Asteroid | Position, Gatherable, Drawable |

## The one architectural decision that makes this possible

The engine currently offers **no way to learn an entity's id** — not from a chunk
index during iteration, and not from `GameWorld.AddEntity` (ids are assigned
later, inside `ProcessDeferredUpdates`). The previous sandbox build dodged this
with `const int PlayerEntityId = 0`, which obviously does not survive a game with
dozens of units.

So this design **never uses entity ids at all.** Everything that would normally
be an entity reference is instead either component state or a proximity query:

- **Selection** is a `bool` on a `Selectable` component, not a list of selected
  ids. Selecting and ordering become ordinary chunk iterations.
- **Targeting** is "nearest hostile in range", recomputed each frame, rather than
  a stored target id.
- **Miner ↔ asteroid ↔ station** links are nearest-position lookups against
  per-frame index snapshots, not references.
- **Production** pushes plain spawn *requests* onto a list that the game loop
  drains, so nothing needs the id of the ship it just built.

This turned out to be more ECS-idiomatic than an id-based design would have been,
so it is worth keeping even after the engine grows ids. See `ENGINE_REQUESTS.md`
for the gaps that forced it and the two that are still genuinely limiting.

## System order (per frame)

Index (asteroids, stations) → player input (selection, orders, build hotkeys) →
miner AI → enemy AI → steering → movement integration → combat → death →
production. Then `Sandbox.OnUpdate` drains the spawn queue and updates the HUD.

## Deliberately not in scope

Finite asteroids, building placement, multiple resources, unit collision/flocking,
fog of war, pathfinding around obstacles, more than one enemy, sound.
