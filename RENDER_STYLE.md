# Blocky 3D renderer: production pass

Branch `render/production` (folder `Proto4X-render\production`). It starts from the blocky3d spike,
`render/blocky3d`, and turns it into something to build on. `git diff render/blocky3d` shows exactly
what changed.

This document has six parts:

1. Whether the spike was production-ready. It wasn't, and the list says why.
2. What changed.
3. Measurements.
4. The choices still open for a final solution.
5. A walkthrough of the implementation and the MonoGame APIs it uses.
6. How to bring it into the live repo.

## How this was checked

The game was never launched, per the standing rule. Everything below was checked headlessly:

- **Build:** the whole solution builds with 0 warnings and 0 errors (Sandbox, Proto4X, Factory, Headless, Analyzers).
- **Simulation:** the 42 sim checks pass. Determinism is unchanged; nothing in the sim reads the new content versions.
- **Picking:** `picking` gives 0 wrong tiles on the planet at two zooms and on the turned ship.
- **`render-check`:** new. 101 checks; details in 5.11.
  - What the chunk cache draws equals a full rebuild, pixel for pixel. This holds when warm, and after each of these: ore mined out, buildings removed (both pending and torn down), belts placed across a chunk edge (before and after the tick), a whole stress block stamped, and 300 ticks of normal running.
  - Frustum culling on equals culling off, pixel for pixel.
  - No see-through holes.
  - The ship relights when it turns under a locked camera.
- **Against the spike:** the snapshots compare like this.
  - Planet shots: every pixel is within ±2 per channel, except 2–30 single pixels per 1280×720 image on shadow edges. Those flip coverage because the shadow is now projected by a matrix at draw time.
  - Ship shot: 2% of pixels differ, which is intended. The sun no longer turns with the deck.
  - The pictures are in `../snapshots/production/`, next to the spike's in `../snapshots/blocky3d/`.
    `ship-spike-vs-production.png` stacks the two ship shots.

**Not verified until you run it:** the GPU path itself, meaning vertex buffers, `BasicEffect` lighting
and the device state. Run `dotnet run --project Proto4X.Factory/Game`.
- **If the world is blank or wrong, press F4.** It switches to the immediate path, which streams the
  same meshes from the CPU every frame, the way the spike drew. If F4 fixes it, the fault is in the
  vertex buffer code (`StaticMesh.Upload` / `DrawLayer`).
- **If both modes are wrong,** suspect the lit `BasicEffect` setup (`SceneLighting.Configure`).

---

## 1. Was the spike production-ready?

**No.** It was a sound proof of the style: the models, the camera, the picking and the CPU rasterizer
were all good. It was not a foundation. The findings, most serious first:

| # | Finding | Severity | Status |
|---|---|---|---|
| 1 | **Everything rebuilt and re-uploaded every frame.** At mid zoom over the stress blocks that was 271k triangles, 8.4 ms of CPU, and about 15 MB of vertices and indices streamed to the GPU, all to redraw things that hadn't changed. It doesn't scale with factory size. | Blocking | **Fixed:** per-chunk retained meshes (§5.4, §5.8) |
| 2 | **Lighting was baked in each surface's *local* space.** `ModelMesher` lit faces with local normals, and the ship's turn was applied only later on the GPU. On a turned ship the sun turned with the deck, and so did the shadows. The spike README's claim that the ship was "lit correctly" was wrong. | High, given that ships turn freely | **Fixed:** lighting on the GPU in world space; shadow matrix aimed per frame (§5.6, §5.7) |
| 3 | **No render invalidation existed.** The grid's chunk versions were built for the sim's neighbour caches and miss what a viewer needs. Mining out ore bumps nothing. Removal clears occupancy a tick before the entity goes. Belt shapes only settle at the next tick. | Blocking, once anything is cached | **Fixed:** separate content versions plus "provisional" meshes (§5.8) |
| 4 | **Four styles' worth of code in one tree:** sprite baker, 2D renderers, `ViewState`, pixel-unit `SurfaceView`. The 3D renderers borrowed helpers from the 2D ones. | Medium (noise, coupling) | **Fixed:** pruned. The shared helpers moved to `MachineState` and `BeltItems`. |
| 5 | **Culling was a ground-footprint box with ad-hoc padding per system.** That is loose for perspective views (about 1.35× over-fetch) and wrong for tall things seen from low angles. | Medium | **Fixed for static content:** a real frustum test per chunk. Dynamic parts still pad the box, which is fine for small things. |
| 6 | **No ownership of GPU resources.** Nothing was disposable. | Low now, high once meshes are retained | **Fixed:** `StaticMesh`, `SurfaceMeshes` and `RenderStyle` are `IDisposable` |
| 7 | **`OrbitCamera` inherits the 2D `Camera`'s API.** `Zoom`, `Pan`, `RotateAround`, `ScreenTransform` and `WorldProjection` mean nothing for it, but are callable. | Medium (API honesty) | **Open.** See §4.8 |
| 8 | **The mirrored view needs culling off.** The ground frame (x east, y south, z up) is left-handed, so the view mirrors x, which flips winding, so back-face culling stays off. That costs up to 2× fragment work on closed boxes. | Low (GPU cost never measured) | **Open, deliberately.** See §4.7 |
| 9 | **Engine: the system query cache never caches.** `_cachedComponentArrays` is never written to. `GetValueOrDefault(key, fallback)` evaluates its fallback argument every call, so every system re-runs a LINQ archetype query every frame. That is the ~6 KB/frame of allocation in every measurement. | Low to medium | **Open, not touched.** Caching it properly needs invalidation when new archetypes appear (commit 2faef50 notes this). `SystemScheduler.cs:60`, `:102`, `:113`, `:143` |
| 10 | **The GPU path had never executed.** Correctness rested on the CPU rasterizer copying its rules. | Inherent to the no-launch rule | **Mitigated:** F4 fallback, plus the CPU rasterizer now mirrors the lit path exactly |

