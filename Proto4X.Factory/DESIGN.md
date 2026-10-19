# Proto4X.Factory — design

A factory-automation vertical slice on `MonoGameLibrary`, in the genre of the well-known
belts-and-inserters game and its multi-planet expansion. It's built as an **engine-limits
exploration, mini demo and stress-test platform**, not a clone. Engine gaps go to
`ENGINE_REQUESTS.md` in this folder. Nothing in `MonoGameLibrary` was changed.

**Copyright stance.** Game mechanics (belts, inserters, recipes as data) aren't protected
expression. Everything expressive is ours: generic item and building names, our own numbers and
recipe set, no art (every texture is a runtime-generated 1×1 pixel), and no names from the original's
planets or expansion. The ship design deliberately departs from the original's static space platform
(see "Ships").

## Layout and running it

```
Proto4X.Factory/
  Simulation/   deterministic sim, classlib: grid, stores, components, update systems
  Game/         MonoGame WinExe: input, rendering, HUD
  Headless/     console: scenario checks, determinism hash, benchmark
```

The sim/presentation split is an **assembly boundary** on purpose. Lockstep multiplayer, headless
servers and replays all require the sim to be a pure function of (state, commands, tick), and an
assembly that can't see input or graphics is the cheapest way to keep that true.

- `dotnet run -c Release --project Proto4X.Factory/Headless`: 42 checks, including conservation of
  items, belt throughput, side-loading, curves, loops, power starvation, deconstruct/replace,
  multi-removal integrity, recipe change, surface conditions, launch to ship, and determinism.
- `dotnet run -c Release --project Proto4X.Factory/Headless -- bench 8000`: benchmark.
- `dotnet run --project Proto4X.Factory/Game`: the game. **Not run by Claude.** It's build-verified
  only, so everything visual and interactive needs a first playtest.
- Not in `Proto4X.sln` yet. To add it:
  `dotnet sln add Proto4X.Factory/Simulation Proto4X.Factory/Game Proto4X.Factory/Headless`.

**Controls:** `1`–`0`, `-` pick a building · `R` rotate · `T` cycle recipe (with no tool selected,
it re-sets the assembler under the cursor) · `Q` clears the tool · left-drag builds · right-drag
removes · `Tab` switches surface · `F5` stamps 100 smelting modules (2,800 buildings; a debug shortcut that calls `TryPlace` directly
rather than going through the command queue) · `Space`
pauses · arrows pan, the wheel zooms about the cursor, `Z`/`X` rotate (`Input/CameraController.cs`).
Each surface remembers its own view, and on the ship the view rides along with the hull, so the deck
stays upright on screen while it drifts and turns. Numbers (tick, sim ms, counts) are in the
window title.

## The slice

Ore tiles → **drill** (2×2) → **belts** (two lanes, curves, side-loading, loops) → **inserter** →
**furnace** (picks its recipe from its input) → inserter → **chest**. **Assemblers** (3×3) take a
recipe. **Power**: a **generator** burns coal fed by an inserter, **poles** link into networks, and
every consumer runs at the network's satisfaction. A **launch silo** on the planet ships 20-item
payloads to a **receiver** on the **ship**, a second surface whose grid drifts and rotates while its
own factory (a debris **collector** → crusher assembler) keeps running.

Recipes: smelt iron and copper, gear, wire, circuit, crush chunk, and hull segment. Hull is
ship-only, which proves surface conditions are data.

Creative-mode simplifications: buildings are free, there's no player inventory, deconstruction
destroys contents, and recipe changes discard the old inputs. There's one belt tier, inserters move
one item per swing, and belt items render straight through curves.

## Architecture

