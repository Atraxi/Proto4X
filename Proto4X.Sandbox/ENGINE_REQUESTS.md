# Engine Requests

Requests filed from `Proto4X.Sandbox` while building a minimal playable loop against `MonoGameLibrary`. Each entry: what's missing, why it came up, what was tried instead, and possible approaches. This doubles as one of Jason's TODO lists — sorted open-and-high-priority first, down to resolved/closed at the bottom. Pull items into conversation or a GitHub issue when you want to act on one.

**Sort order:** Open items first (sorted High → Medium → Low priority), then Resolved/closed items at the bottom (priority no longer drives sort once closed). Default new ideas — things Jason raises as possible enhancements without discussing urgency — to **Low** priority; only mark Medium/High when there's a concrete reason (blocks a planned mechanic, recurring pain, discussed as important).

<!-- Template:

## <short title>
**Status:** Open | Open — partial | Open — discussed | Open — worked around | Resolved | Resolved — decided against
**Priority:** High | Medium | Low

<details>
<summary>Details</summary>

**What:** what the engine can't currently do.

**Why it came up:** the specific thing in the sandbox that needed it.

**Tried instead:** any workaround attempted or considered and rejected, and why.

**Possible approaches:** rough shape of a fix, if any come to mind. Not a spec.

</details>

