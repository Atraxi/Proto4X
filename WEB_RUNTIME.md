# A browser runtime for Proto4X

Exploration notes, 2026-08-28. Question: what would it take to run `Proto4X.Sandbox`
— and, ideally, anything else built on `MonoGameLibrary` — in a browser, as a purely
additive side project?

**Nothing in this document has been built.** This file is the only thing added to the
repo; it changes no build, no project, and no code. Delete it and the repo is exactly
as it was.

## The short version

Build a **`browser-wasm` compile target with a hand-written XNA-shaped compatibility
shim**, living entirely in a new `Web/` folder, source-linking the existing engine and
game `.cs` files without editing them. Zero new third-party dependencies. The one
prerequisite is a Microsoft-published SDK workload.

This is viable because the engine's actual MonoGame surface is *tiny* — 9 call sites,
no shaders, no fonts, no audio, no threads — which is a much stronger position than
"MonoGame game wants to run on the web" usually implies.

Gate it on a **Phase 0 proof** with a stated kill criterion, so the downside is a
folder you delete rather than a commitment.

The credible alternative, **KNI**, passes its security audit and is a legitimate
choice. The reason to prefer the shim is drift ownership, not quality. There is a
clean decision rule for when KNI becomes the better call — see §11.

---

## 1. Why this is unusually cheap here

The normal reason a MonoGame game can't go to the browser is custom `.fx` shaders
(HLSL that has to be cross-compiled to GLSL), followed by fonts, audio, and threading.
This codebase has none of them.

| Blocker | Present? | Evidence |
|---|---|---|
| Custom shaders (`.fx`) | **No** | No `.fx` anywhere in the repo |
| `SpriteFont` / `DrawString` | **No** | Zero hits across all three projects — the UI is deliberately text-free |
| Audio | **No** | No `.wav` / `.ogg` / `.mp3`, no `SoundEffect` / `Song` |
| Threads / `Task` / `Parallel` | **No** | None in engine or game — matters, because browser WASM is single-threaded by default |
| File I/O, `Stopwatch`, `Environment` | **No** | None in engine or game |
| Reflection (`Activator`, `MakeGenericType`) | **No** | Only `typeof(T)` used as dictionary keys — AOT-safe |

And the platform surface that *is* used is remarkably small. Across the entire
solution there are **9 call sites** touching `GraphicsDevice` / `Window` / `Content` /
`Graphics`:

```
MonoGameLibrary/Core.cs:34     Graphics.ApplyChanges
MonoGameLibrary/Core.cs:36     Window.Title
MonoGameLibrary/Core.cs:37     Content.RootDirectory
MonoGameLibrary/Core.cs:59     GraphicsDevice.Viewport
Proto4X.Sandbox/Sandbox.cs:154 Window.Title
Proto4X.Sandbox/Sandbox.cs:159 GraphicsDevice.Clear
Proto4X/Proto4X.cs:28,30       Content.Load
Proto4X/Proto4X.cs:49          GraphicsDevice.Clear
```

Structurally, **18 of the 35 engine source files reference no MonoGame type at all** —
the whole archetype/ECS core (`Archetype`, `ArchetypeChunk`, `ComponentColumn`,
`ComponentTypeMask`, `ComponentTypeRegistry`, `EntityIdRegistry`, `SystemRef`,
`InputManager`) is already platform-free. The MonoGame coupling is concentrated in
rendering, input state, and the `Core : Game` shell — exactly the layer a browser
target is supposed to replace.

This is, in other words, close to the best-case starting position.

## 2. The additive mechanism

This is the part that keeps the work out of your way, so it's worth stating precisely.

```
Proto4X/
  MonoGameLibrary/          <- untouched
  Proto4X.Sandbox/          <- untouched
  Proto4X.sln               <- untouched (Web/ deliberately NOT added to it)
  Web/                      <- everything new lives here
    Xna.Shim/               <- Microsoft.Xna.Framework.* implemented over WebGL2
      Xna.Shim.csproj
    Sandbox.Web/            <- the browser build of the sandbox
      Sandbox.Web.csproj
      main.js               <- WebGL + event glue (~350 lines, vendored, no CDN)
      index.html
      wwwroot/Content/      <- the source PNGs, copied
    Xna.Shim.Conformance/   <- golden-value tests (see 5.3)
    nuget.config            <- scoped feed for the web tree only
```

