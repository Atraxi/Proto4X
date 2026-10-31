# Review guide: render/production vs main

Each section is one reviewable unit: the files, an extract of the key code (trimmed, with `// …` where
lines are omitted), what it does, why, the alternatives, and what to check. Sections are ordered so each
only depends on earlier ones. Line numbers refer to HEAD of `render/production`.

Review the 3D work with `git diff 96c0261 HEAD`. The baseline `96c0261` is your own snapshotted WIP. The
engine parts of it are covered by `git diff main 96c0261 -- MonoGameLibrary`.

Verified by you: the game was run after the CPU cleanup and the factory renders correctly, so the retained GPU path works. No automated render check remains (the CPU rasterizer and pixel-comparison tools were deleted), so regressions would only show up by eye.

| # | Unit | Layer | Risk to existing code |
|---|---|---|---|
| 1 | Vertex format | engine | none (new) |
| 2 | CPU triangle buffer | engine | none (new) |
| 3 | Retained GPU mesh | engine | none (new) |
| 4 | Lighting + planar shadow | engine | none (new) |
| 5 | Batch flush (device state) | engine | none (new) |
| 6 | Scheduler + geometry context | engine | **modifies** `SystemScheduler.Draw` |
| 7 | Camera base refactor | engine | **modifies** `Camera`, removes input |
| 8 | OrbitCamera | engine | none (new) |
| 9 | Sandbox camera input | game | replaces removed code |
| 10 | Block models + mesher | Factory | none (new) |
| 11 | Content versions | Factory sim | +43 lines in `SurfaceGrid` |
| 12 | Chunk mesh cache | Factory | none (new) |
| 13 | Chunk draw + culling | Factory | none (new) |
| 14 | Live renderers | Factory | none (new) |
| 15 | Wiring | Factory | none (new) |

---

## 1. Vertex format
`MonoGameLibrary/Graphics/MeshVertex.cs`

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MeshVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate) : IVertexType
{
    public Vector3 Position = position;          // offset 0
    public Vector3 Normal = normal;              // 12
    public Color Color = color;                  // 24
    public Vector2 TextureCoordinate = textureCoordinate;   // 28   => 36 bytes

    public static readonly VertexDeclaration VertexDeclaration = new(
        new VertexElement(0,  VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
        new VertexElement(24, VertexElementFormat.Color,   VertexElementUsage.Color, 0),
        new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));
}
```

- **Does:** defines the one vertex layout, which `BasicEffect` needs in order to light and tint at once.
- **Why:** MonoGame has no built-in vertex type with position, normal, colour and UV.
- **Alternatives:**
  - Pack to ~20–24 B. Normal as `NormalizedByte4`, or drop the UV until textures exist.
  - Separate streams for static and dynamic data. This only pays off with a custom shader.
- **Check:** the offsets match the field order. They do (3+3 floats = 24, `Color` = 4, UV at 28).
  The UV is unused today.

## 2. CPU triangle buffer
`MonoGameLibrary/Graphics/GeometryBuffer.cs`

```csharp
public enum GeometryLayer { Opaque = 0, Emissive = 1, Translucent = 2, Additive = 3 }

public const int MaxVerticesPerPage = 32_764;      // so a short index can address it

public void AddQuad(GeometryLayer layer, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color colour)
// …
    var page = layers[(int)layer].Reserve(4);
    // 4 vertices, then two triangles sharing the a-c diagonal:
    indices[i]     = (short)v;  indices[i + 1] = (short)(v + 1);  indices[i + 2] = (short)(v + 2);
    indices[i + 3] = (short)v;  indices[i + 4] = (short)(v + 2);  indices[i + 5] = (short)(v + 3);
