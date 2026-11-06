# Plan 3 — The prototype loop: warehouse factories → orbit → shipyard → fleet

Depends on plan 1, because the panels need text. It doesn't depend on plan 2, which is cosmetic.

## The question this prototype answers

> Is "build a ground factory → ship components to orbit → mass-produce your own ship designs →
> fight with them" a loop that holds together, with **compressed factories** (warehouses that
> abstract to rates when you're not looking) as the scaling mechanic?

Every element below exists to make that loop testable end to end in one session. Anything that
doesn't serve the loop is cut and listed at the bottom.

## Design stance: copy proven mechanics, name the source

As requested, mechanics are lifted from games where they're known to work, rather than invented.
Each one is listed with its source and with what changed. Names, numbers and presentation are our
own. **There is one deliberate departure: automation instead of crew.** It's called out wherever it
applies.

| Mechanic | Copied from | Changed |
|---|---|---|
| Belts, inserters, drills, furnaces picking recipe from input, assemblers, poles, power satisfaction | Factorio | already built; unchanged |
| Steel smelted from plates in a furnace | Factorio | numbers |
| A building whose inside is its own floor you walk into and build on, connected to the outside | **Factorissimo 2** (Factorio mod) | connections are docks, not belt pass-throughs (see below) |
| Every building of a type shares one interior design | Factorissimo's "factory blueprint" item; also Shapez 2's blueprints | edits apply to *all* instances, like Unity prefabs |
| A compressed building running as a rate-based black box | Factorio rate calculators (Helmod/Factory Planner) for the rating, **X4: Foundations** station modules for the abstract production | the rating is *measured* by simulating the blueprint, not computed from recipes |
| Docks that request a specific item / provide whatever arrives | Factorio requester and provider chests | they exchange with the warehouse's own buffer, not with robots |
| Launch silo → orbital receiver cargo | Factorio: Space Age cargo rockets | unchanged (built earlier) |
| Solar panels as constant power in orbit | Factorio solar panel | no day/night |
| Free-form grid ship editor: parts with sizes, rotation, connectivity | **Cosmoteer** | part list trimmed to 7 |
| Thrust per direction from where thrusters sit and face; turning from torque about the centre of mass; blocked exhaust disables a thruster | **Cosmoteer** | plus a gyro part for easier designs (KSP reaction wheel) |
| Power from reactors as a ship-wide budget | Cosmoteer reactors | **automation:** no batteries carried by crew. Power is a pooled budget with the factory's satisfaction rule |
| Parts need operating, and a ship has a finite capacity to operate them | Cosmoteer crew requirement per part | **automation:** a *control* budget supplied by autonomy cores (KSP probe-core idea); over budget means every controlled part runs at capacity ÷ load |
| A shipyard building that produces a selected design on repeat | Factorio assembler "recipe" + RTS production queues (Age of Empires) | the cost is the design's part bill, consumed up front |
| Select ships, box-select, right-click to move, auto-engage the nearest hostile | the Sandbox project (Age of Empires style) | duplicated, not referenced (no project dependency) |
| Idle ships attack hostiles that come near, then return to their post | Age of Empires "defensive stance" | — |
| Timed escalating waves with an on-screen countdown | **Mindustry** wave timer | waves come from a destructible enemy base |
| Win by destroying the enemy core; lose when yours falls | Mindustry attack maps / RTS standard | — |

## Pillar 1 — Warehouses (ground)

### Rule that forces them

**Assemblers can't be built on open ground.** Data: `Assembler.AllowedOn = Interior | Orbit`.
Mining, smelting, belts, power and the launch silo stay outside. "The bulk of production" (every
multi-ingredient recipe) therefore has to sit inside a warehouse. One rule, expressed as data, using
the surface-condition mechanism that already gates the orbit-only hull recipe.

### Warehouse building

| | |
|---|---|
| Footprint | 6×6, planet only |
| Interior | 24×24 foundation floor, its own `Surface` of kind `Interior` |
| Exterior inventory | up to 4 input stacks + 4 output stacks, laid out from the blueprint's **rated I/O** |
| Power | the exterior is an ordinary consumer on the planet's pole network; the interior has no poles of its own and runs at the exterior's satisfaction |
| Ports | inserters and belts outside feed it like any assembler, but only with items the blueprint rates as inputs |

### Docks (inside)

