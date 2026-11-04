# Engine Requests — Proto4X.Factory

Gaps and hazards found while building a factory-automation vertical slice (belts, inserters,
machines, power, two surfaces, one of them a moving ship) on `MonoGameLibrary`. This is the factory
project's own log, kept separate from `Proto4X.Sandbox/ENGINE_REQUESTS.md`. Where an entry
overlaps one there, it says so and adds the new evidence rather than restating it.

Same conventions as the sandbox log. Open entries sort High → Medium → Low, resolved ones go last.
Every workaround lives in `Proto4X.Factory/`. Two entries, the culling-bounds fix and the camera rework, were made in
`MonoGameLibrary` itself, each with a one-off approval from Jason. Nothing else there was changed.

Measurements come from `Proto4X.Factory/Headless` (`dotnet run -c Release -- bench <modules>`), on
this machine, .NET 10, single-threaded. One module is 28 buildings (see `Layouts.StampSmeltingModule`).
Figures are medians of four runs per size, two in each variant order, after a JIT warm-up pass.
Without that pass, whichever variant is timed first reads about 2x slow at small sizes.

<!-- Template: see Proto4X.Sandbox/ENGINE_REQUESTS.md -->

## Cross-building exchange has no declared path into another archetype's data
**Status:** Open — worked around (game-owned stores)
**Priority:** High

<details>
<summary>Details</summary>

**What:** a factory is a graph of buildings trading items. An inserter iterating the `Inserter`
archetype must, in the same tick, ask a *different* building (chest, furnace, assembler, generator,
silo) "would you accept item X?" and then put the item in. The same goes for drills, which push into
whatever is in front of them. That's a transactional read-then-write into another archetype's
components. The engine has no shape for it today. The decided direction, a system-declared,
frame-bound archetype handle (sandbox log, "Entity-to-entity references through system-declared
archetype handles"), doesn't exist yet.

**Why it came up:** it is the core loop of the genre. Every item movement except belt-to-belt
travel crosses an archetype boundary.

**Tried instead:** this decision was made deliberately, not drifted into. Authoritative
**inventories live in a game-owned per-surface `InventoryStore`** (flat stack arrays). A building's
component holds its stack start index, and other buildings reach it by resolving a tile coordinate to
a building slot (`BuildingTable`). Behaviour state (crafting progress, swing phase, fuel energy)
stays in components. `Simulation/Exchange.cs` is the only code that crosses buildings, so **it is
effectively the spec for the declared handle**:

- resolve a building by a stable name (here a tile coordinate, valid because buildings never move);
- test, then write, its storage within one tick (`CanInsert` → `TryInsert`, `TryTakeFor`);
- honour per-kind rules about which stacks are inputs and which are outputs;
- refuse buildings that are being torn down.

Rejected alternatives:

1. **Request/apply.** Inserters post intents, and the target's own system applies them. It still needs
   a shared, mutable "room left" record per target, or two inserters can both see room for the last
   item and overfill it. That record is a store, so this is the store design plus a tick of latency
   plus a copy.
2. **Padded superset archetypes**, the sandbox's `Weapon Range=0` trick. Every building would carry
   every inventory shape. It doesn't scale past two kinds.

**What the handle needs, from the call sites:** lookups happen per *transfer*, not per tick per
entity. The per-tick path only compares a cached slot against a chunk version (next entry), so lookup
cost isn't critical. What matters is the declared read/write set, because `Exchange` writes into
three archetypes (crafters, generators, launchers) plus chest-shaped buildings with no behaviour
component at all.

**Related:** sandbox log, "Systems can only declare one AND-mask query; no cross-archetype
join/union". This is the same gap with a much higher call count, and it's the clearest case yet that
the join and the handle are the same feature.

</details>

## Deferred removal applies the removal set in insertion order — corrupts archetypes
**Status:** Open — reproduced, worked around
**Priority:** High

<details>
<summary>Details</summary>

**What:** `Archetype.ProcessDeferredUpdates` calls `DeferredEntityRemovalsByIndex.ToList().Sort(...)`.
That sorts a discarded copy and then iterates the `HashSet` itself, which enumerates in insertion order
after a `Clear`. Swap-remove is only correct in descending index order. This was already reported from
the events work. **New here: a deterministic reproduction and a concrete failure.**

**Reproduction:** `Headless/Scenarios.cs`, "removing several same-archetype entities in one tick". It
builds 28 belts and 28 chests, then removes x = 3, 7, 11, 28, 29 in one tick. With removals queued in
ascending order, the integrity probe fails: a removed building's entity survives (it was swapped into
an earlier hole before its own turn) and a live one is lost (its data is copied past `count`). The
condition is "a removal targets an index that an earlier removal in the same flush already swapped out
of range", so it needs the last few entities among those removed, which is rare in small tests and
routine in a factory (deconstructing a freshly built row).