```

- **Does:** collects indexed triangles per layer into fixed-size, reused pages. Both the per-frame immediate
  path and the retained path write into this one format.
- **Why:** 16-bit indices work on every graphics profile, so paging replaces a 4-byte index. The layers
  exist because lit/unlit and blended/opaque need different GPU state.
- **Alternatives:** 32-bit indices on HiDef (no paging code); a `List<MeshVertex>` per layer (allocates).
- **Check:** a page is ~1.3 MB. That is fine as shared scratch, which is why `StaticMesh` copies compactly
  (§3). `Reserve` starts a fresh page when a quad wouldn't fit. Quads are never split across pages.

## 3. Retained GPU mesh
`MonoGameLibrary/Graphics/StaticMesh.cs`

```csharp
public void CopyFrom(GeometryBuffer source)       // exact-size copy, per page, per layer
// …
segments[page].Fill(vertices, vCount, indices, iCount);

// Segment.Upload — lazily, on first draw
if (VertexBuffer is null || VertexBuffer.VertexCount < VertexCount)
{
    VertexBuffer?.Dispose();
    VertexBuffer = new VertexBuffer(device, MeshVertex.VertexDeclaration, Vertices.Length, BufferUsage.WriteOnly);
}
// …
VertexBuffer.SetData(Vertices, 0, VertexCount);

// DrawLayer
device.SetVertexBuffer(segment.VertexBuffer);
device.Indices = segment.IndexBuffer;
device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, segment.IndexCount / 3);
```

- **Does:** keeps a mesh in GPU memory so a static chunk costs one draw call and no per-frame transfer. Refills
  reuse the buffers when the new content fits.
- **Why:** this is the headline performance win. Without it, 271k triangles were re-uploaded per frame.
- **Alternatives:**
  - One big dynamic buffer updated with sub-ranges.
  - Drop the CPU copy after upload. This halves memory but complicates refills.
  - Mesh straight into a mapped buffer.
- **Check:**
  - The CPU copy is kept, so memory is doubled (52 MB each side worst case).
  - It is `IDisposable`, and the owners must call it (§12).
  - Now confirmed working by a real launch. It has no automated check any more.
  - `gpuStale` is set by `Fill` and `Empty`, and cleared by `Upload`.

## 4. Lighting and planar shadow
`MonoGameLibrary/Graphics/SceneLighting.cs`

```csharp
// colour × (Ambient + Diffuse × max(0, N·Sun)), evaluated per vertex on the GPU
internal void Configure(BasicEffect effect)
{
    effect.LightingEnabled = true;
    effect.AmbientLightColor = new Vector3(Ambient);              // 0.52
    effect.DirectionalLight0.Direction = -sun;                    // BasicEffect shines *along* Direction
    effect.DirectionalLight0.DiffuseColor = new Vector3(Diffuse); // 0.48
    effect.SpecularColor = Vector3.Zero;
    effect.DirectionalLight1.Enabled = false;  effect.DirectionalLight2.Enabled = false;
}

public static Matrix PlanarShadow(Vector3 localSun, float lift)
{
    const float sliver = 1e-4f;   // a true flatten has no inverse, and lighting needs World⁻¹ᵀ
    return new Matrix(
        1, 0, 0, 0,
        0, 1, 0, 0,
        -localSun.X / localSun.Z, -localSun.Y / localSun.Z, sliver, 0,
        0, 0, lift, 1);
}
```

- **Does:** one directional sun plus ambient, done by the GPU. `PlanarShadow` flattens geometry onto z = 0
  along the sun.
- **Why:** the spike baked light into vertex colours using local-space normals. A turning ship therefore
  turned its sun and shadows. Lighting on the GPU in world space fixes that with no rebuild.
- **Alternatives:**
  - A custom `.fx` shader (also gives emissive, UV scroll, instancing).
  - Shadow mapping (shadows on buildings; needs a shader and a render target).
  - Blob shadows.
  - Baked light (the spike).
- **Check:**
  - The sign of `-sun` (a wrong sign makes everything dark).
  - The `sliver` trick.
  - Shadows are not clipped to the deck (see the `shortcut:` in §12).
  - The maths is the part most worth re-deriving by hand. Row-vector convention: `z`'s row is
    `(-s.x/s.z, -s.y/s.z, ·, 0)`.

## 5. Batch flush and device state
`MonoGameLibrary/Graphics/GeometryBatch.cs`

```csharp
device.Clear(ClearOptions.DepthBuffer, Color.Transparent, 1f, 0);   // per world
device.RasterizerState = RasterizerState.CullNone;

