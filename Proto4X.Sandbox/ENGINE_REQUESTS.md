# Engine Requests

Requests filed from `Proto4X.Sandbox` while building a minimal playable loop against `MonoGameLibrary`. Each entry: what's missing, why it came up, what was tried instead, and possible approaches. This is a log, not a task queue — pull items into conversation or a GitHub issue when you want to act on one.

<!-- Template:

## <short title>
**Status:** open | discussed | resolved
**What:** what the engine can't currently do.
**Why it came up:** the specific thing in the sandbox that needed it.
**Tried instead:** any workaround attempted or considered and rejected, and why.
**Possible approaches:** rough shape of a fix, if any come to mind. Not a spec.

-->

## SystemScheduler crashes on a GameWorld with zero registered systems
**Status:** resolved, indirectly
**What:** `SystemScheduler.Update`/`Draw` (`MonoGameLibrary/Systems/Infrastructure/SystemScheduler.cs`) index `_updateSystems[gameLayer]` / `_drawSystems[gameLayer]` directly. A `GameWorld` only gets an entry in those dictionaries the first time `SystemScheduler.Add` is called for it. If `Update`/`Draw` ever runs before any system has been registered for that world, it throws `KeyNotFoundException` instead of treating "no systems yet" as a no-op.
**Why it came up:** freshly scaffolding `Proto4X.Sandbox` — a bare `Core`-derived class with an empty `Initialize`/`LoadContent`, no systems registered yet — crashed on the very first `Update` call.
**Tried instead:** registered a trivial no-op `PlaceholderSystem` in the sandbox's `Initialize` purely to unblock local iteration. Not treating this as "solving" the underlying issue — it just avoided tripping it.
**Resolution:** `Core.Initialize` now unconditionally registers `Renderer` and `TimerSystem` before `base.Initialize()` runs, for every `Core`-derived game. Since every real game goes through `Core`, both dictionaries always have an entry before `Update`/`Draw` fire. The underlying fragility (raw indexer instead of `TryGetValue`) is technically still there, it's just unreachable through the only entry point that exists — accepted as fine since it's not a priority. Side effect: the sandbox's `PlaceholderSystem` is now redundant; left in place, harmless.