```
Universe ─ tick counter, command queue, interplanetary shipping
 └─ Surface × N (planet, ship, ...) ─ each fully independent
     ├─ GameWorld (engine)        entities: one per building, behaviour state in components
     ├─ SurfaceGrid               tiles, ore, occupancy, per-chunk version stamps
     ├─ BuildingTable             slot per building: kind, footprint, inventory run, recipe, network
     ├─ InventoryStore            every stack on the surface, flat arrays
     ├─ BeltNetwork               lane segments, gap-encoded items
     ├─ PowerGrid                 pole networks, satisfaction per network
     └─ its own instances of every update system
```

### One tick

```
Universe.Step
  1. apply queued commands        place / remove / set recipe (stores now, entities queued)
  2. per surface: BeforeTick      rebuild belt segments / power networks if dirty
  3. per surface: scheduler.Update
       Deconstruction → Generator → PowerSolve → Drill → Belt → Inserter → Crafter → Launcher
       → engine flush (deferred removals, then additions)
  4. per surface: AfterTick       release stores of buildings whose entities just left
                                  collect outbound shipments, integrate the ship frame
  5. deliver arrived shipments    between ticks, never mid-tick
```

Each system gets the same synthetic 1/60 s `GameTime`. All timing is integer ticks multiplied by
integer power satisfaction (fixed-point, 1024 = 100%), so there are no floats in the sim except the
ship frame.

### The central decision: what's a component and what's a store

| Data | Where | Why |
|---|---|---|
| Swing phase, crafting progress, fuel energy, cached targets | **components** | per-entity behaviour, only touched by the owning system: exactly what iteration is for |
| Inventories | **InventoryStore** | the medium of exchange. Other buildings must test-and-write them in the same tick (see below) |
| Kind, footprint, inventory location, recipe, network | **BuildingTable** | what *other* buildings and the outside world need to know or change |
| Belt items and lanes | **BeltNetwork** | variable-length, millions of items. Neither items nor per-tile ticking can be entities |
| Tiles, ore, occupancy | **SurfaceGrid** | spatial and static. The tile coordinate is a building's stable name |

**Inventories are outside the ECS deliberately.** An inserter iterating the inserter archetype has
to ask a furnace in another archetype "will you take this?" and then put the item in, in the same tick.
Without that atomicity, two inserters overfill the last free slot. The engine's planned answer, a
system-declared frame-bound archetype handle, doesn't exist yet. Request/apply with a tick of latency
was considered and rejected, because it still needs a shared "room left" record, i.e. a store, plus a
copy. So the store stands in for the handle, and `Simulation/Exchange.cs`, the only code that crosses
buildings, doubles as its spec. The full argument is in `ENGINE_REQUESTS.md`, first entry.

**References live in component data**, which answers open question 1 of the entity-reference
direction. An inserter caches its pickup and drop building slots and the version of its 32×32 chunk.
Every structural change bumps the 3×3 chunks around it. So the per-tick cost of staying correct is one
compare, and a placement only makes nearby inserters and drills re-resolve.

**External writes become store + version + pull.** Nothing outside a system can write a component (by
design), so a recipe change goes into `BuildingTable`, bumps `Surface.ConfigVersion`, and
`CrafterSystem` pulls it into its own component. Power-network membership and wiring use the same
pattern.

### Subsystems

**Belts.** Each lane of a run of belt tiles is a `LaneSegment` holding items gap-encoded from the
downstream end. `Gaps[0]` is front-item-to-end and `Gaps[i]` is item i-1 to item i. Shrinking one gap
moves that item *and everything behind it*, so a flowing lane costs one subtraction per tick
regardless of item count. A cached "compressed until" index skips nose-to-tail runs, so a backed-up
lane costs a compare. Units are 256 per tile, speed is 8 per tick (1.875 tiles/s, 15 items/s per belt),
and spacing is 64. Segments cap at 16 tiles, which bounds an inserter's search for an item in its tile
and the cost of mid-lane inserts. Topology rules follow the genre: a belt with no back input and exactly
one side input is a curve, otherwise side inputs load onto the near lane. Loops close by pointing the
last segment at the first. Any belt change rebuilds every segment on the surface, preserving items by
(tile, lane, offset). That's O(belts), fine for hand-building and stamping, and the first thing to make
incremental.

