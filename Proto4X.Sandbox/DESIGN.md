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
| Double click | Select every unit of that kind on the map |
| Left drag | Box-select your units |
| Shift + click/drag | Add to selection |
| Right click | Move order for everything selected (fans out into a ring), or set a **rally point** if a station is selected |
| `F` | Spawn a free fighter wave for every faction — load test only |
| `1` / `2` | Issue the selected unit's commands — see below |

Hotkeys are **per selection, not global**. What a key does depends on what you
have selected, exactly as in Age of Empires:

| Selected | `1` | `2` |
|---|---|---|
| Station | Queue a Miner (50) | Queue a Fighter (75) |
| Miner | Found a new station here (200) | — |

Select several stations and they all queue, each charged separately. Select
several miners and press `1` and exactly one starts building, because the others
fail to afford it — the economy does the arbitration, so there are no
"who goes first" rules to write.

Founding a station is a movement decision: fly a miner to the asteroid field you
want worked, then build. New stations are drop-off points and shipyards
immediately, so expanding is how you scale mining. Set a station's rally point on
an asteroid field and every miner it builds goes straight to work there.

## The map and the opponents

Three factions on a 6400×3600 map: you, and two AI factions that expand on their
own. Roughly 100 procedurally scattered asteroid fields, plus two guaranteed
fields next to each homeland so nobody starves on an unlucky seed.

**One miner per asteroid.** Claims are held as positions rather than entity
references, and every claim is gathered before any new one is handed out, so a
rock that's taken is invisible to the next miner looking for work. A miner
releases its rock while hauling, so nothing sits idle across the round trip.

The AI factions target their nearest *hostile* station rather than the player
specifically, so they will happily fight each other. Each new station they found
raises their miner target, which makes fleet growth super-linear — the point
being to generate real load without the player having to drive it.

**Defence outranks attack.** A faction with a hostile ship near any of its
stations recalls its fleet, overriding an in-flight wave. Without that, the first
faction to attack was also the first to lose its undefended base, and a lost base
means no production, so the game decided itself in the first minute.

**Stations repair themselves**, at 18/s against a fighter's 14 dps. One raider
achieves nothing; nine are a siege. Taking a station is meant to be a decision,
not an accumulation of pokes.

**Wave size scales with stations**, not miners — 8 fighters for a faction with
one station, +4 per additional station, capped at 28. The threshold is really
asking "do I have enough surplus that leaving won't cost me a base", and both
halves of that scale with station count: more stations means more to defend *and*
more production to reach a bigger number. Scaling on miners would mean a richer
faction attacks *later*, which is backwards.

**Expansion goes to the frontier, not the far corner.** Picking the asteroid
furthest from your own stations sounds like claiming the most ground and actually
means walking across the map into someone else's base. AI factions take the
*nearest* field that isn't already theirs and refuse sites close to a rival, so
territory grows outward in a ring.

## Instrumentation

A rolling frame-time graph sits in the top-right: one column per recent frame,
green under the 60fps budget, amber under 30fps, red over. An average FPS number
would hide the thing worth catching, which is the occasional long frame.

Known O(n²) sweeps, in the order they're expected to hurt: `SeparationSystem`
(every idle ship against every other, no early range rejection), then
`CombatSystem` (same shape, but range-limited). Both want a uniform spatial grid
when that day comes.
| Arrow keys / `Z` `X` / scroll | Pan / rotate / zoom (engine `Camera`) |
| `Esc` | Quit |

## No text, on purpose

The game renders **no text at all**, and this is a design constraint rather than an
engine limitation to be worked around. A game that is legible without words is a
better game once words are added, because text stops being the crutch that
explains mechanics the visuals failed to convey.

What that means in practice:

| Instead of | The game shows |
|---|---|
| "Miner — 50 minerals" | The miner's own sprite, in a slot, with a ring that fills as you approach 50 and closes when you can afford it |
| "Building: Fighter, 4s left" | A radial ring around the station, with the fighter's sprite above it |
| "3 queued" | Three pips under the station |
| "Press 1" | One pip under the slot; two pips for `2` |
| A unit list | A minimap of faction-coloured dots |

The rule of thumb: if a number is the answer, ask whether the player's actual
question was "how close am I" or "can I yet" — those have shapes, not digits.

The `Window.Title` line still carries raw counts, but it's now a debug readout
rather than the HUD. Victory and defeat are the last things still expressed in
words, and are the obvious next thing to design without them.

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
