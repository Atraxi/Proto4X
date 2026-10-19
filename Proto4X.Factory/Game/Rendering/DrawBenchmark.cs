using Microsoft.Xna.Framework;
using MonoGameLibrary.Systems.Infrastructure;
using MonoGameLibrary.UI;
using Proto4X.Factory.Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// CPU cost of building one frame's geometry for this style, at a sweep of zooms over both stress
    /// blocks (about 5,600 buildings): what the draw systems cost before anything reaches the GPU.
    /// </summary>
    /// <remarks>
    /// Measures <see cref="RenderStyle.PrepareFrame"/> plus <see cref="SystemScheduler.CollectGeometry"/>:
    /// medians after a warm-up pass, with the zoom order alternated between two measured passes (the
    /// Factory benchmark's rule - whichever runs first otherwise reads slow). Not measured: the GPU,
    /// and the per-frame vertex upload of DrawUserIndexedPrimitives. No style caches meshes, so these
    /// are relative numbers under one shared naive strategy, not a ceiling for any of them.
    /// </remarks>
    public static class DrawBenchmark
    {
        private static readonly float[] zooms = [1f, 0.5f, 0.25f, 0.1f];
        private static readonly Vector2 centre = new(130f, 72f);

        public static void Run(int iterations = 25)
        {
            var style = new RenderStyle();
            var scheduler = new SystemScheduler();
            var universe = FactoryScene.Create(scheduler, out var planet, out _);
            foreach (var surface in universe.Surfaces)
            {
                style.RegisterSurface(scheduler, surface, controller: null);
            }

            FactoryScene.StampStressBlock(planet, 0);
            FactoryScene.StampStressBlock(planet, 1);
            for (var t = 0; t < 600; t++)
            {
                universe.Step();
            }

            var camera = style.CreateCamera();
            camera.Resize(new Rectangle(0, 0, 1280, 720));

            Console.WriteLine($"Draw benchmark: style '{style.Name}', {planet.Buildings.AliveCount:N0} buildings, 1280x720, median of {iterations * 2}");
            Console.WriteLine($"Atlas: {style.Atlas.Image.Width}x{style.Atlas.Image.Height}, {style.Atlas.FilledFraction:P0} of its height used");
            Console.WriteLine();

            // Warm-up: every zoom once, so tiered JIT has promoted every draw system.
            foreach (var zoom in zooms)
            {
                Measure(style, scheduler, camera, planet, zoom, iterations: 5);
            }

            var results = new Dictionary<float, List<Sample>>();
            foreach (var pass in new[] { zooms, zooms.Reverse().ToArray() })
            {
                foreach (var zoom in pass)
                {
                    if (!results.TryGetValue(zoom, out var list))
                    {
                        results[zoom] = list = [];
                    }

                    list.AddRange(Measure(style, scheduler, camera, planet, zoom, iterations));
                }
            }

            Console.WriteLine("| zoom | triangles | collect ms (median) | alloc KB/frame | tiles culled in | tiles on screen | culled-in / on-screen |");
            Console.WriteLine("|---:|---:|---:|---:|---:|---:|---:|");
            foreach (var zoom in zooms)
            {
                var samples = results[zoom];
                var milliseconds = samples.Select(s => s.Milliseconds).OrderBy(m => m).ToArray();
                var (culled, onScreen) = CountTiles(style, camera, planet, zoom);
                Console.WriteLine($"| {zoom:0.##} | {samples[0].Triangles:N0} | {milliseconds[milliseconds.Length / 2]:F2} | {samples.Average(s => s.AllocatedBytes) / 1024.0:F1} | {culled:N0} | {onScreen:N0} | {culled / (double)Math.Max(1, onScreen):F2} |");
            }
        }

        private readonly record struct Sample(double Milliseconds, long AllocatedBytes, int Triangles);

        private static List<Sample> Measure(RenderStyle style, SystemScheduler scheduler, Camera camera, Surface planet, float zoom, int iterations)
        {
            style.AimCamera(camera, planet, centre, zoom, 0f);
            var world = style.WorldTransform(planet);
            var samples = new List<Sample>(iterations);
            var timer = new Stopwatch();
            for (var i = 0; i < iterations; i++)
            {
                var before = GC.GetAllocatedBytesForCurrentThread();
                timer.Restart();
                style.PrepareFrame(camera, planet);
                var bounds = camera.GetVisibleLocalBounds(world);
                scheduler.Geometry.Clear();
                scheduler.CollectGeometry(planet.World, scheduler.Geometry, bounds);
                timer.Stop();
                samples.Add(new Sample(timer.Elapsed.TotalMilliseconds, GC.GetAllocatedBytesForCurrentThread() - before, scheduler.Geometry.TriangleCount));
            }

            return samples;
        }

        /// <summary>
        /// Tiles inside the culling box (what a ground pass visits) against tiles whose centre is
        /// actually on screen: how much a style's axis-aligned culling over-fetches.
        /// </summary>
        private static (int Culled, int OnScreen) CountTiles(RenderStyle style, Camera camera, Surface planet, float zoom)
        {
            style.AimCamera(camera, planet, centre, zoom, 0f);
            var world = style.WorldTransform(planet);
            var visible = SurfaceView.VisibleTiles(camera.GetVisibleLocalBounds(world), planet.Grid, padding: 0);
            var culled = 0;
            var onScreen = 0;
            for (var y = visible.MinY; y <= visible.MaxY; y++)
            {
                for (var x = visible.MinX; x <= visible.MaxX; x++)
                {
                    culled++;
                    var point = Vector3.Transform(new Vector3(x + 0.5f, y + 0.5f, 0f), world);
                    var screen = camera.WorldToScreen(new Vector2(point.X, point.Y));
                    if (camera.Viewport.Contains(screen))
                    {
                        onScreen++;
                    }
                }
            }

            return (culled, onScreen);
        }
    }
}
