# Splitting the engine into layers

Discussion notes, 2026-08-05. The idea: split `MonoGameLibrary` into a
genre-agnostic base engine plus a strategy-focused starter pack, so future
projects in other genres can start from a decent foundation instead of scratch.

Summary of the position below: **the direction is right, the architecture makes
it unusually low-risk, but extraction should be driven by evidence of reuse
rather than done upfront.** Split base-engine vs. everything-else now; let the
strategy pack crystallise out of the games only when a second strategy game
reaches for it.

## Why it's safer here than the usual "genre engine" trap

The normal failure mode of a genre layer is that it bakes in assumptions you then
spend years fighting: an inheritance chain you must extend, a framework that
calls you rather than the other way round, lifecycle hooks that assume a shape
your next game doesn't have.

ECS mostly dissolves that. A genre pack is a **menu**, not a framework — a set of
components and systems you opt into by registering them. A system you don't
register does not exist. If `MoveToTargetSystem` doesn't fit your game, you write
your own and never reference theirs, at zero cost, with no base class to escape
and no virtual to override.

That's a materially different risk profile from an OOP engine, and it's the
strongest argument in favour of doing this at all.

## But extract on evidence, not prediction

The thing that makes genre layers go wrong is generalising from N=1 — you extract
the shape you happened to write rather than the shape the genre needs, and end up
with an abstraction that fits exactly one game and fights every subsequent one.

The position is better than that sounds, because there are already two games on
the engine: `Proto4X` and `Proto4X.Sandbox` have **independently written
`MovementSystem` twice**, and the two copies have already drifted (the sandbox
version has no max-speed clamp and no rotation wrap). That's the real signal —
duplication that has *happened*, not duplication you predict.

### A test for which layer something belongs in

**Would removing it change what the game is?**

| Thing | Removing it changes the game? | Layer |
|---|---|---|
| Selection rings | No | Genre |
| Movement integration | No | Genre (arguably base) |
| Line/arc drawing primitives | No | **Base** |
| Miner state machine | Yes | Game |
| Asteroid mining economy | Yes | Game |
| Faction colours | No | Genre |

### What has earned promotion today

- **`Motion` + a tuned movement integrator.** Written twice, independently,
  already drifted. Already logged in `ENGINE_REQUESTS.md`.
- **`Primitives` (line / arc / rectangle drawing).** Interesting case: this isn't
  strategy-specific at all. Every game wants to draw a line; it only lives in the
  sandbox because the engine draws nothing but sprites. That's a **base-engine gap
  wearing game-code clothes**, and the layering exercise is what makes it visible.

### What's close

`MoveOrder` + `MoveToTargetSystem`, `Selectable` + selection. These get their
second data point the moment `Proto4X` takes the RTS-style controls that were
always intended to be ported across by hand.

### What to leave alone for now

`Health` / `Faction` / `Weapon`. Genre-level in principle, but heavily shaped by
one specific combat model (continuous beam, nearest-target, no projectiles).
One data point is not enough to know which parts are the genre and which are
this game.

## Library vs. template — two different products

There are two ways to ship "a strategy starter", with very different economics:

| | Library layer | Starter template |
|---|---|---|
| Reuse by | Dependency | Fork |
| Getting it wrong | Expensive — constrains every game forever | Cheap — each game diverges freely |
| Fixes propagate | Yes | No |
| Good for | Invariants | Flavoured decisions |

The invariant material — storage, scheduling, rendering, input, primitives —
clearly wants to be a library. The flavoured material — what selection *means*,
whether units have facing, instant vs. projectile combat, whether resources are
finite — is exactly what you want to be able to change per game, and a template
serves that better than a library you'd otherwise keep bolting configuration
knobs onto.

**"Library for the invariant, fork for the flavoured"** is probably a better
split than three tiers of library.

## What the split would force you to fix first

`Core.Initialize` hardcodes registering `Renderer` and `TimerSystem`, and a
default UI system is planned. The moment a strategy pack wants to contribute its
own default systems, that list has to become extensible or every layer ends up
forking `Core`.

That's the first concrete design question the layering raises — and it's worth
solving regardless of whether a genre pack ever ships.

## Two smaller notes

**Ordering composes cleanly with layers.** `RunAfter(SystemRef.Of<T>())` requires
naming a type: a pack can name base-engine types, a game can name both, and the
base engine never needs to name anything above it. The constraint graph flows the
same direction as the project references, which is a nice property to get for
free. The one thing it can't express is ordering between two *sibling* genre
packs — fine as long as no game depends on two of them at once.

**The component budget is shared.** `ComponentTypeMask` is 128 bits across
everything registered, so a genre pack spends from the same allowance the game
does. Not close to a problem yet — worth knowing before a pack defines forty
components.

## Suggested sequencing

1. **Now: split base-engine vs. everything-else.** Two projects. Not for reuse —
   for honesty. An assembly boundary turns "is this engine or game?" into a
   question with a real answer instead of a folder convention. That boundary has
   already earned its keep twice, surfacing the `internal`
   `IDependsOnOtherComponents` problem and the `ComponentContainerAttached` leak.
2. **Alongside: make `Core`'s default system registration extensible**, since the
   split immediately needs it.
3. **Later: let the strategy pack crystallise** out of the games, promoting only
   what has been written twice and agrees on structure. Three projects on one
   data point is speculative; two projects is just bookkeeping about what you
   already know is reusable.