`Sandbox.Web.csproj` never references the sandbox project. It **source-links** it:

```xml
<Compile Include="..\..\MonoGameLibrary\**\*.cs"
         Exclude="..\..\MonoGameLibrary\obj\**;..\..\MonoGameLibrary\bin\**" />
<Compile Include="..\..\Proto4X.Sandbox\**\*.cs"
         Exclude="..\..\Proto4X.Sandbox\obj\**;..\..\Proto4X.Sandbox\bin\**;..\..\Proto4X.Sandbox\Program.cs" />
<ProjectReference Include="..\Xna.Shim\Xna.Shim.csproj" />
```

Because the shim declares the same `Microsoft.Xna.Framework.*` namespaces and the
MonoGame NuGet package is simply absent from this project, every `using
Microsoft.Xna.Framework;` in your existing code resolves to the shim instead. Not one
line of engine or game source changes.

Three carve-outs this needs:

- **`Program.cs` is excluded.** It's `using var game = new Sandbox(); game.Run();`. In
  a browser, `Run()` cannot block — it schedules a `requestAnimationFrame` callback and
  returns — so `Main` would exit immediately and `using` would `Dispose()` the live
  game. The web target supplies its own entry point that parks the instance in a static
  field and never disposes it.
- **`Content/` is copied, not built.** See §6.
- **`Analyzers`** can be referenced or dropped; it's a source analyzer with no runtime
  effect either way.

**Compiler switches must be set explicitly, because source-linking carries `.cs` files,
not csproj properties.** `MonoGameLibrary.csproj` sets
`<CheckForOverflowUnderflow>True</CheckForOverflowUnderflow>` in *both* configurations
and `<Nullable>enable</Nullable>`; `Proto4X.Sandbox.csproj` sets neither. The web
project compiles both file sets into **one assembly under one set of switches** — so by
default the engine code would get *unchecked* arithmetic despite being written and
debugged with checking on. Set both properties explicitly in `Sandbox.Web.csproj` to
match the engine, or the web build has quietly different semantics from the same source.

Related, and worth knowing rather than fixing: merging the two projects into one
assembly **dissolves the `internal` boundary** between engine and game — the boundary
`ENGINE_LAYERING.md` credits with surfacing the `IDependsOnOtherComponents` and
`ComponentContainerAttached` problems. The web build is therefore a strictly *weaker*
check than the desktop build, and never a substitute for it.

Consequences worth being explicit about: your daily `dotnet build` / F5 loop is
completely unaffected, `Proto4X.sln` never restores the web tree, and a web build that
breaks can never break the desktop build.

## 3. The options considered

| Option | Verdict |
|---|---|
| **A. KNI** (`nkast` MonoGame fork, WebGL backend) | Viable. Fastest to first pixel. Extends trust to a ~13-package single-maintainer dependency graph. Audited below. |
| **B. Microsoft-only `browser-wasm` + hand-written shim** | **Recommended.** No new third-party trust. More upfront work, all of it mine. |
| **C. Official MonoGame web target** | **Does not exist and is not coming.** MonoGame issue #8102 (WebAssembly support) is closed as *not planned*; 3.8.5 (July 2026) added Vulkan and DX12 preview platforms, not web. |
| **D. NativeAOT-LLVM → WASM** | Experimental (`dotnet/runtimelab`), never shipped as a supported path. Would not remove any of the shim work — you'd still need rendering and input backends. No advantage over B. |
| **E. Port the game to TypeScript** | Rejects the premise. The engine is the point of the project; a port is a second codebase to keep in sync forever. |
| **F. Cloud-stream the desktop build** | Not a browser runtime, and it costs money per player. Out of scope. |