DrawLayer(device, GeometryLayer.Opaque,      world, BlendState.Opaque,     DepthStencilState.Default);
DrawLayer(device, GeometryLayer.Emissive,    world, BlendState.Opaque,     DepthStencilState.Default);
DrawLayer(device, GeometryLayer.Translucent, world, BlendState.AlphaBlend, DepthStencilState.DepthRead);
DrawLayer(device, GeometryLayer.Additive,    world, premultipliedAdditive, DepthStencilState.DepthRead);

// per retained mesh: its own World, so its own Apply
effect.World = draw.Transform * world;
effect.CurrentTechnique.Passes[0].Apply();
draw.Mesh.DrawLayer(device, layer);
// then the Immediate buffer, via DrawUserIndexedPrimitives
```

- **Does:** at flush time, sets all the device state, draws each layer in order (retained meshes first, then
  immediate triangles), and clears depth per world.
- **Why:** `SpriteBatch` leaves its own state behind (no depth, back-face culling), so nothing may be
  assumed. Opaque goes first so blended pixels can depth-test against solids.
- **Alternatives:**
  - Custom shader: collapses 4 layers to ~2 and avoids an `Apply` per mesh.
  - Back-face culling: needs consistent winding (see `OrbitCamera`, §8).
  - Sort translucent geometry (it is currently drawn in submission order).
- **Check:**
  - Each retained mesh costs an `Apply` plus a draw per layer it uses. At 48 visible chunks this is cheap,
    but it grows with visible chunk count.
  - `Sampler` is set after each `Apply`.
  - Clearing depth per world means a ship never occludes against the planet. That is intended.

## 6. Scheduler and geometry context
`MonoGameLibrary/Systems/Infrastructure/SystemScheduler.cs` and `IGeometryDrawSystem.cs` (**modified existing**)

```csharp
public void Draw(SpriteBatch spriteBatch, UserInterfaceManager ui, GameWorld gameLayer, Matrix worldTransform)
{
    var camera = ui.Camera;
    var cullingBounds = ui.GetVisibleLocalBounds(worldTransform);

    if (_geometrySystems.TryGetValue(gameLayer, out var geometrySystems))
    {
        Geometry.Clear();
        _geometryFrustum.Matrix = worldTransform * camera.GetViewMatrix() * camera.GetProjectionMatrix();
        var context = new GeometryDrawContext(cullingBounds, _geometryFrustum, worldTransform);
        foreach (var geometrySystem in geometrySystems) { /* … geometrySystem.Draw(Geometry, context, archetypeChunks) */ }
        Geometry.Flush(spriteBatch.GraphicsDevice, worldTransform, camera.GetViewMatrix(), camera.GetProjectionMatrix());
    }
    if (_drawWorldSystems.TryGetValue(...)) { spriteBatch.Begin(transformMatrix: worldTransform * camera.GetCameraTransform()); /* … */ }
    if (_drawScreenSystems.TryGetValue(...)) { spriteBatch.Begin(); /* … */ }
}

