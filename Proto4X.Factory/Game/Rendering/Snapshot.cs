using Microsoft.Xna.Framework;
using MonoGameLibrary.Systems.Infrastructure;
using MonoGameLibrary.UI;
using Proto4X.Factory.Rendering.Software;
using Proto4X.Factory.Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// Renders the game's own draw systems to PNG without a window: the scene runs headless, each
    /// shot's camera is aimed, the scheduler collects the geometry pass, and the software rasterizer
    /// draws it. No HUD (it's SpriteBatch), otherwise what the GPU would get.
    /// </summary>
    public static class Snapshot
    {
        /// <summary>One picture: which surface, where, how close, and how far the grid is turned from upright.</summary>
        public readonly record struct Shot(string Name, int Surface, Vector2 LocalTile, float Zoom, float ScreenRotation, bool WithStressBlock = false, string Preset = "");

        public static readonly Shot[] Shots =
        [
            new("factory", 0, new Vector2(15f, 8.5f), 1.25f, 0f),
            new("closeup", 0, new Vector2(10f, 3.5f), 3.5f, 0f),
            new("ship", 1, new Vector2(11.5f, 8f), 1.9f, 0.55f),
            new("overview", 0, new Vector2(118f, 40f), 0.2f, 0f, WithStressBlock: true, Preset: "top"),
            new("low-angle", 0, new Vector2(13f, 6f), 1.6f, 0f, Preset: "low"),
            new("iso-ortho", 0, new Vector2(15f, 8.5f), 1.25f, 0f, Preset: "iso"),
            new("overhead-ortho", 0, new Vector2(15f, 8.5f), 1.25f, 0f, Preset: "overhead"),
        ];

        /// <param name="supersample">1 matches the game, which has no MSAA and no mipmaps.</param>
        public static void CaptureAll(string directory, int width = 1280, int height = 720, int supersample = 1, int ticks = 900)
        {
            Directory.CreateDirectory(directory);
            using var scene = new Scene(ticks);
            var camera = scene.Style.CreateCamera();
            camera.Resize(new Rectangle(0, 0, width, height));
            var rasterizer = new SoftwareRasterizer(width * supersample, height * supersample);
            var report = new List<string>();

            foreach (var shot in Shots)
            {
                scene.PrepareFor(shot);
                var timer = Stopwatch.StartNew();
                var image = scene.Render(shot, camera, rasterizer, supersample);
                var path = Path.Combine(directory, $"{scene.Style.Name}-{shot.Name}.png");
                PngWriter.Write(path, image);

                var line = $"{shot.Name,-14} {scene.Scheduler.Geometry.TriangleCount,8:N0} triangles  {scene.Scheduler.Geometry.MeshDraws.Length,4} meshes  render {timer.Elapsed.TotalMilliseconds,7:F1} ms  -> {path}";
                report.Add(line);
                Console.WriteLine(line);
            }

            File.WriteAllLines(Path.Combine(directory, $"{scene.Style.Name}-report.txt"), report);
        }

        /// <summary>
        /// The opening scene with the style registered, stepped and ready to photograph: shared by
        /// the snapshots, the render checks and the draw benchmark.
        /// </summary>
        public sealed class Scene : IDisposable
        {
            private bool stressStamped;

            public Scene(int ticks = 900)
            {
                Universe = FactoryScene.Create(Scheduler, out var planet, out var ship);
                Planet = planet;
                Ship = ship;
                foreach (var surface in Universe.Surfaces)
                {
                    Style.RegisterSurface(Scheduler, surface, controller: null);
                }

                // Headless pictures must show the current state, never a mesh waiting its turn.
                foreach (var renderer in Style.StaticRenderers)
                {
                    renderer.RebuildBudget = int.MaxValue;
                }

                Step(ticks);
            }

            public RenderStyle Style { get; } = new();
            public SystemScheduler Scheduler { get; } = new();
            public Universe Universe { get; }
            public Surface Planet { get; }
            public Surface Ship { get; }

            public void Step(int ticks)
            {
                for (var t = 0; t < ticks; t++)
                {
                    Universe.Step();
                }
            }

            /// <summary>Stamps the first stress block (once) for shots that ask for it.</summary>
            public void PrepareFor(Shot shot)
            {
                if (shot.WithStressBlock && !stressStamped)
                {
                    FactoryScene.StampStressBlock(Planet, 0);
                    Step(600);
                    stressStamped = true;
                }
            }

            /// <summary>Aims the camera for the shot and collects its geometry into the scheduler's batch.</summary>
            public Matrix Collect(Shot shot, Camera camera)
            {
                var surface = Universe.Surfaces[shot.Surface];
                Style.AimCamera(camera, surface, shot.LocalTile, shot.Zoom, shot.ScreenRotation);
                Style.AimSpecial(camera, shot.Preset);
                Style.PrepareFrame(camera, surface);

                var world = Style.WorldTransform(surface);
                Scheduler.Geometry.Clear();
                Scheduler.CollectGeometry(surface.World, Scheduler.Geometry, Scheduler.CreateGeometryContext(camera, world));
                return world;
            }

            public CpuImage Render(Shot shot, Camera camera, SoftwareRasterizer rasterizer, int supersample = 1)
            {
                var world = Collect(shot, camera);

                // The projection maps the camera's window pixels to clip space, so a rasterizer
                // with more pixels than the window simply supersamples.
                rasterizer.Clear(Style.Background);
                rasterizer.Draw(Scheduler.Geometry, world, camera.GetViewMatrix(), camera.GetProjectionMatrix(), Style.Atlas.Image, Style.LinearSampling);
                return rasterizer.Resolve(supersample);
            }

            public void Dispose() => Style.Dispose();
        }
    }
}