The real choice is A vs. B.

One option I want to explicitly **reject**: introducing `IRenderer` / `IInputSource`
abstractions into `MonoGameLibrary` so the browser backend can plug in cleanly. It's
the architecturally tidy answer, and it's wrong here twice over — it violates the
additive-only constraint, and `ENGINE_LAYERING.md` already sets the house position of
extracting on evidence rather than prediction. A speculative web port is not evidence.

---

## 4. Security audit

You asked for this before committing to any dependency, so it leads rather than
trails. Facts below were verified against nuget.org, the GitHub API, and MonoGame's
issue tracker on 2026-08-28; anything I could not verify is labelled.

### 4.1 Threat model

Two genuinely different surfaces, and it matters not to blur them:

- **Runtime (in the browser).** Low risk for *both* options. WASM executes in the
  browser sandbox: no filesystem, no arbitrary network, same-origin policy enforced.
  A malicious game framework in the browser is roughly as dangerous as a malicious web
  page — which is to say, not nothing, but heavily contained, and it isn't running on
  your machine.
- **Build time (on your dev machine).** This is where the actual risk lives. NuGet
  packages can ship MSBuild `.targets` and task assemblies that execute **as you, with
  your privileges, on restore and build**. This is the standard .NET supply-chain
  surface, and it's the thing worth thinking about.

So the question is not "is WebGL safe" but "whose build-time code am I willing to run".

### 4.2 Option A — KNI

