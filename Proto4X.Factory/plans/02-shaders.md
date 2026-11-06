# Plan 2 — Shader infrastructure

Independent of plans 1 and 3. It's split between the engine, which provides the plumbing, and the
Factory game, which provides the first three real shaders. The same branch rules as plan 1 apply,
with one commit per piece.

## Where the engine is today

- `GeometryBatch` draws every layer through two stock `BasicEffect`s (lit and unlit). There's no
  content pipeline step, which is a deliberate choice recorded in its remarks and in the Factory
  csproj.
- There's no render-target or post-process path. `Core.Draw` (sealed) runs `OnDraw` first, then the
  meta world's systems (the HUD).
- There's one vertex format, `MeshVertex`: position, normal, colour, texcoord (36 bytes).

## What "infrastructure for utilizing shaders" means here

There are three distinct ways a game uses a custom shader. The infrastructure should make each one
a few lines, and the prototype should exercise all three, so the plumbing is proven rather than
speculative:

| Path | What it is | First use in the prototype |
|---|---|---|
| **Material on geometry** | a custom vertex and pixel shader drawing triangles in the 3D pass, with depth | the *hologram* look for build ghosts and the frozen warehouse interior preview |
| **Full-screen background** | a pixel shader over a screen quad, drawn before the world | the procedural *starfield* behind the orbital view, with parallax |
| **Post-process** | the world rendered into a render target, then drawn to the screen through an effect | *colour grade + vignette*; the HUD stays sharp because it draws after |

## Decisions

### Compile shaders with the MonoGame content pipeline

Shaders must be compiled (HLSL → MGFX, cross-compiled to GLSL for DesktopGL). There is no runtime
compiler, so this **reverses the Factory's "no content pipeline" stance for effects only**. Textures
are still generated at runtime. The pipeline is pinned to the resolved runtime version
(`MonoGame.Framework.DesktopGL` 3.8.6, so `MonoGame.Content.Builder.Task` 3.8.6 and `dotnet-mgcb`
3.8.6 in the Game project's tool manifest). An effect compiled by a different MGFX version fails at
`Content.Load`, which build-verify would never see.

The `.fx` files live with the game (`Proto4X.Factory/Game/Content/Shaders/`), not the engine,
because they are art. The engine owns the C# side: binding, materials and passes.

### Engine API

```
Material (MonoGameLibrary/Graphics/Material.cs)
  Effect, BlendState, DepthStencilState, RasterizerState
  standard parameters bound by name if the effect declares them:
    World, View, Projection, WorldViewProjection, Time, SunDirection, CameraPosition
  Parameters: the Effect's own EffectParameterCollection for anything game-specific

GeometryBatch
  Draw(StaticMesh, transform, Material)        retained mesh through a custom material
  Custom(Material) : GeometryBuffer            per-frame immediate triangles for a material
  Time                                         seconds, set by the game each frame
  flush order: Opaque, Emissive, [custom opaque], Translucent, [custom blended], Additive

FullscreenPass (MonoGameLibrary/Graphics/FullscreenPass.cs)
  Draw(spriteBatch, effect, destination, source?)   one quad through a pixel shader
  shaders written against SpriteBatch's vertex output (POSITION, COLOR0, TEXCOORD0)

PostProcessor (MonoGameLibrary/Graphics/PostProcessor.cs)
  Begin(device)   → redirects drawing into a scene RenderTarget2D (with depth), resized to the backbuffer
  End(spriteBatch, effect?) → draws the scene to the backbuffer through the effect, or plain
```

**Why "bind by name"** (copied from Unity's `_Time`/`unity_ObjectToWorld` built-ins and MonoGame's
`IEffectMatrices`): a material doesn't have to know which pass draws it, and a shader that
declares `float Time;` just gets it. Lookups happen once, when the material is constructed, and are
cached as `EffectParameter` references. The per-draw cost is a matrix set plus an `Apply`, the same
as `BasicEffect` today, so the draw loop gets no new cost.

**Custom vertex shaders must take `MeshVertex` as declared** (POSITION0, NORMAL0, COLOR0,
TEXCOORD0). A mismatch fails only at draw time, so the semantics are documented on `MeshVertex` and
every shader starts from the same input struct.

### The three first shaders (game)

1. `Hologram.fx`: unlit, alpha-blended, depth-read. It combines rim brightening (1 − |N·V|),
   world-space scanlines scrolling with `Time`, and a slow pulse. The ghost renderer and the
   warehouse interior preview draw through it.
2. `Starfield.fx`: three hashed star layers, each offset by the camera's ground position times a
   per-layer parallax factor, twinkling with `Time`. It's drawn as the orbit view's background
   instead of a clear colour.
3. `Grade.fx`: post-process with a slight contrast/saturation curve and a vignette. It's toggled
   with F4 so the plain path stays testable.

## Verification

- Build-time: MGCB compiles all three. That catches syntax and semantic errors in HLSL, the only
  shader verification available without launching.
- **Not verifiable headless**: effect loading, parameter binding, vertex-layout match and visual
  output. The game is never launched here. Each shader is written so a failure falls back
  visibly: the ghost renderer keeps its BasicEffect path if the material fails to load, and the
  starfield falls back to the clear colour.

## Later, not now

Hot reload (watch `.fx`, rebuild with `mgcb`, `Content.Unload`), shadow maps, a second vertex format
(skinned or instanced), material sorting and batching once there are many materials, and bloom,
which needs a downsample chain.