What was already production-quality and was kept as is:
- `OrbitCamera`: its picking ray, the pitch clamp that keeps the horizon off screen, and orthographic and perspective framed the same.
- The block models and the CPU rasterizer's clipping and edge rules.
- 16-bit-index paging, so meshes draw on any graphics profile.
- The per-world draw transform.
- The headless tooling.

---

## 2. What changed

```
                    engine (MonoGameLibrary/Graphics)                         game (Proto4X.Factory)
  ┌──────────────────────────────────────────────────────┐   ┌──────────────────────────────────────────────┐
  │ MeshVertex        position, normal, colour, uv (36 B) │   │ SurfaceMeshes   per-chunk cache: body (3 LOD │
  │ GeometryBuffer    CPU triangles by layer, 32k pages   │◄──┤                 tiers) + shadow casters,     │
  │ StaticMesh        compact copy + GPU vertex/index buf │◄──┤                 keyed on ChunkContentVersion │
  │ SceneLighting     sun + ambient; BasicEffect config;  │   │ StaticSurface3DRenderer  frustum-cull chunks,│
  │                   planar shadow matrix                │   │                 submit meshes + shadow matrix │
  │ GeometryBatch     per frame: retained draws +         │◄──┤ live renderers  chevrons, items, arms, status │
  │                   Immediate buffer → Flush per layer  │   │                 cubes, removals, bars, ghost  │
  │ GeometryDrawContext  culling rect + frustum + world   │   │ SurfaceGrid     + ChunkContentVersion (sim)   │
  └──────────────────────────────────────────────────────┘   └──────────────────────────────────────────────┘
```

The split follows ENGINE_LAYERING.md. Vertex formats, buffers, lighting and the batch are
genre-agnostic, so they are engine. Models, chunking and invalidation are this game's own.

Engine API changes, which are breaking only for geometry systems (Factory is the only user):
- `IGeometryDrawSystem.Draw` takes an `in GeometryDrawContext` instead of a bare `Rectangle`.
- `GeometryBatch` no longer has `AddQuad`. Immediate geometry goes into `batch.Immediate`.
- `GeometryLayer` gained `Emissive`. Opaque is now lit, and there is no alpha test.
- `SystemScheduler.CreateGeometryContext` is new. Headless callers use it.

---

## 3. Measurements

From headless `bench-draw`: both stress blocks (5,694 buildings), 1280×720, medians of 50. It measures
the CPU cost of the geometry pass, and not the GPU.

| zoom | tier | spike: tris, ms | production, cache on: tris, ms | production, cache off: ms |
|---:|---|---:|---:|---:|
| 1 | Fine | 39k, 3.80 | 96k, **0.50** | 3.79 |
| 0.5 | Fine | 134k, 6.21 | 221k, **1.30** | 6.64 |
| 0.25 | Mid | 271k, 8.42 | 286k, **2.26** | 8.75 |
| 0.1 | Coarse | 63k, 2.54 | 63k, **0.14** | 2.00 |