**Tried instead:** `DeconstructionSystem` iterates each chunk backwards, so insertion order is
descending. That makes the test pass, but it depends on `HashSet<int>` enumeration order, an
implementation detail. Any other code path that queues removals (death systems, etc.) has to know
about it too.

**Possible approaches:** sort a real list and iterate that, e.g. `var order = set.ToArray();
Array.Sort(order); iterate backwards`, or keep the dedupe set and drain a sorted array.

</details>

## Core is single-world: no way to choose which GameWorld is drawn, or to give one a transform
**Status:** Open — worked around
**Priority:** High

<details>
<summary>Details</summary>

**What:** three linked limits, all on the path to multiple surfaces and moving ships:

1. `Core.World` is fixed, and `Core.Update`/`Core.Draw` are sealed, so the game can't change which
   world is simulated or drawn.
2. `SystemScheduler.Draw` opens its world pass with `Camera.GetCameraTransform()` and nothing else.
   A world can't carry its own transform.
3. There is one camera, with no per-world camera. Since the camera rework it can be set, so the factory stores a view per surface and writes it in (see the resolved camera entry).

**Why it came up:** each surface (planet, ship) is its own `GameWorld`, with its own stores and system
instances, and the player switches between them. A **ship is a surface whose grid moves**: freely built, moving ships, rather than the
original's static space platforms (see DESIGN.md, "Ships"). So its world needs `camera × shipFrame`
as its draw transform.

**Tried instead:**

- `Core.World` is an empty "meta" world holding only the HUD. Every surface is updated from
  `OnUpdate` (`Universe.Step` calls `SystemScheduler.Update(surface.World)`) and the active one is
  drawn from `OnDraw` via `SystemScheduler.Draw(..., surface.World)`. `OnDraw` runs before Core draws
  the meta world, so the HUD lands on top. That works, but only because of call order inside a sealed
  method.
- Ship transform: every renderer pushes every quad through `SurfaceView.TileToWorld` and passes the
  frame rotation by hand (`Game/Rendering/SurfaceView.cs`). That's per-sprite transform math on the
  CPU for what should be one matrix, and every renderer has to remember to do it.

**Possible approaches:** let a world (or a draw call) supply an extra matrix composed into `Begin`,
e.g. `SystemScheduler.Draw(spriteBatch, ui, world, Matrix worldTransform)`, or an `IWorldTransform`
the scheduler asks for. Separately, make "which worlds tick, which one draws" the game's call rather
than `Core`'s. This is the same "who owns the defaults" question `ENGINE_LAYERING.md` raises for
system registration.

</details>

## No sleeping: an idle factory costs about the same as a busy one
**Status:** Open — measured
**Priority:** Medium

<details>
<summary>Details</summary>

**What:** every entity a system declares is visited every tick. A starved inserter re-scans its
source, an idle furnace re-checks its inputs, and a depleted drill branches out. The original game's
central optimisation is the opposite: entities sleep and are woken by the change that matters
(inventory changed, belt item arrived).

**Measured** (ms/tick; "idle" means ore runs out immediately, so everything starves):