- **Input dock** (1×1, interior only): has an item filter (the player picks it, like a requester
  chest). Between ticks it's topped up from the exterior's input stack for that item. Inserters
  take from it.
- **Output dock** (1×1, interior only): accepts anything. Between ticks its contents move into the
  exterior's matching output stacks. Anything with no stack, or no room, stays and backs up, which
  is the backpressure.

Factorissimo connects belts through wall tiles. Docks are simpler, both to build and to *rate*: a
dock's filter states its item outright, where a belt connection would have to be inferred.

### Blueprint = the interior

- Every interior **is** a blueprint: a list of `(kind, x, y, facing, recipe, config)`. There's no
  separate "save as blueprint" step. That's the "automatically a blueprint" requirement.
- A warehouse names a blueprint id. Placing a new warehouse uses the blueprint picked with `T`
  (like picking an assembler recipe). Instances of one blueprint share one design, which is how a
  proven block gets mass-produced on the ground.
- Editing any instance's interior edits the blueprint. When the player leaves, the layout is
  snapshotted into the blueprint (version++), and the blueprint is re-rated.

### Rating: auto-computed inputs and outputs

When a blueprint changes, it's **calibrated**: copied into a throwaway universe (its own scheduler,
so nothing leaks into the real game) and run under ideal conditions. Input docks are refilled every
tick, output docks are emptied every tick and counted, and power is full. There's 1,800 ticks of
warm-up (30 s, so the first crafts settle), then 3,600 measured ticks (60 s).

The result is the **rating**:

- **Inputs**: every input-dock item with consumption > 0, with the count per minute.
- **Outputs**: every item that reached an output dock, with the count per minute.
- **Draw**: mean power demand per tick.
- Reduced by GCD into a **cycle**: `consume aᵢ, produce bⱼ, every T ticks`. For example 120 plates
  + 60 copper → 30 circuits per 3,600 ticks reduces to 4 + 2 → 1 every 120 ticks.

The panel shows those numbers. This is the Factorio-calculator view, but derived from the real
layout, so mistakes (an inserter facing the wrong way, a starved assembler) show up in the rating
exactly as they would in play.

### Live vs. abstract

| State | When | What runs |
|---|---|---|
| **Live** | the player is inside (viewing or editing) | the interior surface ticks. Docks sync with the exterior buffers between ticks. The exterior draws what the interior drew last tick |
| **Abstract** | otherwise | the interior doesn't tick at all. The exterior runs the rating cycle like an assembler with a composite recipe: start when every input has a cycle's worth and every output has room, progress by power satisfaction, deliver |

- Entering and leaving are **commands** (`EnterInterior` / `ExitInterior`) in the command stream.
  Sim state depends on which warehouse is live, so it has to be replayable like any other input.
- Only one warehouse is live at a time. Entering another collapses the first.
- **Conservation:** abstract mode never touches interior items, and live mode never runs the cycle.
  A collapsed interior is *frozen*, not discarded, and resumes as it was. Items move between the
  two only through dock sync, one item at a time, so the total is exact across switches. (A
  headless check enforces this.)
- An instance whose frozen interior is out of date (the blueprint was edited through another
  instance) rebuilds it on entry. Items inside the stale interior are dropped. *Shortcut*: they
  should be refunded into the exterior buffer.

**Why measured rather than computed:** the analytic version (sum each crafter's recipe rate,
resolve the graph) is what calculator mods do. It's wrong whenever the layout is wrong, e.g. an
inserter can't keep up or a belt is under-fed, and it needs a graph solver. Running the real sim for
90 s is a few milliseconds on a 24×24 floor, it's exact by construction, and it reuses every system
unchanged.

**Scaling story:** N warehouses of one blueprint cost N × one small system iteration per tick,
instead of N × (every belt, inserter and crafter inside). That's what lets ground production scale
to the numbers a fleet needs.

## Pillar 2 — Components and logistics

New items and recipes (assembler times in ticks at 60/s):

| Item | Made in | Recipe | Time | Used for |
|---|---|---|---|---|
| Steel | furnace | 5 iron plate | 480 | everything structural |
| Frame | assembler | 2 steel + 2 gear | 240 | every part |
| Armor plate | assembler | 3 steel + 2 copper plate | 360 | armor |
| Engine | assembler | 1 steel + 2 gear + 2 circuit | 480 | thrusters, gyros |
| Power cell | assembler | 3 circuit + 4 copper plate | 360 | reactors, emitters |
| Processor | assembler | 3 circuit + 4 wire | 360 | autonomy cores |
| Emitter | assembler | 1 power cell + 2 circuit + 1 steel | 480 | lasers |

