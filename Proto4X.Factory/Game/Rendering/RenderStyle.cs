using Microsoft.Xna.Framework;
using MonoGameLibrary.Systems.Infrastructure;
using MonoGameLibrary.UI;
using Proto4X.Factory.Input;
using Proto4X.Factory.Rendering.ThreeD;
using Proto4X.Factory.Simulation;
using System;
using System.Collections.Generic;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// Everything about how the factory looks, in one place: the sun, the camera, which draw systems
    /// each surface gets, and each surface's draw transform.
    /// </summary>
    /// <remarks>
    /// The block models drawn as lit geometry through an <see cref="OrbitCamera"/>, perspective or
    /// orthographic. World units are tiles, z up; a surface's frame is a rotation about z plus a
    /// translation, so a ship turns for free. Static content is cached per chunk
    /// (<see cref="SurfaceMeshes"/>); moving parts are rebuilt every frame.
    /// </remarks>
    public sealed class RenderStyle : IDisposable
    {
        /// <summary>Towards the sun: high, from the north-west (screen top-left at yaw 0), so shadows fall south-east.</summary>
        public static readonly Vector3 Sun = new(-0.45f, -0.6f, 1f);

        private readonly List<SurfaceMeshes> meshes = [];

        public Color Background => new(14, 16, 24);

        public Detail Detail { get; } = new();

        public OrbitCamera CreateCamera() => new OrbitCamera(minDistance: 5f, maxDistance: 320f);

        public void RegisterSurface(SystemScheduler scheduler, Surface surface, BuildController controller)
        {
            scheduler.Geometry.Lighting.Sun = Sun;

            // Static first: its chunks are the bulk of the opaque layer, and drawing them before the
            // small moving parts keeps depth ties resolving the way the per-system passes did.
            var surfaceMeshes = new SurfaceMeshes(surface);
            meshes.Add(surfaceMeshes);
            scheduler.Add(surface.World, new StaticSurface3DRenderer(surfaceMeshes, Detail));
            scheduler.Add(surface.World, new BeltChevron3DRenderer(surface, Detail));
            scheduler.Add(surface.World, new BeltItem3DRenderer(surface, Detail));
            scheduler.Add(surface.World, new BuildingStatus3DRenderer(surface, Detail));
            scheduler.Add(surface.World, new Progress3DRenderer(surface, Detail));
            scheduler.Add(surface.World, new Launcher3DRenderer(Detail));
            scheduler.Add(surface.World, new Inserter3DRenderer(surface, Detail));
            scheduler.Add(surface.World, new Ghost3DRenderer(surface, controller));
        }

        /// <summary>Grid-local tiles to world tiles: the frame's turn about z, then its position. No pixel scale in 3D.</summary>
        public Matrix WorldTransform(Surface surface) =>
            Matrix.CreateRotationZ(surface.Frame.Rotation) * Matrix.CreateTranslation(surface.Frame.Position.X, surface.Frame.Position.Y, 0f);

        public void PrepareFrame(OrbitCamera camera)
        {
            Detail.Distance = camera.Orthographic ? camera.Distance * 1.15f : camera.Distance;
        }

        public void Dispose()
        {
            foreach (var surfaceMeshes in meshes)
            {
                surfaceMeshes.Dispose();
            }
        }
    }
}
