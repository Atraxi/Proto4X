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

</details>

## No text rendering
**Status:** Open — worked around
**Priority:** High

<details>
<summary>Details</summary>

**What:** no `SpriteFont` loading or `SpriteBatch.DrawString` usage anywhere in `MonoGameLibrary` or either game project.

**Why it came up:** the MVP loop ends in a stockpile count that needs to be shown somewhere — there's currently no way to render any text at all, HUD or otherwise.

**Tried instead:** sidestepped for the MVP by writing the stockpile count to `Window.Title` each frame (`Proto4X.Sandbox/Sandbox.cs`) instead of drawing it in-world. Fine for a single always-visible counter, not a real HUD solution — doesn't generalize to positioned/multi-value/styled text, and will need solving properly as soon as more than one value needs showing at once.

**Possible approaches:** likely just a `SpriteFont` content item plus a thin `IDrawSystem` (or a thing `UserInterfaceManager` can do directly) that draws a string at a screen-space position, unaffected by the world camera transform used for the rest of `SystemScheduler.Draw`.

</details>

## No way to spawn or destroy an entity from inside a running system
**Status:** Open — partial
**Priority:** Medium

<details>
<summary>Details</summary>

**What:** `Archetype` already had working deferred-queue machinery (`QueueEntityForAddition`, `QueueEntityForRemovalById`/`ByIndex`, `ProcessDeferredUpdates`, which `SystemScheduler.Update` calls after every system runs), and `GameWorld.RemoveEntity(entityId)` reached that queue — but only from outside a system's `Update`. A running `IUpdateSystem.Update` was only ever handed `List<ArchetypeChunk>`, with no reference to `GameWorld` or the owning `Archetype`.

**Why it came up:** any dynamic mechanic beyond "consume an existing entity" needs this — a projectile system spawning a shot, a resource node splitting, a health system killing an entity at 0 HP already works (removal), but nothing can spawn from inside a system yet.

**Resolution — removal:** `ArchetypeChunk` now carries an `IArchetype` and exposes `QueueEntityForRemovalById`/`QueueEntityForRemovalByIndex` as a minimal, read/write-free handle. A system can now queue removal of an entity it's currently iterating. Along the way, a real batching bug got fixed: `Archetype.ProcessDeferredUpdates` processed `DeferredEntityRemovalsByIndex` in queue order rather than descending index order, which corrupted `EntityIdToDenseIndex` for an unrelated live entity whenever two-or-more removals landed on the same archetype in one frame. The fix (`DeferredEntityRemovalsByIndex.Sort((A, B) => B - A)`) is now active.

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

## No way to get an entity's id from an archetype chunk index
**Status:** Open — worked around
**Priority:** Medium

<details>
<summary>Details</summary>

**What:** `ArchetypeChunk` exposes `Get<T>(index)`/`GetSpan<T>()` for component data and `QueueEntityForRemovalByIndex`/`ById` for removal, but nothing that maps a chunk index back to the entity id that owns it. `Archetype.DenseIndexToEntityId` holds exactly that mapping but is private, and `ArchetypeChunk` only holds an `IArchetype` handle narrow enough for removal, not lookup. So a system iterating a chunk can read/write a matched entity's components in place, or remove it, but can't learn its id.

**Why it came up:** building `SelectionSystem` (`Proto4X.Sandbox/Systems/SelectionSystem.cs`) for the new click-to-select-and-order movement model — picking "whichever ship is nearest the click" from an archetype query would need each candidate's id (to remember as `SelectedEntityId`, and later pass to `GameWorld.MutateComponent<MoveOrder>`). With no id-from-index path, this doesn't generalize.

**Tried instead:** `SelectionSystem` takes a fixed `IReadOnlyList<int> selectableEntityIds` constructor argument instead of querying an archetype at all, and checks each known id directly via `GameWorld.TryGetComponent<Position>`. Works today because the sandbox has exactly one ship (`PlayerEntityId = 0`, same hardcoded-id convention `GatherSystem` already uses) but doesn't scale to "select whichever ships are on screen" once there's more than one — the moment real multi-unit selection is wanted, this will need solving for real.

**Possible approaches:** expose an `ArchetypeChunk.GetEntityId(index)` (or a paired `Span<int>`/`ReadOnlySpan<int>` of ids alongside the component spans) backed by the same `DenseIndexToEntityId` array `Archetype` already maintains — read-only, no new structural-mutation surface, same shape/cost character as the existing `Get<T>`/`GetSpan<T>` accessors.

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