- **CPU per frame drops 4–18×.** What remains is the live parts (belt items, arms, status cubes) plus
  one frustum test per chunk. "Cache off" matches the spike, which confirms like is being compared with like.
- **The GPU now draws more triangles up close** (96k against 39k at zoom 1). Whole 32×32 chunks are
  drawn when any part of one is visible, along with their shadows. That is the deliberate trade:
  - CPU time is what scales with factory size.
  - A hundred thousand flat-shaded triangles is trivial for any GPU from the last decade.
  - Smaller chunks reduce it (§4.3).
- **A cold build of every planet chunk** (48 chunks, all tiers, ~700k triangles) costs:
  - Fine tier: 36 ms in total, 1.5 ms for the worst single chunk
  - Mid tier: 24 ms
  - Coarse tier: 9 ms
  - Shadows: 10 ms

  A typical edit rebuilds 1–4 chunk meshes. The frame budget caps rebuilds of already-built chunks at
  4 per frame; a chunk that has never been built is built immediately.
- **Memory:** 52 MB on the CPU with every tier of every chunk built, and the same again on the GPU.
  - That worst case is ~9 KB per building per copy, terrain included.
  - At the 56k buildings of the sim benchmark it would be ~0.5 GB per copy.
  - That is the first scaling limit (§4.5). Two fixes: drop the CPU copy, and evict unused tiers.
- **Allocation:** still ~6 KB/frame, all of it from finding #9. The new code allocates nothing per frame at steady state.

---

## 4. Choices for a final solution

Each choice lists what this branch does, the alternatives, and when to switch.

### 4.1 Lighting: `BasicEffect` versus a custom shader

**Chosen:** stock `BasicEffect`, per-vertex, one directional light plus ambient. It needs no content
pipeline step, and its formula is simple enough to copy exactly in the CPU rasterizer.

**Costs:** it can't do the following.
- Per-vertex emissive. That is why `Emissive` is a separate layer and an extra draw call per mesh.
- UV animation. That is why belt chevrons are rebuilt every frame instead of scrolling in a shader.
- Alpha test while lit.
- Multiple shadowed lights, fog-of-war, or tinting per instance.

**Alternative:** write one `.fx` file, compiled by MonoGame's `mgfxc` through the MGCB content pipeline.
The `Content.mgcb` files exist but nothing goes through them yet.
- A custom shader would fold the four layers into two.
- It would scroll chevrons for free and allow instancing (§4.6).
- It would make later effects possible: outlines, selection highlights, a blueprint overlay.

**Switch when:** you want any of those, or textured models (§4.9). You will want a custom shader
eventually. The cost is the build step, and the CPU rasterizer must copy whatever the shader does.

### 4.2 Shadows: planar projection, shadow maps, or blobs

**Chosen:** planar projection. Each building's boxes are flattened onto the ground along the sun by a
matrix applied at draw time.
- It is cheap.
- It is exact for boxes on flat ground.
- It works on a turning ship with no rebuild.

**Costs:**
- Shadows fall only on the ground, never on other buildings.
- One shade per building, taken from the tile under its origin.
- The geometry is drawn twice.

**Alternatives:**
- **Shadow mapping.** Render depth from the sun into a texture, then compare against it per pixel.
  - Shadows fall on everything, and they come out right on a ship because the map is rendered per frame.
  - Needs: a custom shader, a render target, bias tuning to avoid acne and peter-panning, and cascades
    to cover large zoom ranges.
  - It can't be checked headless unless the CPU rasterizer gains it too.
- **Blob shadows.** A dark quad under each building. Cheapest, and good enough at far zoom.

**Switch when:** buildings get tall enough that shadows falling on neighbours matter visually. Planar
shadows are probably the right answer for this art style for a long time.

### 4.3 Chunk size

**Chosen:** 32×32, the same as the sim's `SurfaceGrid` chunks, so the content version is one array lookup.

**Trade:**

| Chunk size | Overdraw at close zoom | Draw calls | Rebuild cost per edit |
|---|---|---|---|
| Smaller (16×16) | Less | 4× more | Cheaper |
| Larger | More | Fewer | Dearer |