public readonly record struct GeometryDrawContext(Rectangle LocalCullingBounds, BoundingFrustum LocalFrustum, Matrix WorldTransform);
```

- **Does:**
  - Adds a geometry pass before the sprite passes. Systems register as `IGeometryDrawSystem`.
  - The context gives a system the visible ground box and a frustum in the surface's local space.
  - A per-world `worldTransform` positions a ship.
  - A world without some pass kinds no longer throws.
- **Why:** it plugs the 3D batch into the existing ECS scheduling. The frustum is built from
  `world × view × projection`, so local-space chunk boxes can be tested with no transform.
- **Alternatives:**
  - A separate `Renderer3D` outside the scheduler. This avoids touching existing code but loses archetype
    queries.
  - Per-system transform instead of per-world.
- **Check:**
  - **The old `Draw(…)` overload is kept and forwards with `Matrix.Identity`.** Confirm that Sandbox and
    Proto4X still draw identically.
  - `GetValueOrDefault(key, <fallback evaluated eagerly>)` is the **pre-existing** never-caches bug,
    copied into the new geometry loop.
  - The single reused `BoundingFrustum` assumes worlds draw sequentially.

## 7. Camera base refactor
`MonoGameLibrary/UI/Camera.cs`, `UserInterfaceManager.cs`, `Core.cs` (**modifies existing, removes behaviour**)

```csharp
public class Camera(float minZoom = 0.05f, float maxZoom = 20f)
{
    public Matrix WorldProjection { get; set; } = Matrix.Identity;   // flat layout (e.g. isometric)
    public Matrix ScreenTransform { get; set; } = Matrix.Identity;   // pan / zoom / rotate in pixels

    public virtual Matrix GetViewMatrix()       => GetCameraTransform();
    public virtual Matrix GetProjectionMatrix() => Matrix.CreateOrthographicOffCenter(0, Viewport.Width, Viewport.Height, 0, -1, 1);
    public virtual Vector2 GroundToScreen(Vector2 world);
    public virtual Vector2 ScreenToGround(Vector2 screen);

    public Rectangle GetVisibleLocalBounds(Matrix worldTransform) { /* 4 screen corners → ScreenToGround → inverse world → AABB */ }
    public void Pan(Vector2 d); public void ZoomAt(Vector2 p, float f); public void RotateAround(Vector2 p, float r);

    internal void Update(Rectangle viewportBounds) { /* resize keeps the centre fixed; NO input handling any more */ }
}

// Core.cs
protected virtual Camera InitializeCamera() => new();
```

- **Does:**
  - Makes the camera an object with overridable view, projection, project and unproject, so `OrbitCamera` can
    plug in.
  - Removes keyboard/mouse reading from `Camera.Update`.
  - `Core` can now be given a camera subclass.
  - The geometry pass's depth format is requested explicitly: `PreferredDepthStencilFormat = Depth24Stencil8`.
- **Why:** culling and picking must work for a perspective camera, and 3D games want different controls.
- **Alternatives:**
  - An abstract base or `ICamera` (`Camera2D` and `OrbitCamera` as siblings). The branch's own doc recommends
    this; it fixes `OrbitCamera` inheriting 2D members it can't use (finding #7).
  - Leave `Camera` alone and add a separate 3D camera type. This is less shared-code churn but duplicates
    the culling and picking hooks.
- **Check (highest merge risk):**
  - Input was removed from `Camera`. A grep found no use of `Camera` in `Proto4X/`; Sandbox is the one
    consumer, and §9 replaces its input.
  - `Pan`, `ZoomAt` and `RotateAround` replace the old ad-hoc `cameraMatrix *=` code. Check the sign
    conventions against the old behaviour.

## 8. OrbitCamera
`MonoGameLibrary/UI/OrbitCamera.cs`

```csharp
public override Matrix GetViewMatrix()
{
    var eye = Orthographic ? Target + (Eye - Target) * (OrthographicEyeDistance / Distance) : Eye;
    return Matrix.CreateLookAt(eye, Target, Vector3.UnitZ) * Matrix.CreateScale(-1f, 1f, 1f);   // x mirror
}

public override Matrix GetProjectionMatrix() { /* ortho: same apparent size at target; else CreatePerspectiveFieldOfView(fov, aspect, Distance*0.05, Distance*12+100) */ }

