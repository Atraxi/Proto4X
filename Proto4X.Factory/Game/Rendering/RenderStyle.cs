using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Systems.Infrastructure;
using MonoGameLibrary.UI;
using Proto4X.Factory.Input;
using Proto4X.Factory.Rendering.Software;
using Proto4X.Factory.Simulation;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// Everything about how the factory looks, in one place: the atlas, which draw systems each
    /// surface gets, each surface's draw transform, and how a camera is aimed. The game and the
    /// headless snapshots both go through this, so a snapshot is the game's real rendering, minus
    /// the HUD.
    /// </summary>
    /// <remarks>
    /// This is the seam the art-style variants replace. This one is the original flat top-down
    /// look, ported from SpriteBatch onto the geometry pass unchanged.
    /// </remarks>
    public sealed class RenderStyle
    {
        public RenderStyle()
        {
            Atlas = new AtlasBuilder(256);
        }

        public string Name => "flat";

        /// <summary>Every texture the style draws from, built on the CPU at startup.</summary>
        public AtlasBuilder Atlas { get; }

        public bool LinearSampling => false;

        public Color Background => Palette.Background;

        public Camera CreateCamera() => new(minZoom: 0.1f, maxZoom: 4f);

        /// <summary>Registration order is draw order within a layer of equal depth.</summary>
        public void RegisterSurface(SystemScheduler scheduler, Surface surface, BuildController? controller)
        {
            scheduler.Geometry.WhiteUv = Atlas.WhiteUv;
            scheduler.Geometry.Sampler = LinearSampling ? SamplerState.LinearClamp : SamplerState.PointClamp;

            scheduler.Add(surface.World, new TerrainRenderer(surface));
            scheduler.Add(surface.World, new BeltRenderer(surface));
            scheduler.Add(surface.World, new BeltItemRenderer(surface));
            scheduler.Add(surface.World, new BuildingRenderer(surface));
            scheduler.Add(surface.World, new CrafterProgressRenderer(surface));
            scheduler.Add(surface.World, new LauncherProgressRenderer());
            scheduler.Add(surface.World, new InserterRenderer(surface));
            if (controller is not null)
            {
                scheduler.Add(surface.World, new GhostRenderer(surface, controller));
            }
        }

        /// <summary>What this frame's camera and surface look like, for renderers that need it (sprite styles keep billboards upright with it).</summary>
        public ViewState View { get; } = new();

        public Matrix WorldTransform(Surface surface) => SurfaceView.WorldTransform(surface.Frame);

        /// <summary>Called once per frame before the surface is drawn, by the game and by snapshots.</summary>
        public void PrepareFrame(Camera camera, Surface surface)
        {
            View.Update(camera, WorldTransform(surface));
        }

        /// <summary>
        /// Puts <paramref name="localTile"/> of <paramref name="surface"/> at the screen centre, at
        /// <paramref name="zoom"/>, with the surface's grid turned <paramref name="screenRotation"/>
        /// from upright. Used by snapshots; the game drives the camera through CameraController.
        /// </summary>
        public void AimCamera(Camera camera, Surface surface, Vector2 localTile, float zoom, float screenRotation)
        {
            var world = SurfaceView.TileToWorld(surface.Frame, localTile);
            camera.ScreenTransform = Matrix.CreateTranslation(-world.X, -world.Y, 0) *
                Matrix.CreateRotationZ(screenRotation - surface.Frame.Rotation) *
                Matrix.CreateScale(zoom, zoom, 1) *
                Matrix.CreateTranslation(camera.Viewport.Width / 2f, camera.Viewport.Height / 2f, 0);
        }
    }
}