**Inserters.** A four-phase state machine: wait for pickup, swing, wait to drop, swing back. Swings
take 26 ticks at full power. An inserter only picks up what its target will accept and drops onto a
belt's far lane. The waiting phases poll, and that's the dominant cost (see measurements).

**Crafters.** Start when ingredients and output room are both present; output room is reserved at
start, since only inserters ever take from the output. Furnaces choose their recipe from their input
item. Input stacks cap at twice the recipe amount, which is what stops inserters overfeeding.

**Power.** Solved one tick behind on purpose. Consumers record demand, generators offer capacity, and
`PowerSolveSystem` computes satisfaction = min(1, capacity/demand), applied next tick. That turns a
circular dependency into a fixed system order. Generators burn fuel for what was actually drawn.
Network index 0 means "unconnected" and permanently has zero satisfaction, so consumers index without a
branch. Generators start with one coal's worth of charge, since the inserter feeding a generator runs
on that generator's power. (The original solves this with burner inserters.)

**Deconstruction** is two-phase because entity removal is deferred. The command marks the building
pending and clears its tiles (so a replacement can go in the same batch). `DeconstructionSystem` finds
the entity by scanning `Building` components, gated on a pending count so it's free on quiet ticks, and
queues removal **in descending index order**, which works around the engine removal-order bug.
`AfterTick` releases the slot and inventory once the entity is gone.

**Determinism.** `Universe.ComputeStateHash` folds grid, tables, stores, belts, power and every
component (via one hash system per component type) into one value. The headless check runs the same
command script twice and requires equal hashes, then requires that moving one command a tick later
changes the hash.

## Measurements

`Headless -- bench N [idle-first]`: N self-powered modules on one planet. "Idle" lays one ore per
tile, so everything starves within seconds. Each figure is the median of four runs (two per variant
order), after a JIT warm-up pass. Without it, whichever variant was timed first read 2x slow, which
briefly looked like "idle is half the cost of busy". It isn't.

| Modules | Buildings | Busy ms/tick | Idle ms/tick | Engine overhead (empty bodies) |
|---|---|---|---|---|
| 2,000 | 56,000 | 0.36 | 0.37 | ~0.004 ms, 5.7 KB alloc/tick |
| 8,000 | 224,000 | 2.43 | 2.19 | ~0.002 ms, 5.7 KB alloc/tick |

Per system at 8,000 modules (busy): inserters 1.46 ms (37 ns each), belts 0.70 ms (32k segments,
22 ns each), crafters 0.11 ms (7 ns), drills 0.09 ms (6 ns). At 2,000 modules those per-entity
figures are about 22, 11, 5 and 5 ns. The rise is the working set leaving cache.

What it says:

1. **The engine's iteration core is not the bottleneck.** Crafters and drills run at 4–10 ns per
   entity including real work. Scheduler overhead is negligible at this archetype count, though it
   allocates (see ENGINE_REQUESTS).
2. **Polling is.** A starved factory costs as much as a working one, and idle inserters cost
   slightly *more* than busy ones (27 vs 22 ns at 2k modules, 40 vs 37 at 8k), because waiting
   means re-scanning the source every tick. The original's design answer is sleeping entities woken
   by events, and it's the best case yet for the flag column + entity-addressed events discussed for
   the engine.
3. **Belt segments are pointer-chasing.** Each `LaneSegment` is an object with three arrays, so a
   segment tick is a few cache misses, not arithmetic. An SoA segment table (header arrays, pooled
   item storage) is the obvious next step and is entirely game-side.
4. Rough ceiling: at 60 UPS, ~16 ms buys about 1.5M buildings of this mix single-threaded before
   rendering, extrapolating linearly from 224k, which is optimistic given the cache trend above. That's not a target, just a sense of where it ends.