The bottleneck is steel, deliberately: it's the Factorio early-game steel crunch, and it makes
"build another steel warehouse" a real decision.

**Logistics up:** components go into a launch silo, 20 items per launch, 5 s of launching plus 3 s
in transit. That's about 2.5 items/s per silo, which is the throughput cap that makes a second
silo meaningful. Payloads land in a receiver on the orbital station, where inserters feed the
shipyard. Shipments are now addressed to the first `Orbit` surface (previously "the first surface
of another kind", which would have included interiors).

## Pillar 3 — Ship design (Cosmoteer rules, automated)

Grid up to 16×16 cells; 1 cell = 1 tile of space. The nose is local north (−y).

| Part | Size | Mass | HP | Power | Control | Cost | Effect |
|---|---|---|---|---|---|---|---|
| Hull | 1×1 | 1 | 30 | 0 | 0 | 1 frame | structure; connects parts |
| Armor | 1×1 | 3 | 140 | 0 | 0 | 1 armor plate | HP per mass |
| Autonomy core | 2×2 | 6 | 120 | −2 | **+12** | 2 frame + 2 processor | required, at least one. Supplies control |
| Reactor | 2×2 | 6 | 100 | **+16** | −1 | 2 frame + 2 power cell | supplies power |
| Thruster | 1×2 | 2 | 50 | −2 | −1 | 1 frame + 1 engine | 24 thrust in its facing direction; exhaust must be clear to the grid edge |
| Gyro | 1×1 | 2 | 40 | −2 | −1 | 1 frame + 1 engine | 30 torque each way |
| Laser | 1×2 | 2 | 50 | −4 | −2 | 1 frame + 1 emitter | 10 DPS, range 22 |

**Validity** (Cosmoteer's editor errors):

- all parts are 4-connected;
- there's at least one core;
- thrusters with a blocked exhaust are flagged and contribute nothing.

**Efficiency** (the automation rule): `power = min(1, supply/draw)` and
`control = min(1, capacity/load)`, and every active part runs at `power × control`. This is the
factory's satisfaction rule reused, so an under-powered or under-automated ship is slow and weak
rather than broken, which is Cosmoteer's soft failure.

**Derived stats** are pure functions of the design, testable headless:

- **mass**, **centre of mass**, and **moment of inertia** Σm(r² + 1/6);
- **acceleration per local direction** = Σ thrust facing that way × eff ÷ mass;
- **torque**: each thruster's 2D cross product r × F about the centre of mass, summed separately
  clockwise and anticlockwise, plus gyros. Angular acceleration = torque ÷ inertia;
- **top speed** = acceleration ÷ drag, with linear drag k = 0.6/s. Cosmoteer also caps speed
  through drag, so lighter ships with more thrust are faster;
- **HP**, **DPS** and the **cost bill**;
- **build time** = 120 + 30 × part count ticks.

**Preset designs** (slots 1–3) let the loop start without designing first: *Picket* (light laser
escort), *Lancer* (three lasers, fragile) and *Bulwark* (armoured, slow). There are 8 slots; the
player edits any slot in the designer (`B`).

## Pillar 4 — Shipyard

- 4×4, orbit only. One stack per component (6), 40 each, filled by inserters from receivers.
- Order is `design × mode`, with mode = Off / Once / **Repeat**. Repeat is the mass-production
  switch, like leaving an assembler on its recipe.
- Cycle: wait until the whole bill is present → consume it → build for the design's build time
  (scaled by power) → launch the ship into space beside the station. After Once, it goes back to
  Off.
- The panel shows the design, mode buttons, a progress bar, and the missing components with counts.

## Pillar 5 — Fleet and the threat

**Space** is a separate simulation layer: its own `GameWorld` and systems, ticked after the
surfaces, and floats like the ship frame. It's 480×320 tiles. The orbital station surface sits in
it at its frame, and the station is also a space body: 4,000 HP and two built-in lasers.

**Ship behaviour** (systems in order):

1. **Orders**: move commands carry ship serials and a point, and are applied by iterating ships
   (no by-id lookup).
2. **Targeting**: the nearest hostile within weapon range, from the previous tick's snapshot.
3. **Gunnery**: continuous beams. Damage accumulates in a per-tick damage store keyed by serial
   (the store-and-pull pattern `Exchange` already uses for cross-archetype writes).
4. **Damage**: each ship pulls its damage. Dead ships are removed in descending index order (the
   engine removal-order workaround).
5. **Helm**: autopilot. It turns toward the desired heading with bang-bang braking
   (ω_target = sign(err) × min(ω_max, √(2α|err|))). Velocity is steered toward a target with
   arrival braking: Δv goes into local axes, and each axis is limited by that axis's thrust, so side
   thrusters strafe, as in Cosmoteer. In an engagement it holds at 80% of weapon range, facing the
   target.
6. **Snapshot**: positions, HP and beam endpoints are published for rendering and picking (the
   Sandbox per-frame snapshot pattern).

**Stances:** idle ships engage any hostile within 30 tiles, then return to their post (AoE
defensive stance).

**Raiders:** a **Bastion** (8,000 HP, four lasers, range 26) sits on the far side of the map. The
wave timer is Mindustry's: the first wave comes at 6:00 (time to build), then one every 3:00.
Wave *n* is n+1 *Skiffs* (fast, light) + ⌊n/2⌋ *Brutes* (armoured). Raiders fly to the station and
attack whatever is in range on the way.

**End states:** win when the Bastion is destroyed; lose when the station is destroyed. Neither is
judged until both have been observed once (the Sandbox's settled-world latch, because of deferred
adds). `F6` toggles peaceful mode so the factory can be played without pressure.

## Controls

| Context | Keys |
|---|---|
| Planet / interior / station (build) | as before: `1`–`0`,`-`,`=` tools · `R` rotate · `T` recipe / blueprint / filter · `Q` no tool · drag to build/remove |
| Any building, no tool | left-click opens its panel (warehouse, shipyard, dock, assembler) |
| Warehouse | `E` over it, or its panel's button: enter. `E` / `Esc` inside: leave |
| Orbit | `F` toggles **fleet mode**: left-click/drag selects ships, right-click moves, `Ctrl+A` selects all |
| Anywhere | `B` ship designer · `Tab` planet ↔ orbit · `Space` pause · `F4` grade shader · `F6` peaceful |

## Starting scene

- **Planet:** iron, copper and coal fields with an open-air mining and smelting line (existing),
  steel furnaces, power, and a launch silo. Plus **two pre-built warehouses** with working
  blueprints: *Gears & circuits* and *Frames*. They teach the mechanic by example.
- **Orbit:** the station has solar panels, a receiver → inserter → shipyard line, and the shipyard
  set to *Picket, Repeat*.
- **Space:** 2 Pickets guarding the station, and the Bastion far east.

## Out of scope (named so it isn't mistaken for forgotten)

- Per-part damage and ship break-up (Cosmoteer's signature). HP is pooled.
- Turret firing arcs; projectile weapons and ammo; shields.
- Fluids; trains; robots; research and tech tree; player inventory and building costs (the game
  stays creative-mode for buildings).
- Belt pass-through ports on warehouses; nested warehouses; warehouses on the station.
- Design names via text entry; save and load.
- Fixed-point space physics: floats, like the existing frame. Lockstep would need it.

## Verification plan (headless)

- **Ratings:** a known blueprint rates to the expected cycle. Abstract output over 10 minutes is
  within 5% of live output over the same 10 minutes when given the same inputs.
- **Conservation:** live → abstract → live with no inputs flowing keeps the exact item total
  (exterior + interior).
- **Determinism:** the existing state-hash check, now with enter/exit commands and a fleet fight in
  the script.
- **Design stats:** symmetric rear thrusters → zero torque and forward acceleration = Σthrust/mass.
  A blocked thruster contributes 0. A disconnected part is invalid. Power and control deficits scale
  efficiency. The cost bill equals the sum of the parts.
- **Shipyard:** given exactly one bill, it builds exactly one ship, and its inventory ends empty.
- **Combat:** two Lancers kill one Skiff, and a lone Skiff dies to the station.
- **The whole loop:** from the starting scene, without player input, the shipyard produces at least
  one Picket within N minutes. This is the "loop closes" check.

Anything visual or interactive (panels, designer, fleet control, shaders) is build-verified only.