| Item | Finding | Confidence |
|---|---|---|
| License | MS-PL (permissive, no copyleft, compatible with your MIT-style LICENSE) | Verified |
| Current package | `nkast.Kni.Platform.Blazor.GL` **4.3.9001**, published **2026-08-22** | Verified |
| Target frameworks | net8.0 **and net10.0** — matches your `net10.0` projects directly | Verified |
| Deprecated / known vulns | No, and no advisories listed | Verified |
| Ownership | `nkast` (Kastellanos Nikos), verified owner prefix on nuget.org | Verified |
| Adoption | ~19.4K downloads for the web backend, ~86.9K for the core package. Used by SadConsole, FlatRedBall, Apos.Gui | Verified |
| Repo activity | Most recent commit 2026-08-23. Actively developed | Verified |
| Dependency fan-out | ~13 packages pulled in — 6 × `nkast.Wasm.*` + 7 × `nkast.Xna.Framework.*` — **all authored by the same person** | Verified |
| Maintainer concentration | Recent commit history is overwhelmingly `nkast`, with one occasional contributor. The 100+ name contributor list on the repo is **inherited MonoGame history** (KNI is a fork of MonoGame's full ~15,600-commit tree), not KNI reviewers | Verified |
| Build-time code execution | KNI ships content-pipeline tooling (an `mgcb` fork, `KNIFXC` effect compiler) that runs during build | **Unverified** for the specific packages we'd use — confirm before adopting |

**Provenance of "Verified" above.** These rows come from the nuget.org listings and the
GitHub API as read on 2026-08-28, summarised rather than inspected byte-for-byte — so
read "Verified" as *checked against the published listing on that date*, not *audited*.
One retrieved detail was visibly off (the core `nkast.Xna.Framework` package was
reported as also targeting `net40`), which is exactly why the distinction is worth
keeping honest. Re-check the version, TFM and deprecation rows at adoption time; none
of them change the recommendation, because Option B is robust to KNI being *better*
than described here.

**Findings.**

1. **Effective bus factor is 1, and effective review factor is also ~1.** This is the
   central finding. It is not an accusation — nkast has maintained this for years and
   ships regularly — but it means the fork-specific code, which is precisely the code
   that would run your game, has essentially no second pair of eyes. The impressive
   contributor count is MonoGame's, not KNI's.
2. **Package identity has already churned once.** `nkast.Xna.Framework.Blazor` is now
   marked *deprecated — legacy and no longer maintained*, superseded by
   `nkast.Kni.Platform.Blazor.GL`, with the version line jumping 3.14 → 4.x. Anyone who
   adopted the old name had to migrate. That's a fair predictor of future churn on a
   low-priority side project you aren't watching.
3. **It is not auditable in any honest sense.** It's a fork of a ~200k-line framework.
   Neither of us is going to review it. Adopting KNI is a *trust* decision, and should
   be described as one rather than dressed up as a reviewed one.
4. **Small ecosystem cuts both ways.** ~87K lifetime downloads means fewer eyes on a
   compromise — but also makes it an unattractive supply-chain target compared with
   packages that have millions of installs.

**Verdict: acceptable, not disqualifying.** Permissive license, active maintenance,
real production users, no known vulnerabilities. If you adopt it, apply the hardening
in §4.4. The reservation is concentration and churn, not integrity.

### 4.3 Option B — Microsoft-only

| Item | Finding | Confidence |
|---|---|---|
| New third-party trust | **Zero.** The `wasm-tools` workload is published by Microsoft through the same channel as the SDK already compiling your game — no *new* trust boundary is created | Verified |
| APIs used | `System.Runtime.InteropServices.JavaScript` (`[JSImport]` / `[JSExport]`) — **supported** since .NET 8 | Verified |
| Experimental caveat | The `wasmbrowser` *project template* ships in the separate `wasm-experimental` workload, and Microsoft labels **the templates** experimental. The *runtime APIs* are not. We hand-write the `.csproj` and never touch the template, so the experimental label doesn't attach to this path | Verified |
| JS glue | ~350 lines that I write, you can read, and which lives in your repo. No CDN, no npm, no bundler | By design |
| Runtime risk | Standard browser sandbox | — |

The honest cost of Option B is not security, it's **labour** — the shim is roughly
2,000–2,500 lines that don't exist yet.

### 4.4 Hardening recommendations (apply to whichever you pick)

1. **Lockfile + locked restore.** `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>`
   on the web projects, restore with `--locked-mode`. A silent transitive version bump
   then fails the build instead of executing.
2. **Pin exact versions in the web tree.** No floating `3.8.*` style ranges like the
   desktop projects use. `4.3.9001`, not `4.*`.
3. **Scoped `nuget.config` under `Web/`** listing only nuget.org, with `<clear />`
   first, so no machine-level fallback folder or private feed leaks in.
4. **Keep `Web/` out of `Proto4X.sln`.** Opening or building the solution then never
   restores or executes anything from the web tree. This is the single highest-value
   control: the risky dependency only runs when you deliberately build the web target.
5. **No CDN-loaded JavaScript, ever.** Everything vendored and reviewed in-repo. A
   `<script src="https://...">` in a game shell is a live remote-code-execution channel
   into whatever you ship.
6. **If you ever host it:** static files only, no server component. Only add COOP/COEP
   headers if we ever want WASM threads — we don't, the engine is single-threaded.

### 4.5 Recommendation

**Take Option B.** Not because KNI failed the audit — it passed — but because for a
low-priority side curiosity you've explicitly said you don't want to babysit, the
option that adds *no new trust relationship and no external release cadence to track*
is worth the extra implementation work, especially when that work falls to me and the
codebase makes it unusually small.

---

## 5. Architecture of the recommended build

```
+------------------------------------------------------+
|  Browser tab                                         |
|                                                      |
|  +------------+   events    +--------------------+   |
|  |  main.js   |------------>|  Xna.Shim          |   |
|  |            |             |  Keyboard/Mouse    |   |
|  |  WebGL2    |<------------|  SpriteBatch       |   |
|  |  canvas    |  vertex buf |  Texture2D         |   |
|  |            |             |  Game loop (rAF)   |   |
|  +------------+             +---------+----------+   |
|                                       |              |
|                             +---------v----------+   |
|                             |  MonoGameLibrary   |   |
|                             |  Core, ECS,        |   |   unchanged
|                             |  SystemScheduler   |   |   source
|                             +--------------------+   |
|                             |  Proto4X.Sandbox   |   |
|                             +--------------------+   |
+------------------------------------------------------+
```

### 5.1 The shim surface — full inventory

Derived from what the code actually touches, not from XNA's full API:

| Area | Members needed |
|---|---|
| Math | `Vector2`, `Vector3`, `Matrix`, `Color`, `Rectangle`, `Point`, `MathHelper` |
| Graphics | `SpriteBatch` (`Begin`/`End`/`Draw` ×4 overloads), `Texture2D` (ctor, `SetData`, `GetData`, `Width`, `Height`), `GraphicsDevice` (`Clear`, `Viewport`), `GraphicsDeviceManager`, `Viewport`, `SpriteEffects` |
| Framework | `Game` (`Run`, `Initialize`, `LoadContent`, `Update`, `Draw`, `Exit`, `IsMouseVisible`, `Window.Title`, `Content`, `GraphicsDevice`), `GameTime`, `GameWindow` |
| Input | `Keyboard` / `KeyboardState` / `Keys`, `Mouse` / `MouseState` / `ButtonState` |
| Content | `ContentManager.Load<Texture2D>`, `RootDirectory` |

Roughly 2,000–2,500 lines, most of it mechanical. `Game`'s members must be `virtual`
with XNA's exact signatures, because `Core` declares `protected sealed override` on
`Initialize` / `Update` / `Draw`, and `Sandbox` overrides `LoadContent`. Note also that
`Core`'s *constructor* touches `Window.Title`, `Content.RootDirectory`,
`IsMouseVisible`, and `new GraphicsDeviceManager(this)` with four settable properties
plus `ApplyChanges()` — all before `Run()` is ever called.

### 5.2 Rendering: SpriteBatch → WebGL2

The performance-critical design decision is **interop granularity**. A JS call per
`spriteBatch.Draw` would be catastrophic — there are ~700 asteroids on screen before
any ships exist. The design:

- **Build one texture atlas at load.** Every texture in the game is ≤ 64×64 (3 ships,
  1 station, 3 rocks, ×3 faction tints, plus the 1×1 white pixel ≈ 13 textures). A
  single 512×512 atlas holds all of them with room to spare.
- **`SpriteBatch.Draw` appends a quad** to a pre-allocated float buffer in WASM linear
  memory. No interop at all.
- **One interop call per `Begin`/`End` pair**, handing JS an offset and length into
  WASM memory, which JS reads as a typed-array view directly — zero copy.
- Result: the whole world pass becomes **one or two draw calls per frame**.

`Primitives.DrawLine` and friends stretch the 1×1 white pixel, which becomes a white
region in the atlas — so lines, bars, rings, health bars and the minimap all batch
together with everything else for free.

### 5.3 The `Matrix` trap — highest silent-wrongness risk

`Camera.Update` builds its transform by repeated `cameraMatrix *= Matrix.Create...`,
and that same matrix is then consumed by `spriteBatch.Begin(transformMatrix:)`, by
`Vector2Extensions.Transform`, and by `GetVisibleScreenBoundsAABB`. Selection and
click-to-world orders all depend on it.

XNA uses a **row-vector convention** with a specific multiplication order, and
`SpriteBatch` applies `transformMatrix` in a specific way. Get either subtly wrong and
the game *renders perfectly* while camera pan, zoom-to-cursor and click-to-world are
quietly off by a transform. That is the kind of bug that costs days.

**Mitigation, and it's cheap:** a small conformance test project on the desktop that
references *both* real MonoGame *and* the shim, and asserts numeric equality across a
corpus of matrix and vector operations. It kills the entire risk class in an afternoon,
and keeps killing it as the shim grows.

That project needs one piece of plumbing to compile at all, since both assemblies
define `Microsoft.Xna.Framework.Matrix`, `Vector2` and `Rectangle` — every unqualified
type reference would otherwise be ambiguous. Use an **extern alias**:

```xml
<PackageReference Include="MonoGame.Framework.DesktopGL" Version="3.8.*">
  <Aliases>MgReal</Aliases>
</PackageReference>
```

```csharp
extern alias MgReal;
// then compare MgReal::Microsoft.Xna.Framework.Matrix against the shim's Matrix
```

### 5.4 Initialisation ordering

MonoGame's `base.Initialize()` calls `LoadContent()`. `Core.Initialize` registers
`Renderer` + `TimerSystem`, calls `OnInitialize()`, and only *then* calls
`base.Initialize()` — which means `Sandbox.LoadContent`'s renderer registrations land
**after** everything registered in `OnInitialize`. The shim must reproduce that order
exactly, or the game runs but draw order silently changes.

## 6. Content

Don't ship `.xnb`. The browser already decodes PNG, and the source images are sitting
in the repo — so the web target copies `Content/**/*.png` into `wwwroot/`, and the
`ContentManager` shim maps `"Ships/ship_freighter_32x32"` to a fetch of
`Content/Ships/ship_freighter_32x32.png`. No MGCB in the web build at all, which also
means no `dotnet tool restore` step.

Two fidelity details that must be replicated, or the art will look wrong:

- **`ColorKeyEnabled=True`, `ColorKeyColor=255,0,255`** — magenta must become
  transparent, exactly as MGCB does it.
- **`PremultiplyAlpha=True`** — MGCB premultiplies, so WebGL blending must match or
  every sprite gets a dark halo.

Also: **retain the CPU-side pixel array** on `Texture2D`. `SpriteSet.Tint` calls
`GetData` to bake per-faction recoloured copies at load, so `GetData` has to actually
work rather than throw.

## 7. The input harness

This is the piece you described, and it's where browser reality diverges from XNA's
polling model most sharply. Each row is a real trap, not a hypothetical.

| XNA surface | Browser reality |
|---|---|
| `Keyboard.GetState()` | No polling API exists. Maintain a state set from `keydown`/`keyup`. **Clear all keys on `blur`** or they stick down forever when the user alt-tabs. Needs a `KeyboardEvent.code` → `Keys` mapping table. |
| Right mouse button | The `contextmenu` event **must** `preventDefault()` — the RTS uses right-click for move orders, so the browser menu would otherwise eat every command. |
| `ScrollWheelValue` / `ScrollWheelDelta` | `Camera.Update` only reads the delta's *sign*, but normalise `wheel` to XNA's ±120-per-notch anyway and handle `deltaMode` variance across browsers, or zoom feels wrong or inverted. |
| `Mouse.SetPosition` | Not implementable without Pointer Lock. **Verified: nothing calls it** — reachable only through `MouseInfo`'s own setters, which no game code uses. Ship a no-op that still updates `CurrentState`, so behaviour stays sane if that changes. |
| `IsFullScreen = true` | `Sandbox` passes `fullscreen: true`. Browsers require a user gesture for fullscreen. Ignore the flag, size the canvas to the page, and offer an optional fullscreen button. |
| `Exit()` | Called on <kbd>Esc</kbd>. A tab can't close itself. Halt the loop and show a paused/ended overlay. |
| `Window.Title = ...` | `Sandbox.OnUpdate` sets the title **every frame** with a status line. Maps to `document.title`, but cache it and only write on change — a per-frame interop call plus tab-label churn is pure waste. |
| Frame pacing | Desktop runs `IsFixedTimeStep` at 60Hz. `requestAnimationFrame` on a 144Hz display would run the simulation **2.4× fast**. A fixed-step accumulator is mandatory to keep gameplay tuning identical to desktop. |
| Backgrounded tab | `rAF` stops firing. On resume, clamp elapsed time or the accumulator tries to catch up on minutes of missed frames and locks the page. |
| Canvas size / DPI | Feed `devicePixelRatio` and a `ResizeObserver` into `GraphicsDevice.Viewport.Bounds`, which `Core.Update` reads every frame and hands to the camera. |

## 8. Phasing

**Phase 0 — proof, with a kill gate.** One real sprite, loaded through the real
`SpriteSet` / `SpriteLibrary`, on screen in a browser via a single `dotnet publish`.
This exercises the whole spine: source-linking, the `Game` shell, rAF, content fetch,
atlas, one draw call. *Kill criterion: if this isn't standing up in two or three
focused sessions, abandon it.* Cost of abandoning: delete a folder.

**Phase 1 — shim breadth.** Full `SpriteBatch` overloads, complete input, camera
transform, content, plus the conformance tests from §5.3.

**Phase 2 — the sandbox actually plays.** Selection, orders, build menu, minimap, the
performance graph. Frame pacing matched to desktop.

**Phase 3 — polish.** Canvas sizing, DPI, pause-on-blur, loading indicator, and a
one-command publish that emits a static folder droppable on any static host or itch.io.

**Phase 4 — optional, generalise.** Point the same `Web/Xna.Shim` at `Proto4X` to prove
the target is engine-wide rather than sandbox-specific. This is the real test of the
"applies to any game built from my engine" goal, and it's cheap once Phase 2 works,
because the shim is shared and the per-game project is ~20 lines of csproj.

## 9. What this costs you

- One decision: yes / no / not now.
- One command, once: `dotnet workload install wasm-tools` (Microsoft-published; may
  need an elevated prompt). Neither `wasm-tools` nor `wasm-experimental` is currently
  installed on this machine — verified. I can run it if you'd rather not.
- Nothing else. No edits to your projects, no solution change, no new dependency in
  your game build, no new step in your daily loop.

## 10. The honest ongoing cost: drift

The shim breaks only when you add a **new** MonoGame API to the engine.
`ENGINE_REQUESTS.md` already says what's coming:

| Coming request | Shim impact |
|---|---|
| *"No text rendering"* | **The expensive one.** Needs `SpriteFont` + `DrawString`. Genuinely easier in a browser (render glyphs to a canvas, bake an atlas) but it is real work. |
| *"Drawable carries no tint, scale or draw order"* | Nearly free — more `SpriteBatch.Draw` overloads and a sort key, both already in the design. |
| *"Camera position can only be changed by the arrow keys"* | No shim impact — that's engine-side. |

Two properties make this tolerable:

- **The failure mode is always a broken web build, never a broken desktop build.** That
  asymmetry is the entire point of the additive design. You can ignore a red web build
  indefinitely.
- **The drift is in-repo and mine to fix.** Which is exactly what KNI's drift wouldn't
  be: if nkast renames packages again — and he already has, once — that's an external
  migration on someone else's schedule.

## 11. Which one, and when

Recommendation: **Option B, gated on Phase 0.**

But here's the decision rule rather than just the answer, because there is a real case
for KNI:

> **Switch to KNI if the web build ever stops being a curiosity** — if it becomes a way
> you actually ship or demo, or if you want audio, text, and custom shaders soon. KNI
> gives you all three for free the day you want them; with the shim, each is work I
> have to do. Paying a single-maintainer dependency's churn tax is reasonable for a
> target you actually depend on. It isn't reasonable for one you've told me is a
> low-priority side curiosity.

Phase 0 is deliberately cheap enough that finding this out costs almost nothing, and
KNI stays available as the escape hatch either way — the audit says it's fit to use.

---

## Sources

- [MonoGame issue #8102 — WebAssembly support](https://github.com/MonoGame/MonoGame/issues/8102) (closed as not planned)
- [MonoGame 3.8.5 release notes](https://monogame.net/blog/2026-07-15-3.8.5-release-2026/)
- [kniEngine/kni on GitHub](https://github.com/kniEngine/kni)
- [nkast.Kni.Platform.Blazor.GL on NuGet](https://www.nuget.org/packages/nkast.Kni.Platform.Blazor.GL)
- [nkast.Xna.Framework on NuGet](https://www.nuget.org/packages/nkast.Xna.Framework)
- [JSImport/JSExport interop with a WebAssembly Browser App — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/client-side/dotnet-interop/wasm-browser-app?view=aspnetcore-10.0)