| Modules (buildings) | Busy | Idle | Inserters busy / idle (ns each per tick) |
|---|---|---|---|
| 2,000 (56k) | 0.36 | 0.37 | 22 / 27 |
| 8,000 (224k) | 2.43 | 2.19 | 37 / 40 |

A starved factory costs the same as a working one. **Idle inserters cost slightly more than busy
ones**: a waiting inserter re-scans its source every tick, while a swinging one only adds to a
counter. At 8,000 modules, 40k inserters doing nothing cost 1.6 ms/tick, about a tenth of a 60 UPS
budget. The only thing that gets cheaper when idle is belts (empty segments), at roughly half.
Per-entity cost also rises from 2k to 8k modules (inserters 22 -> 37 ns, belt segments 11 -> 22 ns)
as the working set leaves cache, so sleeping pays twice: it skips the work and the memory traffic.

**Tried instead:** nothing beyond gating `DeconstructionSystem` on a pending count. A game-side
"active list" would be a system-held entity list, which the entity-access direction rules out.

**Possible approaches:** this is the strongest use case found so far for the per-entity flag column
(memory: component change watching). An `Active`/`Asleep` flag the scheduler filters on with a
vector scan, set by the system and cleared by an inventory-change event addressed to the entity,
should take idle inserters from ~40 ns to roughly the cost of skipping a byte. That's an estimate
from the earlier flag-layout measurements, not measured here. The wake-up half needs events addressed
to entities (next entry).

</details>

## Nothing outside a system can change a component, so external writes become "store + version + pull"
**Status:** Open — worked around (pattern used three times)
**Priority:** Medium

<details>
<summary>Details</summary>

**What:** the player changes an assembler's recipe, a pole is placed that changes a building's power
network, a neighbour is built that changes where an inserter reaches. Each is a write into an existing
entity's component from outside its system. There's no path for it, and there shouldn't be an ad-hoc
one (sandbox log, "Ad-hoc random access... decided against").

**Tried instead:** one pattern, used three times. The new value goes into a game store and a version
stamp is bumped. The owning system compares the stamp, hoisted into a local, against a copy cached in
the component. On a mismatch it pulls the new value in and writes its own component.

- recipe: `BuildingTable.RecipeOf` + `Surface.ConfigVersion` → `CrafterSystem.PullConfiguration`;
- power network: `PowerGrid.Version` → every consumer's `PowerIndex`;
- wiring: per-32×32-chunk versions in `SurfaceGrid` → `Inserter`/`Drill` cached target slots. The
  chunk granularity is what stops one placement from re-resolving every inserter on the surface.

It's cheap on the hot path (one compare against a register), but the *granularity* is all-or-nothing:
a single recipe change makes every crafter on the surface take the slow branch once.

**Possible approaches:** events addressed to an entity, i.e. a dense index plus payload, delivered to
the owning system. That's the "change events carrying dense indices" idea from the 2026-09-15
component-change-watching discussion, and this use fits it directly. The version stamp then becomes "this entity has mail", which is again the flag
column.

</details>

## No fixed-tick concept
**Status:** Open — worked around
**Priority:** Medium

<details>
<summary>Details</summary>

**What:** systems receive MonoGame's `GameTime` and nothing else. A factory sim must be a pure
function of (state, commands, tick count) for lockstep multiplayer, replays and the determinism
check. That means integer ticks, no variable delta, and no float accumulation.

**Tried instead:** `Universe` counts ticks and hands every system the same synthetic 1/60 s
`GameTime`. All simulation timing is integer ticks × integer power satisfaction. In the game, MonoGame's
default fixed timestep makes one `Update` one tick. `TimerSystem` isn't used (it's real-time and
auto-registered only on `Core.World`). The headless determinism check (same commands → same hash;
a one-tick-later command → different hash) passes.

