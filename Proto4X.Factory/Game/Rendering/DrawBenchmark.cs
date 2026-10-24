using Microsoft.Xna.Framework;
using MonoGameLibrary.UI;
using Proto4X.Factory.Rendering.ThreeD;
using Proto4X.Factory.Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// CPU cost of a frame's geometry pass over both stress blocks (about 5,700 buildings), at a
    /// sweep of zooms: steady state with the chunk cache warm, the same frame with the cache off
    /// (every visible chunk rebuilt - the spike's behaviour), and the one-off cost of building
    /// every chunk from cold.
    /// </summary>
    /// <remarks>
    /// Measures <see cref="RenderStyle.PrepareFrame"/> plus the scheduler's geometry collection:
    /// medians after a warm-up pass, with the zoom order alternated between two measured passes.
    /// Not measured: the GPU, the upload of a rebuilt mesh into its vertex buffer, and the immediate
    /// geometry's per-frame upload.
    /// </remarks>
    public static class DrawBenchmark
    {
        private static readonly float[] zooms = [1f, 0.5f, 0.25f, 0.1f];
        private static readonly Vector2 centre = new(130f, 72f);

        public static void Run(int iterations = 25)
        {
            using var scene = new Snapshot.Scene(ticks: 0);
            FactoryScene.StampStressBlock(scene.Planet, 0);
            FactoryScene.StampStressBlock(scene.Planet, 1);
            scene.Step(600);

            var camera = scene.Style.CreateCamera();
            camera.Resize(new Rectangle(0, 0, 1280, 720));
            var meshes = scene.Style.StaticRenderers[0].Meshes;

            Console.WriteLine($"Draw benchmark: {scene.Planet.Buildings.AliveCount:N0} buildings, 1280x720, median of {iterations * 2}");
            Console.WriteLine();
            ColdBuild(scene, meshes);

            foreach (var retained in new[] { true, false })
            {
                scene.Style.Retained = retained;
                foreach (var zoom in zooms)
                {
                    Measure(scene, camera, zoom, iterations: 5);
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

                        list.AddRange(Measure(scene, camera, zoom, iterations));
                    }
                }

                Console.WriteLine(retained ? "Cache on (steady state):" : "Cache off (every visible chunk rebuilt each frame):");
                Console.WriteLine("| zoom | tier | triangles | of which cached | meshes drawn | collect ms (median) | alloc KB/frame |");
                Console.WriteLine("|---:|---|---:|---:|---:|---:|---:|");
                foreach (var zoom in zooms)
                {
                    var samples = results[zoom];
                    var milliseconds = samples.Select(s => s.Milliseconds).OrderBy(m => m).ToArray();
                    var s0 = samples[0];
                    Console.WriteLine($"| {zoom:0.##} | {s0.Tier} | {s0.Triangles:N0} | {s0.CachedTriangles:N0} | {s0.Meshes} | {milliseconds[milliseconds.Length / 2]:F2} | {samples.Average(s => s.AllocatedBytes) / 1024.0:F1} |");
                }
                Console.WriteLine();
            }

            scene.Style.Retained = true;
            Console.WriteLine($"Chunk meshes held: {meshes.CpuBytes / (1024.0 * 1024.0):F1} MB on the CPU (as much again on the GPU once uploaded)");
        }

        /// <summary>Every chunk of the planet built at every tier, from nothing: the worst case after a load or a full invalidation.</summary>
        private static void ColdBuild(Snapshot.Scene scene, SurfaceMeshes meshes)
        {
            Console.WriteLine("Cold build, every planet chunk:");
            Console.WriteLine("| mesh | chunks | total ms | worst chunk ms | triangles |");
            Console.WriteLine("|---|---:|---:|---:|---:|");
            foreach (var tier in new[] { DetailTier.Fine, DetailTier.Mid, DetailTier.Coarse })
            {
                Row(tier.ToString(), (int chunk, ref int budget) => meshes.Body(chunk, tier, ref budget, force: true).TriangleCount);
            }
            Row("Shadows", (int chunk, ref int budget) => meshes.Shadows(chunk, ref budget, force: true).TriangleCount);
            Console.WriteLine();

            void Row(string name, BuildChunk build)
            {
                // Twice, keeping the second: the first pays for JIT and for growing the arrays.
                var total = 0.0;
                var worst = 0.0;
                var triangles = 0;
                for (var round = 0; round < 2; round++)
                {
                    total = 0;
                    worst = 0;
                    triangles = 0;
                    var budget = int.MaxValue;
                    for (var chunk = 0; chunk < meshes.ChunkCount; chunk++)
                    {
                        var timer = Stopwatch.StartNew();
                        triangles += build(chunk, ref budget);
                        var ms = timer.Elapsed.TotalMilliseconds;
                        total += ms;
                        worst = Math.Max(worst, ms);
                    }
                }

                Console.WriteLine($"| {name} | {meshes.ChunkCount} | {total:F1} | {worst:F2} | {triangles:N0} |");
            }
        }

        private delegate int BuildChunk(int chunk, ref int budget);

        private readonly record struct Sample(double Milliseconds, long AllocatedBytes, int Triangles, int CachedTriangles, int Meshes, DetailTier Tier);

        private static List<Sample> Measure(Snapshot.Scene scene, Camera camera, float zoom, int iterations)
        {
            var planet = scene.Planet;
            var style = scene.Style;
            var scheduler = scene.Scheduler;
            style.AimCamera(camera, planet, centre, zoom, 0f);
            var world = style.WorldTransform(planet);
            var samples = new List<Sample>(iterations);
            var timer = new Stopwatch();
            for (var i = 0; i < iterations; i++)
            {
                var before = GC.GetAllocatedBytesForCurrentThread();
                timer.Restart();
                style.PrepareFrame(camera, planet);
                scheduler.Geometry.Clear();
                scheduler.CollectGeometry(planet.World, scheduler.Geometry, scheduler.CreateGeometryContext(camera, world));
                timer.Stop();

                var batch = scheduler.Geometry;
                var cached = 0;
                foreach (var draw in batch.MeshDraws)
                {
                    cached += draw.Mesh.TriangleCount;
                }

                samples.Add(new Sample(timer.Elapsed.TotalMilliseconds, GC.GetAllocatedBytesForCurrentThread() - before, batch.TriangleCount, cached, batch.MeshDraws.Length, style.Detail.Tier));
            }

            return samples;
        }
    }
}