public (Vector3 Origin, Vector3 Direction) ScreenRay(Vector2 screen)
{
    var inverse = Matrix.Invert(GetViewMatrix() * GetProjectionMatrix());
    // un-project the point at depth 0 and depth 1, return the ray between them
}

public override Vector2 ScreenToGround(Vector2 screen)    // intersect the ray with z = 0
{
    // …
    var t = -origin.Z / direction.Z;
    // if the ray misses the ground: clamp to Distance*10 along its flat direction
}
```

- **Does:** orbit camera (yaw, pitch, distance), perspective or orthographic. Picking intersects a screen
  ray with the ground. Pitch is clamped so the horizon never shows, which keeps the culling box bounded.
- **Why:** the style needs a tilted view. The x-mirror compensates for the left-handed ground frame (x east,
  y south, z up), so east stays on the right.
- **Alternatives:**
  - Make the world right-handed (touches the sim, every renderer and picking).
  - Swap the mirror for a clockwise cull mode so back-face culling can be turned on.
  - A fixed-angle isometric camera (simpler; a lot of the code here would not be needed).
- **Check:** the mirror flips triangle winding, which is why `CullNone` is used in §5. Picking hits the
  ground plane only, not model surfaces.

## 9. Sandbox camera input
`Proto4X.Sandbox/Systems/CameraControlSystem.cs` (new), `Sandbox.cs` (+7)

```csharp
if (pan != Vector2.Zero) camera.Pan(Vector2.Normalize(pan) * PanPixelsPerSecond * seconds);
if (keyboard.IsKeyDown(Keys.Z)) camera.RotateAround(screenCentre, MathHelper.ToRadians(RotationDegreesPerSecond) * seconds);
if (mouse.ScrollWheelDelta != 0) camera.ZoomAt(mouse.Position.ToVector2(), mouse.ScrollWheelDelta > 0 ? ZoomStep : 1f / ZoomStep);
```

- **Does:** moves the input the old `Camera.Update` used to read into an explicit system. It also centres the
  camera on the player's station at start.
- **Why:** the engine camera no longer owns input (§7).
- **Alternatives:** keep input in the engine `Camera` behind a flag; one shared `CameraController` for both
  games.
- **Check:** the feel against the old camera. Old speed was 100 px/s per axis; the new one is 600 px/s with
  diagonals normalised. Compare rotation and zoom signs with the old code too.

## 10. Block models and mesher
`Proto4X.Factory/Game/Rendering/Models/BlockModels.cs`, `ModelMesher.cs`, `ThreeD/Mesh3D.cs`, `Palette.cs`

```csharp
public readonly record struct ModelBox(Vector3 Min, Vector3 Max, Color Colour, bool Emissive = false);
public sealed class BlockModel(int size, ModelBox[] boxes) { /* … */ }

// ModelMesher.EmitBox: five faces, never the bottom
Face(buffer, layer, c[4], c[5], c[6], c[7],  Vector3.UnitZ, transform, colour, colour);   // top
Face(buffer, layer, c[0], c[1], c[5], c[4], -Vector3.UnitY, transform, foot,   colour);   // sides: darker foot
// …
var normal = Vector3.Normalize(Vector3.TransformNormal(modelNormal, transform));

// EmitShadow: same boxes, flat colour, every normal straight up. The flattening happens on the GPU (§4)
buffer.AddQuad(GeometryLayer.Opaque, c[4], c[5], c[6], c[7], up, shade);
```

- **Does:** each building is a few coloured boxes, "modelled facing east", rotated by `BlockModels.Facing`.
  `ModelMesher` turns boxes into quads with face normals. The only baked shade is a darker foot, which stands
  in for ambient occlusion and does not depend on the sun. Emissive boxes switch to the unlit layer.
- **Why:** it gives the whole look with no art pipeline. The same meshes feed the chunk cache, ghosts and
  pending removals.
- **Alternatives:**
  - Authored models via glTF/OBJ or MGCB, with a texture atlas (needs a loader, padding, mipmaps and linear
    sampling).
  - Pre-rendered impostor sprites (the spike's baker was pruned).
  - Voxel data with greedy meshing.
- **Check:**
  - `TransformNormal` assumes the matrix is rotation plus uniform scale.
  - Quad winding. With `CullNone` a mistake is invisible, and it only matters if back-face culling is ever
    turned on.
  - The foot shade relies on the vertex order of a side face (`a`/`b` bottom, `c`/`d` top).

## 11. Content versions
`Proto4X.Factory/Simulation/Grid/SurfaceGrid.cs` (+43/−2, in the sim)

```csharp
public void SetTile(int x, int y, TileKind kind) { tiles[y * Width + x] = kind; BumpContentAround(x, y, 1); }