## Surfaces, the expansion, and ships

**A surface is the unit of everything:** its own `GameWorld`, grid, stores and system instances.
Surfaces share nothing during a tick, and anything crossing between them is a `Shipment` delivered by
the universe between ticks. Two consequences:

- They're the natural unit of **parallelism**: tick every surface on its own core. That's blocked
  only by the engine's process-global registries (ENGINE_REQUESTS).
- **Surface rules are data:** `TileKind` (ground, foundation, void), per-building `AllowedOn`, and
  per-recipe `SurfaceCondition`. Adding a planet with different rules is adding data, not systems.

**Ships.** Ships will be freely designed movable bodies that fly properly, not static platforms. The
design that makes that tractable is:

> A ship is a complete surface whose grid sits inside a moving `SurfaceFrame` (position, rotation,
> velocity, angular velocity). The factory inside doesn't know it's moving.

Belts, inserters and machines all work in grid coordinates exactly as on a planet, so ship-internal
logistics are solved for free. The ship *as a body* is a separate and much smaller problem: one rigid
transform per ship. Void tiles draw nothing, so the grid reads as a hull rather than a rectangle. The
slice proves the split. The starter ship drifts and rotates while its collector line runs, and
building on it picks tiles through the inverse frame.

Long-term direction, none of it built:

- **Flight.** Thrusters and gyros are buildings that draw power (from the ship's own network) and
  emit force and torque. A ship-physics step outside the factory systems sums them with mass and
  inertia from the grid (occupied tiles × per-kind mass, cached and updated on structural change) and
  integrates the frame. It runs once per ship, so its cost is irrelevant next to the factory.
- **Fixed-point frame.** The frame is floats today. Lockstep needs it in fixed-point like the rest of
  the sim; a binary-angle rotation plus 1/256-tile position is enough.
- **Growing hulls.** Freely designed ships grow in every direction, including negative coordinates.
  `SurfaceGrid` is bounded and origin-anchored. A ship grid needs re-origining (shift contents and
  compensate the frame position) or a chunked grid with signed chunk coordinates. Re-origining is
  simpler and rare.
- **Rendering.** It needs the engine's per-world draw transform (ENGINE_REQUESTS, single-world entry).
  Today every renderer transforms every quad by hand.
- **Space around ships** (where the frames live) becomes its own lightweight layer: frames, and later
  docking and transfer points. Ship-to-ship interaction, combat, and damage/splitting are out of scope,
  and nothing here assumes them.
- **Shipments** generalise from "silo to first receiver on the other surface" to "silo to a chosen
  ship or planet with transit time", which is the same between-ticks delivery, addressed.

## Roadmap (not built) and what each needs from the engine

| Feature | Shape | Engine pressure |
|---|---|---|
| Sleeping entities | active flag + wake on inventory/belt events | per-entity flag column with vector scan; events addressed to entities |
| Incremental belt topology | split/merge segments locally instead of a full rebuild | none (game-side) |
| Splitters, underground belts | new segment output kinds (round-robin, teleport link) | none |
| Fluids | pipe networks as segments with per-segment volume/flow (the original's 2.0 shape) | none; same store pattern as power |
| Trains | rail graph + block reservation; trains as moving entities with schedules | entity refs to stations (the declared handle); a different archetype churn profile |
| Logistic robots | job claims (provider → requester) that outlive a frame | the claims question from the agents design (RimWorld-style reservation) |
| Circuit network | per-network signal sums one tick behind, like power | none |
| Unbounded maps | chunked grid, chunk generation on demand | none, but rendering wants per-chunk draw lists |
| Save / load | serialise stores + components | external entity addressing (id-at-creation entry) |
| Lockstep multiplayer | command stream already exists; add input delay and a desync hash per N ticks | fixed tick; thread-safe registries if surfaces go parallel |
| Parallel surfaces | one task per surface per tick | per-world id allocation; frozen component registry |