At today's numbers (48 chunks, 1.5 ms worst rebuild), 32 is right. Revisit if a dense chunk's rebuild
starts to show as a hitch. A render chunk can be smaller than a sim chunk: keep a per-render-chunk
version array of its own.

### 4.4 Level of detail

**Chosen:** three tiers (Fine, Mid, Coarse), chosen once per frame from the camera's distance to its
target, the same as the spike.

**Alternative:** a tier per chunk, chosen by that chunk's distance to the eye.
- At a low perspective angle, near chunks stay Fine and the horizon goes Coarse.
- The current scheme draws the whole view at one tier, so a low-angle wide view pays Fine detail out to the far edge.
- The cache already stores all three tiers per chunk, so this is a change to
  `StaticSurface3DRenderer.Draw` only: compute a distance per chunk.

**Switch when:** low-angle views over large factories are a real use case. Add hysteresis, meaning
different distances for switching up and down, so chunks on a tier boundary don't flicker.

### 4.5 Memory and rebuild scheduling

**Chosen:**
- The CPU copy of every mesh is kept.
- 36-byte vertices.
- Tiers built lazily and never evicted.
- A count budget of 4 rebuilds per frame, done synchronously.

**Options, roughly in the order to do them:**
1. **Evict unused tiers.** Free a tier no chunk has drawn for N seconds. This cuts memory to about one tier's worth.
2. **Drop the CPU copy after upload.** Keep it only headless, where the CPU rasterizer needs it. The F4
   fallback then has to rebuild instead of re-streaming.
3. **A smaller vertex, about 20–24 B:**
   - pack the normal into 4 bytes (`NormalizedByte4`)
   - drop the UV until there are textures
   - or derive the normal from a face index
4. **A time budget instead of a count:** stop rebuilding once N ms are spent.
5. **Build on a worker thread.** Meshing is pure CPU work over grid and table data. Only the upload has to
   be on the main thread. This needs a read-consistent view of the grid (a copy of the chunk, or
   meshing between ticks), which is the hard part.

### 4.6 Moving things: rebuilt every frame versus instancing

**Chosen:** belt items, arms, status cubes, bars and the ghost are rebuilt into `batch.Immediate` every
frame, culled by the ground rectangle. At current counts that is 1–21k triangles and most of the 0.14–2.3 ms per frame.

**Alternative: hardware instancing.**
- Upload one cube mesh once.
- Each frame, upload only a per-instance buffer: position, rotation, colour.
- Draw with `DrawInstancedPrimitives`.
- Needs: the HiDef graphics profile and a custom shader (§4.1).

**Switch when:** item counts on screen reach the tens of thousands. That is the next CPU bottleneck once
factories get big.

### 4.7 Handedness and back-face culling

**Chosen:**
- The ground frame is x east, y south, z up. That is left-handed.
- `OrbitCamera` mirrors x after the look-at so east is on the right.
- Culling is off.

**Alternatives:**
- **(a) Keep the mirror, but cull clockwise instead of counter-clockwise.** The mirror flips winding, so
  `CullClockwiseFace` culls back faces. This needs every face wound consistently outward:
  - `ModelMesher` already is.
  - The hull edges and flats would need checking.
  - The CPU rasterizer must cull the same way, so a missing face shows up in `render-check`.
- **(b) Make the world right-handed (y north).** This touches the sim, picking and every renderer.

**Switch when:** the GPU is measured as the bottleneck. Until then, culling saves nothing visible and
risks invisible missing faces.

### 4.8 Camera API

The engine's `Camera` is a 2D camera with virtual hooks bolted on. `OrbitCamera` inherits members that are
meaningless for it.

**Recommended:** an abstract camera base, or `ICamera`, holding only:
- `GetViewMatrix`
- `GetProjectionMatrix`
- `ScreenToWorld`
- `WorldToScreen`
- `Viewport`
- `Resize`
- `GetVisibleLocalBounds`

Then `Camera2D` and `OrbitCamera` both derive from it. This touches the Sandbox and Proto4X camera
controls. Do it when the next game picks up the 3D camera.

### 4.9 Textured models (the Kenney kits)

The batch already carries a texture atlas and UVs, but everything samples the white texel today. Real
textured models need four things:
1. **A model loader.** glTF or OBJ at runtime, or MonoGame's `Model` through the content pipeline.
2. **A texture atlas** with padding between entries, and mipmaps.
3. **Linear sampling.**
4. **A rule for which mesh goes in which layer.**