// MineOne: only the last unit changes what the tile shows
if (--oreAmount[index] == 0) { BumpContentAround(x, y, 1); }

public int ChunkContentVersion(int chunkIndex) => contentVersions[chunkIndex];

private void BumpContentAround(int x, int y, int size)   // every chunk within one tile of the footprint
{
    var minChunkX = Math.Max(0, (x - 1) >> ChunkShift);
    var maxChunkX = Math.Min(ChunksWide - 1, (x + size) >> ChunkShift);
    // … same for Y
    for (…) contentVersions[chunkY * ChunksWide + chunkX]++;
}
```

- **Does:** a second per-chunk counter that moves on tile, ore, occupancy and last-ore-unit changes, including
  the neighbouring chunks (belt curves and hull edges read their neighbours).
- **Why:** the sim's own `ChunkVersion` is built for neighbour caches and misses what a viewer needs
  (mining does not bump it). Sharing it would make inserters re-resolve targets whenever the renderer needed
  an update. Nothing in the sim reads the new counter, so determinism is untouched.
- **Alternatives:**
  - An event-driven dirty list: the sim publishes "chunk X changed", with the `EventStream` already in the
    engine as a candidate.
  - Renderer-side polling of a per-tile hash.
  - Reuse `ChunkVersion` and widen what bumps it.
- **Check:**
  - Every mutation path is covered (tiles, ore, occupancy both set and cleared, last unit).
  - Setting occupancy bumps with `size`, so check that clearing goes through the same code.
  - It is the only sim file touched, so verify that the sim hash tests are still green.
  - Integer overflow after ~2 billion bumps is irrelevant.

## 12. Chunk mesh cache
`Proto4X.Factory/Game/Rendering/ThreeD/SurfaceMeshes.cs`

```csharp
public StaticMesh Body(int chunk, DetailTier tier, ref int rebuildBudget)
{
    ref var slot = ref chunks[chunk].Bodies[(int)tier];
    if (NeedsBuild(ref slot, chunk, ref rebuildBudget)) { scratch.Clear(); readBeltShapes = false; MeshBody(chunk, tier); Commit(ref slot, chunk); }
    return slot.Mesh!;
}

private bool NeedsBuild(ref CachedMesh slot, int chunk, ref int rebuildBudget)
{
    if (slot.Mesh is null) return true;                                   // never built: always build, a hole is worse
    var stale = slot.Version != Grid.ChunkContentVersion(chunk) || (slot.Provisional && !surface.Belts.IsDirty);
    if (!stale || rebuildBudget <= 0) return false;                       // stale but over budget: draw the old one
    rebuildBudget--; return true;
}

private void Commit(ref CachedMesh slot, int chunk)
{
    slot.Mesh ??= new StaticMesh();
    slot.Mesh.CopyFrom(scratch);
    slot.Version = Grid.ChunkContentVersion(chunk);
    slot.Provisional = readBeltShapes && surface.Belts.IsDirty;           // built from belt shapes not yet settled
}

