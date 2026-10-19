using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Systems.Infrastructure;
using MonoGameLibrary.UI;
using Proto4X.Factory.Input;
using Proto4X.Factory.Rendering.Software;
using Proto4X.Factory.Rendering.ThreeD;
using Proto4X.Factory.Simulation;
using System;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// Everything about how the factory looks, in one place: the atlas, which draw systems each
    /// surface gets, each surface's draw transform, and how a camera is aimed. The game and the
    /// headless snapshots both go through this, so a snapshot is the game's real rendering, minus
    /// the HUD.
    /// </summary>
    /// <remarks>
    /// <b>Blocky 3D</b> (the shapez 2 / Dyson Sphere Program family): the block models drawn
    /// directly as lit geometry through an <see cref="OrbitCamera"/>, perspective or orthographic.
    /// World units are tiles, z up; a surface's frame is a rotation about z plus a translation, so a
    /// ship turns for free.
    /// </remarks>
    public sealed class RenderStyle
    {
        /// <summary>2D zoom 1 shows 22.5 tiles vertically at 720 px; this is the distance that frames the same at 40 degrees.</summary>
        private const float DistanceAtZoom1 = 30.9f;

        public RenderStyle()
        {
            Atlas = new AtlasBuilder(256);
        }

        public string Name => "blocky3d";

        public AtlasBuilder Atlas { get; }

        public bool LinearSampling => false;

        public Color Background => new(14, 16, 24);

        public Detail Detail { get; } = new();

        public Camera CreateCamera() => new OrbitCamera(minDistance: 5f, maxDistance: 320f);

        public void RegisterSurface(SystemScheduler scheduler, Surface surface, BuildController? controller)
        {
            scheduler.Geometry.WhiteUv = Atlas.WhiteUv;
            scheduler.Geometry.Sampler = SamplerState.PointClamp;

            scheduler.Add(surface.World, new Terrain3DRenderer(surface, Detail));
            scheduler.Add(surface.World, new Belt3DRenderer(surface, Detail));
            scheduler.Add(surface.World, new BeltItem3DRenderer(surface, Detail));
            scheduler.Add(surface.World, new Building3DRenderer(surface, Detail));
            scheduler.Add(surface.World, new Progress3DRenderer(surface, Detail));
            scheduler.Add(surface.World, new Launcher3DRenderer(Detail));
            scheduler.Add(surface.World, new Inserter3DRenderer(surface, Detail));
            if (controller is not null)
            {
                scheduler.Add(surface.World, new Ghost3DRenderer(surface, controller));
            }
        }

        public ViewState View { get; } = new();

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
    }
}
