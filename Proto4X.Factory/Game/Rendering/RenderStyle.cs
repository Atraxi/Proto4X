using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Systems.Infrastructure;
using MonoGameLibrary.UI;
using Proto4X.Factory.Input;
using Proto4X.Factory.Rendering.Software;
using Proto4X.Factory.Rendering.ThreeD;
using Proto4X.Factory.Simulation;
using System;
using System.Collections.Generic;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// Everything about how the factory looks, in one place: the atlas, the sun, which draw systems
    /// each surface gets, each surface's draw transform, and how a camera is aimed. The game and
    /// the headless tools both go through this, so a snapshot is the game's real rendering, minus
    /// the HUD.
    /// </summary>
    /// <remarks>
    /// The block models drawn as lit geometry through an <see cref="OrbitCamera"/>, perspective or
    /// orthographic. World units are tiles, z up; a surface's frame is a rotation about z plus a
    /// translation, so a ship turns for free. Static content is cached per chunk
    /// (<see cref="SurfaceMeshes"/>); moving parts are rebuilt every frame.
    /// </remarks>
    public sealed class RenderStyle : IDisposable
    {
        /// <summary>2D zoom 1 shows 22.5 tiles vertically at 720 px; this is the distance that frames the same at 40 degrees.</summary>
        private const float DistanceAtZoom1 = 30.9f;

        /// <summary>Towards the sun: high, from the north-west (screen top-left at yaw 0), so shadows fall south-east.</summary>
        public static readonly Vector3 Sun = new(-0.45f, -0.6f, 1f);

        private readonly List<StaticSurface3DRenderer> staticRenderers = [];
        private SystemScheduler? scheduler;
        private bool retained = true;

        public RenderStyle()
        {
            Atlas = new AtlasBuilder(256);
        }

        public string Name => "blocky3d";

        public AtlasBuilder Atlas { get; }

        public bool LinearSampling => false;

        public Color Background => new(14, 16, 24);

        public Detail Detail { get; } = new();

        /// <summary>Every surface's static renderer, for tools that inspect or reconfigure the chunk caches.</summary>
        public IReadOnlyList<StaticSurface3DRenderer> StaticRenderers => staticRenderers;

        /// <summary>
        /// True: chunk meshes are cached and drawn from GPU buffers. False: every visible chunk is
        /// rebuilt each frame and streamed from the CPU, the way the spike drew everything. The
        /// false path exercises none of the vertex-buffer code, which is what makes it a useful
        /// switch if the screen is ever blank.
        /// </summary>
        public bool Retained
        {
            get => retained;
            set
            {
                retained = value;
                foreach (var renderer in staticRenderers)
                {
                    renderer.Retained = value;
                }

                if (scheduler is not null)
                {
                    scheduler.Geometry.UseVertexBuffers = value;
                }
            }
        }

        public Camera CreateCamera() => new OrbitCamera(minDistance: 5f, maxDistance: 320f);

        public void RegisterSurface(SystemScheduler scheduler, Surface surface, BuildController? controller)
        {
            this.scheduler = scheduler;
            var geometry = scheduler.Geometry;
            geometry.Immediate.WhiteUv = Atlas.WhiteUv;
            geometry.Sampler = SamplerState.PointClamp;
            geometry.Lighting.Sun = Sun;
            geometry.UseVertexBuffers = retained;

            // Static first: its chunks are the bulk of the opaque layer, and drawing them before the
            // small moving parts keeps depth ties resolving the way the per-system passes did.
            var statics = new StaticSurface3DRenderer(new SurfaceMeshes(surface, Atlas.WhiteUv), Detail) { Retained = retained };
            staticRenderers.Add(statics);
            scheduler.Add(surface.World, statics);
            scheduler.Add(surface.World, new BeltChevron3DRenderer(surface, Detail));
            scheduler.Add(surface.World, new BeltItem3DRenderer(surface, Detail));
            scheduler.Add(surface.World, new BuildingStatus3DRenderer(surface, Detail));
            scheduler.Add(surface.World, new Progress3DRenderer(surface, Detail));
            scheduler.Add(surface.World, new Launcher3DRenderer(Detail));
            scheduler.Add(surface.World, new Inserter3DRenderer(surface, Detail));
            if (controller is not null)
            {
                scheduler.Add(surface.World, new Ghost3DRenderer(surface, controller));
            }
        }

        /// <summary>Grid-local tiles to world tiles: the frame's turn about z, then its position. No pixel scale in 3D.</summary>
        public Matrix WorldTransform(Surface surface) =>
            Matrix.CreateRotationZ(surface.Frame.Rotation) * Matrix.CreateTranslation(surface.Frame.Position.X, surface.Frame.Position.Y, 0f);

        public void PrepareFrame(Camera camera, Surface surface)
        {
            if (camera is OrbitCamera orbit)
            {
                Detail.Distance = orbit.Orthographic ? orbit.Distance * 1.15f : orbit.Distance;
            }
        }

        /// <summary>The grid tile under a screen point: a ray to the ground, then into the surface's frame.</summary>
        public Vector2 ScreenToLocal(Camera camera, Surface surface, Vector2 screen) =>
            surface.Frame.WorldToLocal(camera.ScreenToWorld(screen));

        /// <summary>
        /// Looks at <paramref name="localTile"/> from the default pitch, at the distance that frames
        /// what a 2D camera at <paramref name="zoom"/> would, with the grid turned
        /// <paramref name="screenRotation"/> on screen.
        /// </summary>
        public void AimCamera(Camera camera, Surface surface, Vector2 localTile, float zoom, float screenRotation)
        {
            var orbit = (OrbitCamera)camera;
            var target = surface.Frame.LocalToWorld(localTile);
            orbit.Target = new Vector3(target, 0f);
            orbit.Distance = DistanceAtZoom1 / zoom;
            orbit.Yaw = surface.Frame.Rotation - screenRotation;
        }

        /// <summary>Snapshot extras: the same scene from angles only a 3D style can offer.</summary>
        public void AimSpecial(Camera camera, string preset)
        {
            var orbit = (OrbitCamera)camera;
            switch (preset)
            {
                case "low":
                    orbit.Pitch = 0.5f;
                    orbit.Yaw += 0.6f;
                    break;
                case "top":
                    orbit.Orthographic = false;
                    orbit.Pitch = 1.3f;
                    break;
                case "overhead":
                    // The top-down look from the same models: orthographic, as steep as the clamp allows.
                    orbit.Orthographic = true;
                    orbit.Pitch = orbit.MaxPitch;
                    break;
                case "iso":
                    // True isometric: 45 degrees round, arctan(1/sqrt 2) up, orthographic.
                    orbit.Orthographic = true;
                    orbit.Yaw = -MathF.PI / 4f;
                    orbit.Pitch = MathF.Atan(1f / MathF.Sqrt(2f));
                    break;
                default:
                    orbit.Orthographic = false;
                    orbit.Pitch = 0.95f;
                    break;
            }
        }

        public void Dispose()
        {
            foreach (var renderer in staticRenderers)
            {
                renderer.Meshes.Dispose();
            }
        }
    }
}