// ownership: a building is meshed by the chunk holding its origin tile, found through occupancy
var slot = Grid.OccupantAt(x, y);
return slot >= 0 && buildings.OriginX(slot) == x && buildings.OriginY(slot) == y ? slot : -1;
```

- **Does:**
  - Per 32×32 chunk, holds a body mesh at each of three detail tiers (fine, mid, coarse), plus a shadow mesh.
  - Rebuilds lazily when `ChunkContentVersion` changes, at most 4 stale rebuilds per frame.
  - A mesh built while belt shapes were unsettled is marked provisional and rebuilt later.
  - `BodyBounds` and `ShadowBounds` give conservative boxes that are valid before a chunk is built.
- **Why:** this turns §3's retained mesh into something that stays correct as the factory changes.
- **Alternatives:**
  - Smaller render chunks (16×16): less overdraw, 4× the draw calls.
  - Per-chunk LOD instead of one tier per frame.
  - A time budget instead of a count, or a worker-thread mesher (needs a consistent grid snapshot).
  - Evict unused tiers and drop the CPU copy (~0.5 GB per copy at 56k buildings).
  - An event-driven invalidation (§11 alternatives).
- **Check:**
  - **The first thing to read for correctness.** A stale-but-over-budget chunk keeps drawing the old mesh,
    for a frame or two, and a removed building can linger.
  - `MaxOverhang = 2` and `MaxModelHeight = 3` must stay true for every model.
  - Buildings are found through occupancy, not ECS. A removal frees its tiles a tick before the entity
    goes, hence the see-through pending-removal draw in §14.
  - `shortcut:` at `MeshShadows`: shadows past a deck's edge float.
  - Every tier of every chunk lives forever (never evicted) and is `IDisposable`.

## 13. Chunk draw and culling
`Proto4X.Factory/Game/Rendering/ThreeD/Renderers3D.cs:28` (`StaticSurface3DRenderer`)

```csharp
var localSun = batch.Lighting.SunInLocal(context.WorldTransform);
var shadowTransform = SceneLighting.PlanarShadow(localSun, ShadowLift);

for (var chunk = 0; chunk < meshes.ChunkCount; chunk++)
{
    if (frustum.Intersects(meshes.BodyBounds(chunk)))
        batch.Draw(meshes.Body(chunk, tier, ref budget), Matrix.Identity);

    if (tier != DetailTier.Coarse && frustum.Intersects(meshes.ShadowBounds(chunk, localSun)))
        batch.Draw(meshes.Shadows(chunk, ref budget), shadowTransform);
}
```

- **Does:** per visible chunk, submits the body mesh and the shadow mesh with the shadow matrix. The tier comes
  from `Detail`, which `RenderStyle.PrepareFrame` sets once per frame from the camera distance (fine < 70,
  mid < 140, else coarse).
- **Why:** the per-frame cost is one box-vs-frustum test per chunk plus a draw call per visible mesh.
- **Alternatives:**
  - Per-chunk LOD with hysteresis, so near chunks stay fine at low angles.
  - Rectangle culling (the spike, ~1.35× over-fetch).
  - GPU occlusion or an hierarchical structure, which is overkill at ~48 chunks.
- **Check:**
  - The loop is O(all chunks) per frame. That is trivial now and worth knowing for large maps.
  - Boxes must be conservative. The deleted tooling had checked culled == unculled frames in ten views, and
    that guarantee no longer has a test.
  - Whole chunks are drawn when any part is visible (96k triangles against the spike's 39k at zoom 1).

## 14. Live renderers
`Renderers3D.cs:64` onward (chevrons, belt items, status, progress, launcher, inserter arms, build ghost)

```csharp
var buffer = batch.Immediate;
var visible = SurfaceView.VisibleTiles(context.LocalCullingBounds, surface.Grid, padding: 2);
foreach (var item in BeltItems.Visible(surface, visible))
{
    if (coarse) Mesh3D.Flat(buffer, GeometryLayer.Opaque, /* … */);
    else        Mesh3D.Cube(buffer, GeometryLayer.Opaque, item.Position, 0.2f, 0.1f, Palette.Item(item.Item));
}
```

- **Does:** anything that moves or changes every tick is regenerated into the immediate buffer each frame,
  culled by the padded ground rectangle. Chevrons are emissive so they read as markings. Buildings awaiting
  removal draw see-through.
- **Why:** it is simple, and at current counts it costs 1–21k triangles.
- **Alternatives:**
  - Hardware instancing (one cube mesh plus a per-instance buffer; needs HiDef and a shader). This is the
    next CPU bottleneck once on-screen items reach tens of thousands.
  - A scrolling texture for chevrons (needs a shader and UVs).
  - Retain meshes for arms and status, and move them by matrix.
- **Check:** each renderer's culling padding (2 tiles) must cover its tallest/widest part. `Detail` gates
  what is drawn at Mid and Coarse.

## 15. Wiring
`RenderStyle.cs`, `FactoryGame.cs`, `Input/CameraController.cs`, `Input/BuildController.cs`

```csharp
public Camera CreateCamera() => new OrbitCamera(minDistance: 5f, maxDistance: 320f);