One pitfall already bit this branch: anything that writes untextured geometry must know where the
white texel is (`GeometryBuffer.WhiteUv`), or it samples transparent atlas space and vanishes.
`render-check` now fails on any see-through pixel to catch exactly that. Reintroducing the bug makes
80 of its checks fail.

### 4.10 Smaller decisions

- **Anti-aliasing.**
  - Today there is none, as in the spike.
  - The fix: `GraphicsDeviceManager.PreferMultiSampling = true`, which gives MSAA. It is cheap on
    modern GPUs and removes the stair-stepping on box edges.
  - That is a one-line change in `Core`, plus a matching supersample in the snapshots.
- **Translucency.** Translucent geometry is drawn in submission order, unsorted. That is fine for a
  ghost and pending removals. It would be wrong for overlapping glass.
- **Worlds don't occlude each other.** Depth is cleared per world, so a ship drawn over the planet
  never hides behind planet buildings. That's right for a separate surface view, and wrong if both are
  ever shown in one scene.
- **Picking hits the ground plane, not models.** Picking a building's roof selects the tile behind it at
  low angles. For model picking, test a ray against each building's box. The bounds already exist.

---

## 5. Walkthrough

The frame, end to end:

```
FactoryGame.OnDraw
 └ SystemScheduler.Draw(world, worldTransform)
    ├ CreateGeometryContext   frustum = world × view × projection, culling rect
    ├ CollectGeometry         each IGeometryDrawSystem, in registration order:
    │   ├ StaticSurface3DRenderer   per chunk: frustum test → batch.Draw(body mesh)
    │   │                                                    → batch.Draw(shadow mesh, shadow matrix)
    │   └ live renderers            triangles into batch.Immediate
    └ GeometryBatch.Flush     per layer (Opaque, Emissive, Translucent, Additive):
                                retained meshes (each its own World matrix), then Immediate
```

### 5.1 Vertices: `MeshVertex` and `VertexDeclaration`

`MonoGameLibrary/Graphics/MeshVertex.cs:19`

A vertex is just a struct. The GPU doesn't know C# types, so a **`VertexDeclaration`** describes the
struct's bytes:

| Element | Byte offset | Format | Feeds the shader's |
|---|---:|---|---|
| Position | 0 | `Vector3` | `POSITION` input |
| Normal | 12 | `Vector3` | `NORMAL` |
| Colour | 24 | `Color` (4 bytes) | `COLOR` |
| UV | 28 | `Vector2` | `TEXCOORD0` |

`VertexElementUsage` is what matches each element to a shader input. `BasicEffect`'s lit, vertex-colour,
textured technique asks for exactly these four. `StructLayout(Sequential, Pack = 1)` guarantees the C#
field layout matches the offsets. Implementing `IVertexType` lets MonoGame find the declaration when you
pass an array of these to a draw call.

### 5.2 CPU geometry: `GeometryBuffer`

`MonoGameLibrary/Graphics/GeometryBuffer.cs:36`

Everything is triangles. **Indexed** drawing stores each corner once in a vertex array, then lists
triangles as triples of indices into it. A quad is 4 vertices and 6 indices: (0,1,2) and (0,2,3), the
two triangles sharing the a–c diagonal (`:104`).

**Indices are `short`** (16-bit), so one array can address at most 32,767 vertices. That is why
geometry lives in **pages** of 32,764 vertices, each drawn with its own call. The alternative, 32-bit
indices, needs the HiDef graphics profile; 16-bit works everywhere.

**Layers** keep things that need different GPU state apart: lit versus unlit, and blended versus not.
Mixing them in one draw call is impossible, because state is per call.

Pages are reused across frames (`Clear` just resets counts), so steady-state filling allocates nothing.

### 5.3 Effects and device state

`MonoGameLibrary/Graphics/GeometryBatch.cs:105`

To draw, the GPU needs four kinds of state.

**1. An `Effect`:** a vertex shader plus a pixel shader, and their parameters.
- **`BasicEffect`** is MonoGame's built-in general-purpose one. Its parameters include `World`, `View`,
  `Projection`, `Texture`, lights and material colours.
- Setting a property only changes a C# field. **`effect.CurrentTechnique.Passes[0].Apply()`** is what
  pushes the parameters to the GPU and binds the shaders.
