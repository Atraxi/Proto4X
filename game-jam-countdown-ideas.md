# Game Jam Ideas — Theme: "Count Down"

Context: weekend jam entry, want to reuse engine/ECS work from the bigger proto4x
strategy game. Interest is strategy / factory / incremental / automation genres.
Goal: broad range of theme interpretations, from literal to abstract.

## Initial concept sweep

- **Reverse-factory / decommissioning** — start with a fully running factory or
  colony and spend the game winding it down: automate shutdown sequences,
  salvage before evacuation, decide what to sacrifice as resources tick to zero.
  Strong theme fit, reuses factory-sim logic almost directly.
- **Cascading countdown timers as core mechanic** — every machine has its own
  internal countdown instead of belts moving items; puzzle is synchronizing
  timers so outputs land at the right moment downstream.
- **Reverse 4X / "the long decline"** — start at empire peak, manage collapse
  over a fixed countdown of turns (climate collapse, resource exhaustion).
  Score = what legacy/culture you preserve, not what you conquer.
- **Launch countdown as production deadline** — build a rocket/ship under a
  literal T-minus clock, each subsystem has its own sub-countdown, all must
  hit zero in sync. Classic interpretation, still an automation puzzle.
- **Demographic/incremental decline** — idle game about a shrinking
  population; workforce ages out, you're countdown-managing replacements.
  Inverts the usual "number go up" incremental loop.
- **Countdown as currency, not clock** — actions cost "ticks" from a shared
  global countdown (not time-based); idling is free, doing things burns the
  countdown. Turns the theme into a resource constraint rather than a doom timer.

**Favorite so far:** reverse-factory/decommissioning + reverse-4X, for
strongest theme readability and easiest reuse of existing ECS work.

## Design problems identified with decommissioning idea

1. **Seeding problem** — factory/incremental genres normally get their most
   interesting layouts from player-driven building over time; hard to hand-author
   an interesting "already built" starting state for a jam.
2. **Forcing function problem** — a shutting-down factory or empire has no
   natural analog to structural collapse (unlike a bridge/building). Needs
   *something* that punishes careless/rushed shutdown, or there's no puzzle.

## Solutions to the forcing-function problem

- **Cascading dependency / backpressure** — systems have "load" fed downstream
  (power → pumps → coolant → containment). Cutting supply before downstream is
  drained causes backpressure/overload — cascading fault, not physical collapse.
  Cheap to build as reversed production-chain logic.
- **Living dependents** — workers/colonists on-site need power/food/air until
  relocated; cutting early causes casualties/riots rather than structural
  failure. Turns constraint into a moral/scoring dimension ("how clean was
  your shutdown"), pairs well with an evacuation countdown.
- **Salvage economics** — later-dismantled machines can reprocess earlier
  machines' materials (melt conveyor housings in the smelter before scrapping
  the smelter). Wrong order = lost recoverable value, not disaster. Lower
  stakes, clean scope for 48 hours.
- **Note:** power-only forcing functions (reactor complexity, etc.) felt too
  genre-limiting — wanted something generalizable beyond power/energy systems.

## Generalizing beyond power: loop-based constraints

The key insight: power "works" as a forcing function because it's a *loop*
(power → makes more power) with catastrophic failure on interruption — the
loop shape matters more than the specific resource being power.

- **Closed-loop production chains** — some chains are literal cycles (A's
  byproduct feeds B, B's feeds A: chemical reprocessing, life support loops
  like CO2 scrubber → algae vats → oxygen → crew → CO2, matter recyclers).
  Cycles have no obvious topological shutdown order — you must actively
  *break* the loop, and something must absorb the imbalance temporarily.
  Generalizes to any industry/reskin, not just power.
- **Positive vs. negative feedback as two failure textures:**
  - Positive (self-amplifying) loops fail by **runaway** if cut abruptly —
    need to bleed down gradually (like discharging a capacitor).
  - Negative (self-stabilizing/homeostatic) loops fail by **oscillation** if
    cut abruptly — overcorrection cascades until the system shakes apart;
    need to ease off slowly.
  - Mixing both across systems avoids monotony.
- **Extradimensional/pocket-dimension buffers** — a fold-space stockpile that
  keeps trickling output for a while after its source shuts off. Decouples
  "I turned it off" from "it stopped mattering" — delayed consequences /
  debts already incurred that are still counting down. Strong tie-in to the
  literal countdown theme.
- **Symbiotic pairs** — two facilities that stabilize each other (e.g.
  containment field generator + the thing it contains); neither dangerous
  alone, but desyncing their shutdown timing is. Scales across many
  industry reskins without each needing its own catastrophe justification.

**Recommended core combo:** loop-breaking + rate-sensitive feedback (positive/
negative) as the central tension, with extradimensional buffers as the hook
for "past decisions still counting down toward consequences."

## Open threads / next steps

- Still need to solve initial-state generation concretely — options discussed:
  - write a small bot/solver that plays the build-phase rules forward to
    auto-generate a starting factory (feasible if production rules are simple
    enough to encode a greedy solver for in jam time)
  - set the game in a legible real-world-template location (space station with
    named modules: reactor, hydroponics, quarters; hospital wing; embassy) so
    the starting layout doesn't need bespoke authoring
  - go abstract: procedurally generate a dependency graph rather than a
    spatial factory, sidestepping the "does this look built" problem entirely
- Not yet decided: final theme/setting skin, scope cut for 48-hour jam,
  which forcing-function mechanic(s) to actually implement vs. cut.