**Possible approaches:** an engine `Tick` (frame number, plus "this is a sim tick" vs "render
frame") passed to update systems, and a fixed-step driver in `Core` with catch-up. That would also
give systems a place to read the tick without reaching into game state.

</details>

## No entity id at creation — a factory needs a name before the entity exists
**Status:** Open — worked around
**Priority:** Medium

<details>
<summary>Details</summary>

**What:** same gap as the sandbox log's "Game code has no way to obtain an entity id", half (2). New
evidence: a factory needs to name a building **at placement**, before its entity is flushed. The
occupancy grid must say "tile occupied by X" immediately, or two placements in one command batch
could overlap.

**Tried instead:** `BuildingTable` assigns a slot at placement. The slot goes into the `Building`
component, so the entity can find its row, and into the grid, so a tile can find the building. It's a
second id space beside the engine's, with its own free list and its own stale-reference rules (cached
slots are guarded by chunk versions). Removal is two-phase: mark, then the entity is found by scanning
`Building` components (`DeconstructionSystem`), then the stores are released after the flush.

**Possible approaches:** the archetype handle (High entry above) plus ids at creation would let the
grid hold entity ids and remove `BuildingTable`'s identity role. It would still hold the configuration
other buildings read.

</details>

## Scheduler re-queries every tick and allocates doing it
**Status:** Open — measured
**Priority:** Medium

<details>
<summary>Details</summary>

**What:** `SystemScheduler.Update`/`Draw` call
`_cachedComponentArrays.GetValueOrDefault(system, gameLayer.QueryRelevantComponentArrays(...))`.
The fallback argument is evaluated on every call, and nothing ever writes `_cachedComponentArrays`,
so the query runs per system per frame. `Archetype.GetMatchingComponents` does a LINQ `Where` +
`ToDictionary` + `AsReadOnly` per matching archetype, on top of the `Select` + spread + mask build in
the scheduler. `ArchetypeChunk.GetSpan<T>` then does a dictionary lookup and a cast per chunk.

**Measured:** with every system body removed (same declarations), a tick costs ~0.002-0.005 ms and
**5.7 KB of allocation**, independent of entity count, for one surface with ~10 archetypes and 8
systems. That's small now. It scales with surfaces × systems × archetypes, and a game with dozens
of ships each running the full factory system set multiplies it directly.

**Possible approaches:** cache per (system, world) and invalidate on a world-level archetype-set
version, which is the structural version stamp from the spatial-indexing prep list. Build the column
lookup once per chunk view, not per query.

</details>

## Engine registries are process-global — blocks ticking surfaces in parallel
**Status:** Open
**Priority:** Medium

<details>
<summary>Details</summary>

**What:** `ComponentTypeRegistry`, `EntityIdRegistry` and `SpriteLibrary` are static and unsynchronised
(`EntityIdRegistry.NextId++`, a plain `Dictionary` in `ComponentTypeRegistry`). Surfaces in this
design share nothing during a tick (cross-surface traffic is queued and delivered between ticks), so
they are the natural unit of parallelism. Planets and ships could tick on separate cores, except that
an entity flush on two worlds at once races on the id counter.

Also, `ComponentTypeRegistry.GetId` checks `id > 128` where `ComponentTypeMask.FromTypeId` checks
`>= 128`, so the 129th component type is registered and then throws somewhere far from the cause.