- That is why `DrawLayer` (`:138`) calls `Apply` again after setting each mesh's `World`.

**2. `BlendState`:** how a new pixel combines with the one already there.
- `Opaque` replaces it.
- `AlphaBlend` (premultiplied) does `src + dst × (1 − srcAlpha)`.
- The custom additive state does `src + dst`.

**3. `DepthStencilState`:**
- `Default`: test against the depth buffer and write to it, which is right for solids.
- `DepthRead`: test but don't write, which is right for translucency, so things behind it still draw.

**4. `RasterizerState`:** culling. `CullNone` here (§4.7).

**`SamplerState`** controls how texture pixels are filtered. Point sampling keeps the white texel exact.

`Flush` sets all of this explicitly. `SpriteBatch` changes the same device state and doesn't restore it,
so assuming any state would break the moment a HUD draws first.

The layer order is **opaque before translucent**. Blended pixels need everything behind them already
drawn, and their depth test needs the solids' depth.

### 5.4 Retained meshes: `StaticMesh`

`MonoGameLibrary/Graphics/StaticMesh.cs`

There are two ways to feed the GPU vertices:

- **`DrawUserIndexedPrimitives(array…)`**
  - Hand MonoGame a C# array.
  - It copies the array into a hidden dynamic buffer and draws it.
  - Simple, but the copy happens **every call**.
  - The immediate path and the spike work this way.
- **`VertexBuffer` + `IndexBuffer`**
  - GPU-side memory you allocate once (`new VertexBuffer(device, declaration, count, BufferUsage.WriteOnly)`).
  - Fill it with `SetData`.
  - Each frame, bind it with `device.SetVertexBuffer(vb)` and `device.Indices = ib`.
  - Draw with `device.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseVertex: 0, startIndex: 0, primitiveCount)`.
  - **Nothing crosses the bus per frame.**
  - `WriteOnly` promises the CPU will never read it back, which lets the driver put it in the fastest memory for the GPU.

`StaticMesh.CopyFrom` (`:58`) copies a scratch `GeometryBuffer` into exact-size arrays. Pages are
1.3 MB; a chunk needs kilobytes. It also computes a bounding box.

`Upload` (`:211`) runs lazily on first draw, and again whenever `CopyFrom` marked the mesh stale. It
reuses the GPU buffers when the new content fits, and recreates them only when it outgrew them.

`DrawLayer` (`:124`) picks buffers or the streaming fallback (`UseVertexBuffers`, the F4 switch).

GPU buffers are unmanaged resources, so `Dispose` them. The garbage collector doesn't know how big they are.

### 5.5 Matrices

MonoGame uses **row vectors**: a point transforms as `v × M`. So `A * B` means "A first, then B", and
transforms compose left to right in reading order:

```
local tile  ──place──►  surface-local  ──World──►  camera world  ──View──►  eye space  ──Projection──►  clip space
            (Facing ×                (surface's                 (look-at                (perspective or
             Translation)             turn + position)           + x mirror)             orthographic)
```

- **World**, per surface, is `RenderStyle.WorldTransform`: rotate about z by the frame's angle, then
  translate. Geometry is built once in surface-local tiles and never touched again. The ship turning
  changes only this one matrix.
- **View**, `OrbitCamera.GetViewMatrix` at `MonoGameLibrary/UI/OrbitCamera.cs:78`:
  `Matrix.CreateLookAt(eye, target, up)` turns the world so the eye sits at the origin looking down −z.
  The `CreateScale(-1, 1, 1)` after it is the handedness mirror (§4.7).
- **Projection** (`:86`): `CreatePerspectiveFieldOfView(fov, aspect, near, far)` makes far things
  smaller. It maps the view volume to clip space, where x and y are in [−w, w] and z in [0, w];
  the GPU divides by w afterwards.
  - Near and far scale with distance, which keeps depth precision proportional. With 24 bits of depth,
    precision is roughly `distance² / (near × 2²⁴)`.
  - `CreateOrthographic` drops the divide, so there is no foreshortening. That gives true isometric and
    the overhead look.
- **Picking** runs the chain backwards: `Matrix.Invert(view × projection)` takes a screen point at
  depth 0 and at depth 1 back to two world points. That line is intersected with z = 0 (`:124`).

### 5.6 Normals and lighting

`MonoGameLibrary/Graphics/SceneLighting.cs`

The formula for lit layers:

`colour × (Ambient + Diffuse × max(0, N · Sun))`

- **Ambient:** 0.52, everywhere.
- **Diffuse:** 0.48 × how directly the face points at the sun.
- **N** is the face's unit normal, in world space.

**Why the GPU, not baked colours:** geometry is built in surface-local space. The GPU applies `World`
to the normals too, so a ship's faces keep facing the right way relative to a sun fixed in the sky,
with no rebuild. Baking (the spike) froze the sun into the ship's frame.

**The inverse-transpose.** Normals don't transform like positions. Under a non-uniform scale or a skew,
transforming a normal by `M` tilts it off its surface; the correct transform is `(M⁻¹)ᵀ`.
- `BasicEffect` computes that for you from `World`, as `WorldInverseTranspose`.
- The CPU rasterizer does the same (`SoftwareRasterizer.cs`, `DrawTransform`).
- For pure rotation it equals `M`. It matters for shadows (§5.7).

**`Configure`** (`:70`) pins `BasicEffect` to exactly the formula above:
- `AmbientLightColor` = ambient
- one `DirectionalLight` with diffuse = 0.48
- zero specular everywhere
- lights 1 and 2 disabled
- per-vertex lighting

Mind the sign: `DirectionalLight.Direction` is the way light *travels*, so it is set to −Sun.

The only shading baked into vertices is the darker foot of side faces (`ModelMesher`). It is fake
ambient occlusion, independent of the sun, so baking it is safe. The GPU multiplies vertex colour by
light, so the foot shade survives.

### 5.7 Planar shadows

`SceneLighting.PlanarShadow` (`:59`)

To flatten a point `p` onto z = 0 along the sun direction `s`, slide it down the sun ray until z hits 0:

`p' = p − s × (p.z / s.z)`

That gives `x' = x − (s.x/s.z)·z` and `y' = y − (s.y/s.z)·z`. As a row-vector matrix, z's row is
`(−s.x/s.z, −s.y/s.z, 0)`.

The shadow mesh is the models' boxes, untransformed. The renderer draws it with
`World = PlanarShadow(localSun) × surfaceWorld` (`Renderers3D.cs:54`).
- `localSun` is the world sun turned back into the surface's frame (`SunInLocal`).
- As the ship turns, the shadows swing correctly with no rebuild.

**Two subtleties:**
- **The sliver.** A true flatten has a zero z column, so it has no inverse. Lighting needs
  `(World⁻¹)ᵀ`, so z is scaled by 0.0001 instead of 0.
- **Normals point straight up.** Every shadow vertex's normal is +z (`ModelMesher.EmitShadow`). A
  horizontal plane stays horizontal under the skew, so up stays up, and the shadow lights exactly like
  the ground under it.

The shadow colour is the tile colour × 0.6. It is drawn opaque, which is why overlapping shadow faces
don't darken twice.

### 5.8 The chunk cache: `SurfaceMeshes`

`Proto4X.Factory/Game/Rendering/ThreeD/SurfaceMeshes.cs`

**Invalidation through content versions** (`SurfaceGrid.cs:137`). Every chunk has a counter. It is
bumped by:
- `SetTile`
- `SetOre`
- `SetOccupant`
- `MineOne`, only on the last unit, at `:105`

It bumps every chunk within one tile of the change, because a belt's curve and a deck's hull edge read
their neighbours.

A cached mesh remembers the counter it was built from. A mismatch means stale (`NeedsBuild`, `:186`).

This array is separate from the sim's `chunkVersions` on purpose. A rendering need must never make
inserters re-resolve their targets.

**Ownership.** A building is meshed by the chunk holding its **origin** tile (`OwnedBuildingAt`,
`:311`), so it is meshed exactly once. Buildings are found **through occupancy**, not through ECS
entities. Removal frees a building's tiles immediately, so it leaves the static mesh at once, and the
live pass draws it see-through until the entity goes.

**Provisional meshes.** Belt curve markers read the belt network's shapes, and those are only
recomputed at the next tick boundary. A mesh that read them while the network was dirty is marked
provisional, and rebuilt once the network is clean.

**Budget** (`Body`, `:127`):
- A never-built chunk is built immediately. The alternative is a hole.
- A stale chunk is rebuilt only while the per-frame budget lasts; otherwise last frame's mesh is drawn.
  For a frame or two, a removed building can linger, rather than the frame hitching.