Keep **Status:**/**Priority:** outside the <details> block so they're visible without expanding. Leave a blank line after <summary> and between each field — GitHub only parses markdown (bold, lists) inside an HTML block if it's blank-line-separated from the tags. Re-sort the file (see Sort order above) whenever an entry's status or priority changes.

-->

## Systems can only declare one AND-mask query; no cross-archetype join/union
**Status:** Open — discussed
**Priority:** High

<details>
<summary>Details</summary>

**What:** `SystemBase.RequiredComponentProviders` is a single `ComponentTypeMask`, and `SystemScheduler` hands a system only the archetype chunks whose mask `ContainsAll` that one query. There's no way for a system to express "OR"/"NOT" over component masks, and no way to receive entities from two genuinely different archetypes (e.g. "the player" vs. "resource nodes") in the same `Update` call to relate them to each other — each system pass is scoped to one AND-query's worth of matching archetypes.

**Why it came up:** `GatherSystem` (`Proto4X.Sandbox/Systems/GatherSystem.cs`) needs to check each resource node's distance to the player. Resource nodes are `Position+Gatherable`; the player is `Position+Motion` — different archetypes, neither a subset of the other, so one `RequiredComponentProviders` mask can't select both in the same call.

**Tried instead:** queried only `Position+Gatherable` (the resource nodes) and read the player's `Position` separately via `GameWorld.GetComponentsForEntity(playerEntityId)`, the same single-entity random-access path `UserInterfaceManager` already uses for the WASD-selected entity. Works, but only because there's exactly one player; doesn't generalize to "any two groups of entities need to interact." This kind of interaction (combat, docking, trading, gathering) is central to a 4X game, so this will keep recurring.

**Possible approaches:** discussed with Jason — two different problems worth separating:

1. A query wrapper over `ComponentTypeMask` supporting AND/OR/NOT combinations. Still cheap (same bitmask-logic character `ContainsAll`/`ContainsAny` already have), and would at least let a system's *one* query express "entities with A and (B or C) but not D" — doesn't by itself solve joining across archetypes, but is a small, self-contained first step.
2. Actual join/union queries — letting one system `Update` call see entities from multiple unrelated archetypes together, or relate entities across them — is a fundamentally harder and more computationally expensive problem (more like a relational join than a bitmask test). Would be considerably more broadly useful than (1) if it can be made generic and performant, but isn't a small addition. Not scoped or committed to.

**Update 2026-08-02 — three concrete costs, now that the sandbox is a real RTS.** Building combat, gathering and production against this constraint forced three distinct workarounds, which between them are a decent argument for how much (1) alone would actually buy:

- **Padding entities with components they don't want, to force them into one query.** `CombatSystem` needs shooters and their victims in the same call, so *every* combat participant shares one component set — including unarmed miners, which carry a `Weapon` with `Range = 0` purely to be visible to the query (`Proto4X.Sandbox/Components/Weapon.cs`). It works, and declaring the whole "combat quad" as an `IDependsOnOtherComponents` dependency even makes the invariant self-enforcing, but the archetype now encodes "what queries need to see me" rather than "what I am". Note this is a case AND/OR/NOT wouldn't fix either — it's the join that's missing, not the mask expressiveness.
- **Per-frame snapshot publishing, one system per archetype.** `AsteroidIndexSystem` and `StationIndexSystem` exist only to copy positions out of an archetype into a flat `List<Vector2>` so that systems querying a *different* archetype can read them. Cheap here, and it reads fine, but it is a copy per frame per relationship.
- **Snapshots are read-only, which changed the game design.** A snapshot gives a reader no way to write back to the source archetype, so asteroids in this build are **infinite** — finite ore would need miners to decrement a `Gatherable` they can't reach. That's a design decision made by an engine limitation rather than by choice, which is the clearest signal so far that this one matters.

**Also worth noting:** the O(n²) sweep in `CombatSystem` is *inherent* to "nearest hostile", not caused by this gap — a join would remove the archetype gymnastics, not the pairwise distance test. Spatial partitioning is a separate, later problem, and not one the sandbox needs at this scale.

</details>

## Game code has no way to obtain an entity id — from a chunk, or from the entity it just created
**Status:** Open — designed around
**Priority:** High

<details>
<summary>Details</summary>

**What:** two halves of the same gap, which are only fatal together:

1. **No id from a chunk index.** `ArchetypeChunk` exposes `Get<T>(index)`/`GetSpan<T>()` for component data and `QueueEntityForRemovalByIndex`/`ById` for removal, but nothing maps a chunk index back to the owning entity id. `Archetype.DenseIndexToEntityId` holds exactly that mapping but is private, and `ArchetypeChunk` only holds an `IArchetype` handle narrow enough for removal.
2. **No id from creation either.** `GameWorld.AddEntity(EntityBuilder)` returns `void`, and the id isn't even assigned at call time — `Archetype.AddEntity` calls `EntityIdRegistry.GetNextFreeId()` later, inside `ProcessDeferredUpdates`. So a caller can't be handed the id, and can't predict it: which pending entity gets which id depends on `Archetype` iteration order across a `Dictionary`.

Between them, **no game-side code can obtain a valid entity id for anything it didn't hardcode.** The only ids that exist today are the "spawned first, so it must be 0" convention the previous sandbox build relied on — which is exactly what stops working the moment a game builds units at runtime.

**Why it came up:** the sandbox is now a small RTS with dozens of units built during play (`Proto4X.Sandbox/DESIGN.md`). Every natural design here wants an id: which units are selected, which enemy a fighter is shooting, which asteroid a miner is assigned to, where a station's new ship went.

**Tried instead — and this is the interesting part:** rather than hardcoding ids, the whole game was designed to **never use an entity id at all**, and it worked out better than expected:

- **Selection is component state, not a list of ids.** `Selectable.IsSelected` is a bool on the entity (`Components/Selectable.cs`). Selecting and ordering become ordinary chunk iterations, and a unit that dies stops being selected for free, with no list to invalidate. This is *more* ECS-idiomatic than the id-list version would have been and is worth keeping regardless.
- **References become proximity queries.** Combat re-picks "nearest hostile in range" every frame instead of storing a target. Miners find rocks and drop-off points by nearest-position lookup against a snapshot.
- **Spawning describes rather than creates** (see the deferred-addition entry below), so nothing needs the id of what it just built.

**So this is no longer blocking the sandbox** — but only because the game was shaped around it. Two things it still costs, which no amount of design gets around: an entity cannot hold a *stable* reference to another entity (a fighter can't keep chasing one specific target through a crowd), and nothing can be addressed from outside the world (a save file, a scripted scenario, a network message, a debugger command all need a name for an entity).

**Possible approaches:** (1) is small and self-contained — expose `ArchetypeChunk.GetEntityId(index)`, or a `ReadOnlySpan<int>` of ids alongside the component spans, backed by the `DenseIndexToEntityId` array that already exists. Read-only, no new structural-mutation surface, same cost character as `GetSpan<T>`. (2) is more awkward given ids are deliberately assigned at flush time; the honest options are to assign the id at `AddEntity` time and return it, or to hand back a deferred handle that resolves after `ProcessDeferredUpdates`. Worth noting (2) overlaps heavily with the `EntityView` design below — the generation-stamped id discussed there would answer "what do I hand back" for both.

</details>

## No text rendering
**Status:** Open — worked around
**Priority:** High

<details>
<summary>Details</summary>

**What:** no `SpriteFont` loading or `SpriteBatch.DrawString` usage anywhere in `MonoGameLibrary` or either game project.

**Why it came up:** the MVP loop ends in a stockpile count that needs to be shown somewhere — there's currently no way to render any text at all, HUD or otherwise.

**Tried instead:** sidestepped for the MVP by writing the stockpile count to `Window.Title` each frame (`Proto4X.Sandbox/Sandbox.cs`) instead of drawing it in-world. Fine for a single always-visible counter, not a real HUD solution — doesn't generalize to positioned/multi-value/styled text, and will need solving properly as soon as more than one value needs showing at once.

**Update 2026-08-02 — the workaround is now at its ceiling.** The RTS build has a real HUD's worth of state, and all of it is crammed into one title-bar string (`Sandbox.BuildStatusLine`): minerals, own miner/fighter counts, enemy counts, the build menu with its hotkeys and prices, and the victory/defeat message. It is legible, but it is one line of unstyled text in the window chrome, which means:

- **No per-entity text at all** — no unit labels, no build-progress countdown over the station, no floating damage numbers. Anything attached to a position is simply unavailable.
- **The build menu is hotkeys-only** (`[1] Miner 50  [2] Fighter 75` in the title bar) because a clickable button needs a drawn label. Fine for an RTS, which has hotkeys anyway; not fine for the next thing that wants a menu.
- **Nothing can be emphasised**, so "you can't afford this" and "you can" look identical.

Non-text overlays turned out to be easy to solve game-side: `Rendering/Primitives.cs` stretches a 1x1 white texture into lines, circles and bars, which is enough for selection rings, health bars, weapon beams and the drag rectangle. Text is the only thing that genuinely can't be built out of that.

**Possible approaches:** likely just a `SpriteFont` content item plus a thin `IDrawSystem` (or a thing `UserInterfaceManager` can do directly) that draws a string at a screen-space position, unaffected by the world camera transform used for the rest of `SystemScheduler.Draw`. Note this needs the screen-space draw pass below to land first, or it inherits the same camera-transform problem.

</details>

## A system with no component query can't exist — which is exactly what a HUD is
**Status:** Open
**Priority:** High

<details>
<summary>Details</summary>

**What:** `ISystem.RequiredComponentProviders` has no default and no empty case. A system that queries nothing at all fails twice over:

1. `SystemScheduler` builds the mask with `ComponentTypeMask.FromTypes([.. RequiredComponentProviders.Select(...)])`, and `FromTypes` opens with `FromType(componentTypes[0])` — an empty array throws `IndexOutOfRangeException`.
2. Even if it didn't, the semantics would be wrong in the other direction: an all-zero mask satisfies `ContainsAll` for *every* archetype, so a "no query" system would receive every chunk in the world rather than none.

And a third gate behind those: `Update`/`Draw` both skip a system entirely when `archetypeChunks.Count == 0`, so a system that legitimately matches nothing this frame never runs at all.

**Why it came up:** assessing what's actually still missing for a HUD once `DrawSpace.Screen` lands. A mineral counter, a build menu, a victory banner — none of these are attached to an entity. There is no archetype for "the HUD", so there is no query to declare, so the system can't be expressed. `DrawSpace.Screen` gives such a system the right *coordinates* but no way to get *invoked*.

**Tried instead:** nothing yet — the sandbox HUD is still a `Window.Title` string built in `Sandbox.OnUpdate`, i.e. outside the system model entirely. That's what the workaround has always been, and it's why the limitation hasn't bitten before: the HUD has never been a system.

**Possible approaches:** several shapes, roughly increasing in scope:

- Give `RequiredComponentProviders` a default of `[]` on `ISystem` (matching `RunAfter`), define the empty mask as "matches nothing", and call such systems once per frame with an empty chunk list. Smallest change; makes "no query" mean what it looks like it means.
- Drop the `archetypeChunks.Count > 0` guard, or make it opt-in, so systems can choose to run on empty. Needed regardless of the above — a system that wants to draw "no units selected" has to run in the frame where nothing matches.
- A separate, explicitly entity-free system kind. Cleanest to read, but it's another type in the hierarchy, which is the shape the `ISystem`/`DrawSpace` rework just moved away from.

Worth deciding alongside text rendering, since the HUD is the first thing that needs both and neither is much use alone.

</details>

## No screen-space draw pass — every draw system is inside the camera transform
**Status:** Open — worked around
**Priority:** Medium

<details>
<summary>Details</summary>

**What:** `SystemScheduler.Draw` wraps the entire draw loop in a single `spriteBatch.Begin(transformMatrix: camera.GetCameraTransform())` / `End()` pair. Every `IDrawSystem` therefore draws in world space, through the camera. There's no second pass, no per-system opt-out, and no hook between `Begin` and `End` — so nothing can draw in screen space: no HUD, no minimap, no fixed-position overlay, and nothing that should stay a constant size on screen while the world zooms.

**Why it came up:** the box-select rectangle is defined by two mouse positions, i.e. it is a *screen* rectangle, but it has to be drawn through a world-space camera transform.

**Tried instead:** `SelectionOverlayRenderer` transforms each of the drag rectangle's four screen corners back into world space individually and draws it as four lines. This is actually correct under camera rotation and zoom — more correct than converting to a world AABB would be — so it's a genuinely fine answer for a *shape*. It is not an answer for a HUD, and it does mean overlay line thickness scales with zoom, which is wrong for UI even though it looks fine for in-world decoration.

**Possible approaches:** a second draw list — e.g. an `IScreenDrawSystem` (or a flag on `IDrawSystem`) collected separately and drawn in its own `Begin`/`End` with no transform, after the world pass. Also worth deciding at the same time whether draw *order* should be explicit rather than "order the systems were registered in", which is what it is today (see the `Drawable` entry below).

</details>

## Drawable carries no tint, scale or draw order
**Status:** Open — worked around
**Priority:** Medium

<details>
<summary>Details</summary>

**What:** `Drawable` holds a sprite id and an optional source rectangle, and `Renderer.Draw` hardcodes everything else: `Color.White`, scale `1`, `SpriteEffects.None`, `layerDepth: 1`. So an entity can't be tinted, scaled, faded, or flipped, and since the batch is `SortMode.Deferred`, `layerDepth` does nothing — draw order is purely the order systems were registered in, with no ordering *within* a system's chunk iteration.

**Why it came up:** an RTS needs faction colours (two sides, visually distinguishable at a glance) and would like damage flashes and fade-outs.

**Tried instead:** `Game/SpriteSet.cs` bakes a recoloured copy of every ship and station texture at load — `GetData`, multiply RGB by the faction tint, `SetData` into a new `Texture2D`, register each as its own `SpriteLibrary` entry. That's fine for a fixed, small palette of factions and needs no engine change at all, which is why it wasn't logged as a blocker. It does not extend to anything *dynamic*: a per-frame damage flash or a fade-on-death can't be pre-baked, and the texture count multiplies by faction count.

**Possible approaches:** add `Color Tint`, `float Scale` and a layer/order value to `Drawable` and pass them through in `Renderer`. Cheap in bytes and reads naturally, but worth a thought about whether ordering belongs on the component (a sort per frame) or stays a property of system registration order — the latter is free and has been sufficient so far.

</details>

## No way to spawn or destroy an entity from inside a running system
**Status:** Open — partial
**Priority:** Medium

<details>
<summary>Details</summary>

**What:** `Archetype` already had working deferred-queue machinery (`QueueEntityForAddition`, `QueueEntityForRemovalById`/`ByIndex`, `ProcessDeferredUpdates`, which `SystemScheduler.Update` calls after every system runs), and `GameWorld.RemoveEntity(entityId)` reached that queue — but only from outside a system's `Update`. A running `IUpdateSystem.Update` was only ever handed `List<ArchetypeChunk>`, with no reference to `GameWorld` or the owning `Archetype`.

**Why it came up:** any dynamic mechanic beyond "consume an existing entity" needs this — a projectile system spawning a shot, a resource node splitting, a health system killing an entity at 0 HP already works (removal), but nothing can spawn from inside a system yet.

**Resolution — removal:** `ArchetypeChunk` now carries an `IArchetype` and exposes `QueueEntityForRemovalById`/`QueueEntityForRemovalByIndex` as a minimal, read/write-free handle. A system can now queue removal of an entity it's currently iterating. Along the way, a real batching bug got fixed: `Archetype.ProcessDeferredUpdates` processed `DeferredEntityRemovalsByIndex` in queue order rather than descending index order, which corrupted `EntityIdToDenseIndex` for an unrelated live entity whenever two-or-more removals landed on the same archetype in one frame. The fix (`DeferredEntityRemovalsByIndex.Sort((A, B) => B - A)`) is now active.

**Update 2026-08-02 — the "describe, don't create" pattern works, and the rejection looks right.** Station production in the RTS build is exactly the case this entry predicted ("a gather system consuming a node and spawning a cargo pickup"): a station is `Position+Faction+Health+Timer+Weapon+Selectable+Station+Drawable` and the ship it builds is nothing like that shape, so forwarding to the chunk's own archetype would indeed have been wrong. `ProductionSystem` instead appends a plain `SpawnRequest` record (`Game/SpawnRequest.cs`) — ship type, faction, location — and `Sandbox.OnUpdate` drains the list through `GameWorld.AddEntity` after `SystemScheduler.Update` has returned. Costs one small allocation-free list and a few lines in the game loop, and it lands outside the systems loop, which keeps the synchronous-new-archetype branch of `AddEntity` safe (see the accessibility note below).

**Sharper reason the current boundary is load-bearing, found while writing this:** adding an entity can *reallocate* component storage. `Archetype.GrowEntityCapacity` calls `ComponentColumn<T>.Grow`, which is `Array.Resize` — a **new array**, leaving the old one orphaned. `ArchetypeChunk.GetSpan<T>()` hands out a `Span<T>` over whatever array `Values` pointed at when it was called. So if an addition could ever be flushed mid-frame while a system held a span, that system would silently keep reading and writing the *old, discarded* array — every write lost, with no error anywhere. Today this is unreachable: growth only happens in `ProcessDeferredUpdates` (after every system has returned) or in `GameWorld.AddEntity` from outside the systems loop, and spans never outlive a system's `Update` call. Worth stating explicitly because it means "adds are deferred" isn't only about archetype-shape correctness — it's also what keeps every outstanding `Span<T>` valid, and any future design that flushes additions earlier has to answer this. Worth recording that this only stays painless because **nothing needs the id of what it just built** — if it did, this would collide head-on with the entity-id entry above.

**Still open — addition:** deliberately *not* exposed on `ArchetypeChunk`/`IArchetype`. The rejected shape would have forwarded blindly to the chunk's own archetype regardless of the new entity's actual composition — fine for "spawn another one just like me," broken (wrong archetype, missing/garbage columns, or a `KeyNotFoundException`) the moment a system wants to spawn something differently shaped, e.g. a gather system consuming a node and spawning a cargo pickup. Addition stays `GameWorld`-only (mask-based lookup-or-create) and unreachable from inside a system for now — punted rather than shipped unsafe.

**Sanity-check findings on `Archetype` accessibility, still open** (requested separately, logged here since it's the same surface):

- `RemoveEntityWithId`/`RemoveEntityAtIndex` are `internal`, not `private` — the same immediate, unqueued structural mutation that `AddEntity` just got locked down to prevent (it's now `private`) is still reachable by any other code in the `MonoGameLibrary` assembly. `RemoveEntityWithId` isn't called from anywhere today (dead). Recommend tightening both to `private` for consistency with `AddEntity`.
- `GrowEntityCapacity()` is `protected` on a class with no subclasses — vestigial, not a safety issue, just worth `private` in a cleanup pass.
- `WriteComponentById<T>`/`WriteComponentByIndex<T>` (both `internal`) are unqueued by design and that's fine — they only overwrite an existing slot's value, never touch `count`/`DenseIndexToEntityId`/`EntityIdToDenseIndex`, so there's no structural state to corrupt regardless of call timing. This is the path `UserInterfaceManager` already uses for WASD input via `ComponentContainerAttached<T>.WriteComponentById` — legitimate, not a hole.
- `GameWorld.AddEntity`'s two branches have different immediacy: a brand-new archetype composition is constructed and added synchronously, an existing one is deferred via the queue. **Discussed and accepted as safe today** — a freshly created archetype can't have any in-flight `ArchetypeChunk` referencing it, and both current call sites (`LoadContent`, `UserInterfaceManager`'s right-click spawn) run outside `SystemScheduler.Update`'s systems loop, so nothing is enumerating `Archetypes` at the moment it mutates. **Caveat:** this holds only as long as systems can't reach `GameWorld`. If that boundary ever changes, the two branches would start differing in same-frame visibility too (new-composition spawns become visible to later systems in the same frame; existing-composition spawns don't become visible until next frame) — worth a second look at that point, not now.

</details>

## EntityView — entity lifecycle handle
**Status:** Open — design discussed, not yet built
**Priority:** Medium

<details>
<summary>Details</summary>

**What:** a proposed lightweight per-entity handle utility, in response to "what's the path to interact with a specific entity" now that `GameWorld` access, queueing, and removal are split across a few different surfaces (`GameWorld`, `IArchetype`, `ArchetypeChunk`).

**Intended shape and constraints:**

- Carries only: the entity id, a `GameWorld` reference, and possibly an `IArchetype` handle. **Explicitly does not carry component data or values.**
- Purpose is lifecycle interaction (remove, requeue, existence checks) — not a data-access convenience.
- Deliberately designed to be awkward to build an OOP-style architecture on top of — actively resisting becoming a `GameObject`-style handle that invites storing long-lived references and treating entities as objects, which would erode the archetype/SoA model this engine is built around.
- Stale-id handling is an acknowledged open question, raised in review and agreed as valid: an `EntityView` can outlive the entity it names (removed elsewhere, handle held on). Not yet decided whether `Get`/lifecycle calls on a dead handle throw, return a bool/nullable, or something else — needs a deliberate answer before this ships, not an accidental one.
- Because ids don't map to a stable dense index across frames (swap-remove), any future data-returning method on a handle-style object would necessarily return copies, not `ref`s — different shape than `ArchetypeChunk.Get<T>(index)`/`GetSpan<T>()`, which is part of why data access is being kept off `EntityView` entirely rather than added later.
- Should stay aware that `GameWorld` access is currently a global static (`Core.Instance.World`, already the existing pattern via `UserInterfaceManager`'s right-click spawn) as an intentional MVP placeholder, not a permanent design — the expected evolution is toward "the currently selected/user-active `GameWorld`," with other worlds tracked separately through a process not yet designed. `EntityView` holding a `GameWorld` reference directly (rather than resolving one implicitly) is likely the right call precisely because it doesn't bake in a single-world assumption any harder than necessary.

**Second, distinct motivation surfaced later (2026-07-21):** the original design above is about lifecycle *interaction* (remove/requeue/existence). A separate but compatible use case showed up discussing `GameWorld.TryGetComponent<T>`/`SetComponent<T>` (the public single-entity component API that replaced the internal-type leak from `GetComponentsForEntity`/`ComponentContainerAttached<T>`): those calls re-resolve an entity's archetype/dense-index independently on every call, so a caller holding an id across two calls (e.g. read-then-later-write) has no protection if the entity was structurally removed in between — today this can't actually happen mid-frame (`SystemScheduler` is synchronous/single-threaded and structural changes are deferred to `ProcessDeferredUpdates`), but it's an unenforced invariant, not a guaranteed one, and would become a real hazard the moment anything defers between an entity's read and its write (an `await`, a future parallel scheduler, etc). A generation-stamped id (id + version bumped on removal, compared at access time to fail loud instead of silently writing to a resurrected/reused id) would cover this too, and fits naturally on the same handle rather than being a second bolt-on mechanism. Doesn't change the shape or constraints above — still id + `GameWorld` ref, still no component data, still lifecycle-only — just widens why a stale handle needs a defined answer, and makes the stale-id question above less theoretical.

**Related open question, not decided:** whether to remove single-entity/random-access reads from the engine entirely — specifically `ArchetypeChunk.Get<T>(index)` — to push the architecture harder toward "systems + iteration" as the only access pattern, with `EntityView` handling lifecycle-only concerns instead. Acknowledged tension: UI selection/interaction (the WASD-selected-entity flow in `UserInterfaceManager`, `UserInterfaceManager.cs:57-110`) currently depends on exactly this kind of single-entity random access to read/write the selected entity's `Position`/`Motion`. Unclear yet how restrictive a pure iteration-only model would be for cases like this. Left open.

</details>

## No shared Motion→Position integration system
**Status:** Open — discussed
**Priority:** Low

<details>
<summary>Details</summary>

**What:** `MonoGameLibrary` has no engine-level system that integrates `Motion` (velocity/acceleration) into `Position` each frame — `Renderer` and `TimerSystem` are provided and auto-registered by `Core.Initialize`, but the accel→velocity→position step is not, even though nothing about that integration is game-specific in principle.

**Why it came up:** `Proto4X.Sandbox` needed a `MovementSystem` to make WASD input (which only ever sets `Motion.Acceleration`, via `UserInterfaceManager`) actually move anything. `Proto4X.Systems.MovementSystem` already does this, but `Proto4X.Sandbox` deliberately doesn't reference the `Proto4X` project (it's Jason's separately hand-written game, not a library), so a second copy was written from scratch (`Proto4X.Sandbox/Systems/MovementSystem.cs`) rather than sharing it.

**Tried instead:** wrote the sandbox's own copy, but stripped down — no max-speed/max-angular-velocity clamps and no rotation-wrapping, both of which `Proto4X.Systems.MovementSystem` has. So the two copies aren't just duplicated, they've already silently diverged in behavior.

**Possible approaches:** promote a configurable version to `MonoGameLibrary` alongside `Renderer`/`TimerSystem`. Discussed with Jason: prefer a constructor-injected tuning struct (friction, max speed, max angular velocity, wrap-rotation-or-not) over virtual override hooks — a hot per-entity loop is exactly where virtual dispatch cost isn't wanted, given the engine's stated performance priority. This would only cover "same formula, different numbers" variance; a game wanting genuinely different integration math (grid snapping, no inertia, verlet) would still write its own system, which is fine. Explicitly raised as a possible enhancement, not discussed as urgent — not an MVP blocker, not needed for the current sandbox loop.

</details>

## A non-repeating Timer never disarms — HasJustElapsed is a level, not an edge
**Status:** Resolved
**Priority:** Medium

<details>
<summary>Details</summary>

**What:** `TimerSystem.Update` clears `HasJustElapsed` at the top of each frame, then decrements `Remaining` and re-raises `HasJustElapsed` whenever `Remaining <= 0`. For a repeating timer that's correct, because `Remaining` is reset to `Duration` in the same breath. For a **non-repeating** timer, `Remaining` just keeps going more negative, so `HasJustElapsed` is re-raised **every frame, forever**, despite the name saying "just".

**Why it came up:** `ProductionSystem` drives station build times through the engine's `Timer` rather than a hand-rolled countdown. A one-shot build timer that reports "just elapsed" on every subsequent frame would have produced an infinite stream of free ships.

**Tried instead:** the `station.InProgress != ShipType.None` guard in `ProductionSystem` is what actually makes it safe — the system only believes `HasJustElapsed` while it is expecting a completion, and rearms the timer by hand (`Duration`/`Remaining`/`HasJustElapsed = false`) when starting the next item. Works, but it means the *caller* is carrying the "has this already fired" state that the timer's own name implies it's carrying.

**Possible approaches:** on elapse, if `!ShouldRepeat`, stop counting — e.g. clamp `Remaining` to `0` and only raise `HasJustElapsed` on the transition into that state. That makes the flag a true edge for both timer kinds without changing the repeating behaviour. Cheap, and it removes a trap that isn't visible from the call site. Alternatively rename it to something level-flavoured (`HasElapsed`) so the current behaviour stops being surprising — but the edge version seems more useful.

**Resolved 2026-08-05 — `Timer` is now a countdown, not an event source.** `HasJustElapsed` is gone, replaced by a derived `HasElapsed => Remaining <= 0`, plus `Restart(TimeSpan)` and `Stop()`. `TimerSystem` decrements and, for repeating timers, adds `Duration` back (`+=`, not `=`, so overshoot is preserved and a repeating timer doesn't drift by up to a frame per cycle).

The trap is gone at the root rather than papered over: there is no flag to raise, clear, mis-order, or double-consume, because the elapsed state is derived from the one field that already holds it. Completion semantics move to the consumer, which is where they already lived — `ProductionSystem` needs `InProgress` regardless, to know *what* finished, so storing "has this fired" a second time on the component was the thing creating the trap. `Restart` also makes rearming atomic and puts the `TimeSpan.FromSeconds(x).Ticks` conversion in one place.

**Unplanned bonus: this removed an ordering constraint.** With an edge flag, a consumer had to sit after `TimerSystem` in the frame or it would miss the single frame the flag was up. With a level, a consumer that runs before `TimerSystem` merely sees the countdown one frame late. Ordering is now a latency choice, not a correctness requirement — worth noting alongside the `RunAfter` work, since it is one fewer edge the scheduler has to be trusted to get right.

**Three residuals, none blocking:**

- **Elapsed one-shots keep being decremented forever.** `Remaining` runs unboundedly negative (no practical overflow — ~29,000 years at 60fps — but it's pointless work and `Remaining = -4,000,000,000` doesn't read as "stopped"). An early `if (timer.HasElapsed && !timer.ShouldRepeat) continue;` skips them, and clamping to `0` on the crossing would give "stopped" a single canonical representation shared with `Stop()`.
- **"Never started" and "already finished" are the same state.** There's no `IsRunning`, so `new Timer(sixSeconds, false)` sitting untouched is indistinguishable from one that has run out. Fine as-is — the consumer has to own completion state anyway — but it means `Timer` alone can never answer "did something just finish".
- **`Stop()` and natural completion are also indistinguishable**, both being `Remaining <= 0`. No impact today since nothing cancels a build, but if cancel-vs-finish ever needs different handling, that's the seam it will show up at.

Minor: the repeat branch nests `if (Remaining <= 0) { if (ShouldRepeat) ... }`, which collapses to a single `&&`.

</details>

## SystemScheduler crashes on a GameWorld with zero registered systems
**Status:** Resolved, indirectly
**Priority:** Low

<details>
<summary>Details</summary>

**What:** `SystemScheduler.Update`/`Draw` (`MonoGameLibrary/Systems/Infrastructure/SystemScheduler.cs`) index `_updateSystems[gameLayer]` / `_drawSystems[gameLayer]` directly. A `GameWorld` only gets an entry in those dictionaries the first time `SystemScheduler.Add` is called for it. If `Update`/`Draw` ever runs before any system has been registered for that world, it throws `KeyNotFoundException` instead of treating "no systems yet" as a no-op.

**Why it came up:** freshly scaffolding `Proto4X.Sandbox` — a bare `Core`-derived class with an empty `Initialize`/`LoadContent`, no systems registered yet — crashed on the very first `Update` call.

**Tried instead:** registered a trivial no-op `PlaceholderSystem` in the sandbox's `Initialize` purely to unblock local iteration. Not treating this as "solving" the underlying issue — it just avoided tripping it.

**Resolution:** `Core.Initialize` now unconditionally registers `Renderer` and `TimerSystem` before `base.Initialize()` runs, for every `Core`-derived game. Since every real game goes through `Core`, both dictionaries always have an entry before `Update`/`Draw` fire. The underlying fragility (raw indexer instead of `TryGetValue`) is technically still there, it's just unreachable through the only entry point that exists — accepted as fine since it's not a priority. The sandbox's `PlaceholderSystem` became redundant and was deleted once real systems (`MovementSystem`, `GatherSystem`) were registered for the MVP loop.

</details>

## Possibly: mutating an existing live entity's component set
**Status:** Resolved — decided against
**Priority:** Low

<details>
<summary>Details</summary>

**What:** an `Archetype`'s component-column set is fixed at construction time. There's no path to add or remove a component on an entity that already exists (which would mean moving it between archetypes).

**Why it came up:** a gather loop where an idle ship "becomes" a cargo-carrying ship reads naturally as a component being added/removed.

**Decision:** enforced as delete-and-recreate, not in-place migration. No archetype-migration mechanism is planned. A convenience method wrapping delete+recreate may be worth adding later, depending on how many call sites end up needing it and what access routes are available at that point — not committed now.

</details>