public void RegisterSurface(SystemScheduler scheduler, Surface surface, BuildController controller)
{
    scheduler.Geometry.Lighting.Sun = Sun;
    var surfaceMeshes = new SurfaceMeshes(surface);
    meshes.Add(surfaceMeshes);
    scheduler.Add(surface.World, new StaticSurface3DRenderer(surfaceMeshes, Detail));   // static first
    scheduler.Add(surface.World, new BeltChevron3DRenderer(surface, Detail));           // then the live parts
    // … belt items, status, progress, launcher, inserter arms, ghost
}

public Matrix WorldTransform(Surface surface) =>
    Matrix.CreateRotationZ(surface.Frame.Rotation) * Matrix.CreateTranslation(surface.Frame.Position.X, surface.Frame.Position.Y, 0f);
```

- **Does:** one place owns the sun, camera and per-surface draw systems. A surface's frame (a rotation and a
  translation) is its world matrix, which is how ships turn with no rebuild.
- **Why:** the spike had four styles in one class. The pruned version holds only the 3D one.
- **Alternatives:** register systems in `FactoryGame` directly and drop the class (it is now thin).
- **Check:**
  - Draw order is registration order, and depth ties between coplanar faces depend on it.
  - `RenderStyle` and `SurfaceMeshes` must be disposed on exit.
  - The F4 retained/immediate switch the production commit mentions is gone (no `F4` in the code now), so
    there is no runtime fallback, though the retained path has now been run and renders correctly.

---

## Not 3D rendering (review separately or skip)

| Item | Note |
|---|---|
| `Proto4X.Factory/Simulation/**` (~5k lines) | Your snapshotted sim: belts, power, crafters, drills, inserters, launcher, exchange |
| `Proto4X.Factory/Headless/**` | 42 sim/determinism checks; the render tools were removed |
| `MonoGameLibrary/Events/EventStream.cs`, `IEventSystem.cs` | Double-buffered struct events; nothing in the 3D work references them |
| `WEB_RUNTIME.md`, `game-jam-countdown-ideas.md` | Exploration notes, unrelated |
| `.claude/settings.json` | Worktree isolation setting; probably leave it out |
| `ENGINE_LAYERING.md`, `Proto4X.Factory/DESIGN.md`, `ENGINE_REQUESTS.md` | Design notes |

---

## Update: cameras split (uncommitted)

§7 and §8 describe the earlier shape, where `OrbitCamera` extended the 2D `Camera`. That is now changed:
`CameraBase` (abstract: viewport, view/projection, `GroundToScreen`, `ScreenToGround`, `GetVisibleLocalBounds`)
with `Camera2D` (the old `Camera`, renamed) and `OrbitCamera` as siblings. A `DrawSpace.World` system under a
non-`Camera2D` camera now throws in `SystemScheduler.Draw`, instead of drawing meaninglessly.