**Possible approaches:** per-world entity id allocation (ids only need to be unique per world if
references never cross worlds, and here they don't), and freeze the component registry after startup.

</details>

## Every world needs a system of every kind, or the scheduler throws
**Status:** Open — hit again
**Priority:** Low

<details>
<summary>Details</summary>

Same bug as the sandbox entry "SystemScheduler throws on a GameWorld with no system of a given kind".
New data point: with one meta world plus N surfaces, each needs a screen-space system purely to exist.
`SurfaceStatusRenderer` exists partly for that, and the meta world is only safe because `Core`
auto-registers `Renderer`/`TimerSystem` on it. The headless runner never calls `Draw`, so it's only
safe there by accident. `TryGetValue` at the three sites would close it.

</details>

## One AND-query per system: diagnostics need one system per component type
**Status:** Open
**Priority:** Low

<details>
<summary>Details</summary>

The determinism hash needs one `ComponentHashSystem<T>` per hashed component type (five), and the
headless probe needs four systems to answer "what's in everyone's hands". Neither is a hot path. It's
listed because inspection, save/load and debugging all want "visit every entity, whatever it is", and
today that's a system per archetype shape. Related to the sandbox log's join entry.

</details>

## Primitives written twice now
**Status:** Open — promotion candidate
**Priority:** Low

<details>
<summary>Details</summary>

`Game/Rendering/Primitives.cs` copies the sandbox's line/rectangle helpers and adds rotated rectangles.
That's the second independent copy, which is the "duplication that happened" bar `ENGINE_LAYERING.md`
sets for promotion into the base engine.

</details>

## Culling bounds use the forward camera transform
**Status:** Resolved 2026-10-16 (engine fixed directly, one-off approval from Jason)
**Priority:** Medium

<details>
<summary>Details</summary>

`UserInterfaceManager.GetVisibleScreenBoundsAABB` (`MonoGameLibrary/UI/UserInterfaceManager.cs:37`)
transforms the viewport corners by `Camera.GetCameraTransform()`. That matrix maps world to screen
(it's what `SystemScheduler.Draw` passes to `SpriteBatch.Begin`), so the `cullingBounds` every
`IDrawSystem` receives is the screen pushed through world-to-screen, not the visible region of the
world. Panning moves the box the opposite way to the view, and zooming in grows it when it should
shrink. A renderer that culls with it draws a patch that slides away from the camera, which also
makes arrow-key navigation look inverted.

Never visible before because no existing renderer reads `cullingBounds` (the base `Renderer` has its
check commented out, and the sandbox's renderers ignore it). The factory's tile renderers were the
first to cull. Mouse picking everywhere already uses `Matrix.Invert(camera.GetCameraTransform())`.

**Fix:** `GetVisibleScreenBoundsAABB` now transforms by `Matrix.Invert(Camera.GetCameraTransform())`.
That one line is the only change. The factory's interim `CameraView` workaround was removed, and its
renderers use `cullingBounds` again. **Still open, not changed:** the method allocates on every draw
call (an array, an iterator and four LINQ passes in `ToAABB`).

</details>

## Camera can't be positioned or follow anything
**Status:** Resolved 2026-10-16 (engine changed directly, second one-off approval from Jason)
**Priority:** Low

<details>
<summary>Details</summary>

Same as the sandbox entry "Camera position can only be changed by the arrow keys", with two new
needs: following a moving ship, and a camera position per surface. The engine's arrow keys also
panned the world rather than the view, which felt inverted.

**Fix:** `MonoGameLibrary/UI/Camera.cs` now has no input bindings and holds two matrices:
`WorldProjection` (world-side, identity by default, the slot for isometric experiments) and `ScreenTransform`
(screen-side, baked, origin at the viewport's top-left). `ScreenTransform` changes through screen-space
operations: `Pan(screenDelta)`, `ZoomAt(screenPoint, factor)`, which clamps the step to the
`MinZoom`/`MaxZoom` constructor arguments, and `RotateAround(screenPoint, radians)`. Either matrix can
also be set directly. `Zoom` is read back out of `ScreenTransform`. On a resize the view shifts by half the size
change, so its centre stays put. `ScreenTransform` starts as identity, so world (0, 0) is still at the top-left.
Games supply a camera with other limits by overriding `Core.InitializeCamera`.

Each game now owns its bindings: `Proto4X.Sandbox/Systems/CameraControlSystem.cs` and
`Proto4X.Factory/Game/Input/CameraController.cs`. The factory's version keeps each surface's view as
a grid-local-to-screen matrix and rebuilds `ScreenTransform` from it against the moving frame every frame, which
gives ship follow and per-surface views. **Proto4X has no camera controls since this change**: its
bindings were the engine's, and nothing replaced them.

**Not handled:** a perspective matrix in `WorldProjection` draws, but `ScreenToWorld` and culling use
`Vector2.Transform`, which skips the perspective divide. Isometric with upright sprites would also
need the scheduler to draw sprites without the projection, which it can't choose per renderer yet.

</details>