**Three body tiers plus shadows,** built lazily as the camera visits each tier.

### 5.9 Culling

`StaticSurface3DRenderer` (`Renderers3D.cs:28`)

A `BoundingFrustum` is the six planes of the camera's view volume.
- Built from `view × projection`, its planes are in world space.
- Built from `world × view × projection`, they are in the surface's **local** space.

So the renderer tests each chunk's local-space `BoundingBox` directly, with no per-chunk transform.

**The boxes are conservative** (`BodyBounds`, `:103`):
- the chunk's tiles
- plus 2 tiles of overhang east and south, for a 3×3 model owned at its corner
- from the hull's underside up to the tallest model

Shadows get a box stretched away from the sun by the longest possible shadow (`ShadowBounds`, `:113`).

`render-check` is the evidence the boxes are big enough: culled and unculled renders match pixel for
pixel in all ten test views. That includes the mirrored view, which is the case most likely to break
plane extraction.

`SystemScheduler` keeps **one** `BoundingFrustum` and resets its `Matrix` each frame, because it is a
class and would otherwise allocate.

### 5.10 The live renderers

`Renderers3D.cs`

These rebuild every frame into `batch.Immediate`:
- chevrons (unlit, so they read as markings)
- belt items
- inserter arms (the bases are in the chunk mesh)
- assembler recipe cubes and chest top-items
- buildings awaiting removal
- progress bars
- the build ghost

Each culls by the padded ground rectangle in `GeometryDrawContext.LocalCullingBounds`. The status pass
doesn't iterate at all when nothing it draws would be visible: Mid tier with no pending removals.

### 5.11 Verification tooling

`Proto4X.Factory/Headless`

Run from `Proto4X.Factory/Headless`:

| Command | What it does |
|---|---|
| `dotnet run -c Release` | 42 sim checks plus determinism |
| `dotnet run -c Release -- render-check [dir]` | Cache = rebuild, culling = no culling, no holes, sun fixed. Failing pairs are saved to `dir` as `-a`/`-b` PNGs |
| `dotnet run -c Release -- snapshot [dir]` | The 7 standard shots at 1280×720 |
| `dotnet run -c Release -- bench-draw` | §3's tables |
| `dotnet run -c Release -- picking` | Screen → tile round trip |

The CPU rasterizer (`Software/SoftwareRasterizer.cs`) copies the GPU path rule for rule:
- layer order, and retained meshes before immediate
- depth test and write per layer
- premultiplied blending
- per-vertex lighting with the inverse-transpose normal (`ToClip`, `:153`)
- D3D-style clip-space z, and near/far clipping

**Any change to `GeometryBatch.Flush` or `SceneLighting.Configure` must be copied there,** or the
snapshots stop meaning anything.

### 5.12 First run checklist

1. **Launch.** The planet should look like `../snapshots/production/blocky3d-factory.png`: lit, with
   shadows falling south-east. That folder holds this branch's seven shots; `../snapshots/blocky3d/` is
   the spike's.
2. **F4.** The title bar says `IMMEDIATE` and nothing on screen should change. If anything changes, the
   two paths disagree; report which one looks right.
3. **Tab to the ship, then turn the camera with Z/X.** The ship's shadows should point the same
   world direction as the planet's, south-east, whatever the ship's heading. They should not turn
   with the deck. `../snapshots/production/ship-spike-vs-production.png` shows the difference: spike on
   top, this branch below.
4. **F5 a stress block.**
   - At most a frame or two of the old mesh in the stamped area.
   - No hitch beyond the stamp itself.
5. **Zoom out across distance 70 and 140.** These are the tier switches, with a one-off build the first
   time each tier is seen.

---

## 6. Bringing it into the live repo

`D:\Dev stuff\Proto4X` was not touched. Its working tree carries uncommitted work, which commit
`96c0261` (the baseline) snapshotted on top of its `HEAD` (`291b88e`). This branch descends from
`96c0261`, so either route works:

1. **Commit the live WIP first** (it is the same content as `96c0261`), then fetch this branch and merge or rebase.
2. **Cherry-pick by area.** The engine files under `MonoGameLibrary/Graphics` and the two scheduler
   files form one coherent change. The Factory changes and the one-file sim change (`SurfaceGrid.cs`)
   are another.

The base plumbing from `render/base` (`GeometryBatch`, the per-world transform, virtual camera hooks)
comes along either way. It has never been in the live repo.