## No way to spawn or destroy an entity from inside a running system
**Status:** partially resolved
**What:** `Archetype` already had working deferred-queue machinery (`QueueEntityForAddition`, `QueueEntityForRemovalById`/`ByIndex`, `ProcessDeferredUpdates`, which `SystemScheduler.Update` calls after every system runs), and `GameWorld.RemoveEntity(entityId)` reached that queue — but only from outside a system's `Update`. A running `IUpdateSystem.Update` was only ever handed `List<ArchetypeChunk>`, with no reference to `GameWorld` or the owning `Archetype`.
**Why it came up:** any dynamic MVP mechanic needs this — a gather system consuming a resource node, a projectile system spawning a shot, a health system killing an entity at 0 HP.
**Resolution — removal:** `ArchetypeChunk` now carries an `IArchetype` and exposes `QueueEntityForRemovalById`/`QueueEntityForRemovalByIndex` as a minimal, read/write-free handle. A system can now queue removal of an entity it's currently iterating. Along the way, a real batching bug got fixed: `Archetype.ProcessDeferredUpdates` processed `DeferredEntityRemovalsByIndex` in queue order rather than descending index order, which corrupted `EntityIdToDenseIndex` for an unrelated live entity whenever two-or-more removals landed on the same archetype in one frame. The fix (`DeferredEntityRemovalsByIndex.Sort((A, B) => B - A)`) is now active.
**Resolution — addition:** deliberately *not* exposed on `ArchetypeChunk`/`IArchetype`. The rejected shape would have forwarded blindly to the chunk's own archetype regardless of the new entity's actual composition — fine for "spawn another one just like me," broken (wrong archetype, missing/garbage columns, or a `KeyNotFoundException`) the moment a system wants to spawn something differently shaped, e.g. a gather system consuming a node and spawning a cargo pickup. Addition stays `GameWorld`-only (mask-based lookup-or-create) and unreachable from inside a system for now — punted rather than shipped unsafe.
**Sanity-check findings on `Archetype` accessibility (requested separately, logged here since it's the same surface):**
- `RemoveEntityWithId`/`RemoveEntityAtIndex` are `internal`, not `private` — the same immediate, unqueued structural mutation that `AddEntity` just got locked down to prevent (it's now `private`) is still reachable by any other code in the `MonoGameLibrary` assembly. `RemoveEntityWithId` isn't called from anywhere today (dead). Recommend tightening both to `private` for consistency with `AddEntity`.
- `GrowEntityCapacity()` is `protected` on a class with no subclasses — vestigial, not a safety issue, just worth `private` in a cleanup pass.
- `WriteComponentById<T>`/`WriteComponentByIndex<T>` (both `internal`) are unqueued by design and that's fine — they only overwrite an existing slot's value, never touch `count`/`DenseIndexToEntityId`/`EntityIdToDenseIndex`, so there's no structural state to corrupt regardless of call timing. This is the path `UserInterfaceManager` already uses for WASD input via `ComponentContainerAttached<T>.WriteComponentById` — legitimate, not a hole.
- `GameWorld.AddEntity`'s two branches have different immediacy: a brand-new archetype composition is constructed and added synchronously, an existing one is deferred via the queue. **Discussed and accepted as safe today** — a freshly created archetype can't have any in-flight `ArchetypeChunk` referencing it, and both current call sites (`LoadContent`, `UserInterfaceManager`'s right-click spawn) run outside `SystemScheduler.Update`'s systems loop, so nothing is enumerating `Archetypes` at the moment it mutates. **Caveat:** this holds only as long as systems can't reach `GameWorld`. If that boundary ever changes, the two branches would start differing in same-frame visibility too (new-composition spawns become visible to later systems in the same frame; existing-composition spawns don't become visible until next frame) — worth a second look at that point, not now.

## No text rendering
**Status:** open, anticipated (identified by reading the code, not yet hit through sandbox gameplay)
**What:** no `SpriteFont` loading or `SpriteBatch.DrawString` usage anywhere in `MonoGameLibrary` or either game project.
**Why it came up:** the planned MVP loop ends in a stockpile count rendered as text — there's currently no way to render any text at all, HUD or otherwise.
**Tried instead:** nothing yet. Likely pending the `UserInterfaceManager` engine/game ownership split (see below) rather than a quick bolt-on — not clear yet where a HUD text concern should actually live.
**Possible approaches:** likely just a `SpriteFont` content item plus a thin `IDrawSystem` (or a thing `UserInterfaceManager` can do directly) that draws a string at a screen-space position, unaffected by the world camera transform used for the rest of `SystemScheduler.Draw`.

## Possibly: mutating an existing live entity's component set
**Status:** resolved — decided against
**What:** an `Archetype`'s component-column set is fixed at construction time. There's no path to add or remove a component on an entity that already exists (which would mean moving it between archetypes).
**Why it came up:** a gather loop where an idle ship "becomes" a cargo-carrying ship reads naturally as a component being added/removed.
**Decision:** enforced as delete-and-recreate, not in-place migration. No archetype-migration mechanism is planned. A convenience method wrapping delete+recreate may be worth adding later, depending on how many call sites end up needing it and what access routes are available at that point — not committed now.

## EntityView — entity lifecycle handle
**Status:** design discussed, not yet built
**What:** a proposed lightweight per-entity handle utility, in response to "what's the path to interact with a specific entity" now that `GameWorld` access, queueing, and removal are split across a few different surfaces (`GameWorld`, `IArchetype`, `ArchetypeChunk`).
**Intended shape and constraints:**
- Carries only: the entity id, a `GameWorld` reference, and possibly an `IArchetype` handle. **Explicitly does not carry component data or values.**
- Purpose is lifecycle interaction (remove, requeue, existence checks) — not a data-access convenience.
- Deliberately designed to be awkward to build an OOP-style architecture on top of — actively resisting becoming a `GameObject`-style handle that invites storing long-lived references and treating entities as objects, which would erode the archetype/SoA model this engine is built around.
- Stale-id handling is an acknowledged open question, raised in review and agreed as valid: an `EntityView` can outlive the entity it names (removed elsewhere, handle held on). Not yet decided whether `Get`/lifecycle calls on a dead handle throw, return a bool/nullable, or something else — needs a deliberate answer before this ships, not an accidental one.
- Because ids don't map to a stable dense index across frames (swap-remove), any future data-returning method on a handle-style object would necessarily return copies, not `ref`s — different shape than `ArchetypeChunk.Get<T>(index)`/`GetSpan<T>()`, which is part of why data access is being kept off `EntityView` entirely rather than added later.
- Should stay aware that `GameWorld` access is currently a global static (`Core.Instance.World`, already the existing pattern via `UserInterfaceManager`'s right-click spawn) as an intentional MVP placeholder, not a permanent design — the expected evolution is toward "the currently selected/user-active `GameWorld`," with other worlds tracked separately through a process not yet designed. `EntityView` holding a `GameWorld` reference directly (rather than resolving one implicitly) is likely the right call precisely because it doesn't bake in a single-world assumption any harder than necessary.

**Related open question, not decided:** whether to remove single-entity/random-access reads from the engine entirely — specifically `ArchetypeChunk.Get<T>(index)` — to push the architecture harder toward "systems + iteration" as the only access pattern, with `EntityView` handling lifecycle-only concerns instead. Acknowledged tension: UI selection/interaction (the WASD-selected-entity flow in `UserInterfaceManager`, `UserInterfaceManager.cs:57-110`) currently depends on exactly this kind of single-entity random access to read/write the selected entity's `Position`/`Motion`. Unclear yet how restrictive a pure iteration-only model would be for cases like this. Left open.
