using Microsoft.Xna.Framework;
using MonoGameLibrary.UI;
using Proto4X.Factory.Rendering.Software;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// Headless proof that the chunk-mesh cache is invisible: what it draws must match, pixel for
    /// pixel, what rebuilding everything from scratch draws - after warm-up, with culling off, and
    /// after every kind of change the cache has to notice. Plus a check that the sun stays put
    /// while a ship turns under it.
    /// </summary>
    public static class RenderCheck
    {
        private const int Width = 640;
        private const int Height = 360;

        /// <summary>The standard shots, plus close-ups of the places the mutations below touch.</summary>
        private static readonly Snapshot.Shot[] shots =
        [
            .. Snapshot.Shots.Where(s => !s.WithStressBlock),
            new("ore-field", 0, new Vector2(8f, 22f), 1.6f, 0f),
            new("chunk-edge", 0, new Vector2(32f, 20f), 1.6f, 0.4f),
            new("overview", 0, new Vector2(118f, 40f), 0.2f, 0f, Preset: "top"),
            new("mid", 0, new Vector2(60f, 30f), 0.35f, 0.3f),
        ];

        public static int Run(string? failureDirectory = null)
        {
            using var scene = new Snapshot.Scene();
            var camera = scene.Style.CreateCamera();
            camera.Resize(new Rectangle(0, 0, Width, Height));
            var rasterizer = new SoftwareRasterizer(Width, Height);
            var failures = 0;

            void Check(string stage)
            {
                DiscardRebuildCounts(scene.Style);
                foreach (var shot in shots)
                {
                    // Cached first: a forced rebuild refreshes the cache, which would make the
                    // cached picture trivially right.
                    var cached = scene.Render(shot, camera, rasterizer);
                    var rebuilds = scene.Style.StaticRenderers.Sum(r => r.Meshes.TakeRebuildCount());

                    scene.Style.Retained = false;
                    var fresh = scene.Render(shot, camera, rasterizer);
                    scene.Style.Retained = true;
                    DiscardRebuildCounts(scene.Style);

                    // Cached-versus-rebuilt can't see a bug both share. One that bit once: geometry
                    // sampling a transparent texel draws see-through holes over an opaque clear. (254 is
                    // ordinary: the texture sample rounds a hair under one.)
                    var holes = Array.FindAll(cached.Pixels, p => p.A < 250).Length;
                    var differing = CountDifferences(cached, fresh, out _);
                    Report(differing == 0 && holes == 0, $"{stage,-26} {shot.Name,-14} cached = rebuilt ({rebuilds} chunk meshes rebuilt){(differing > 0 ? $": {differing} pixels differ" : "")}{(holes > 0 ? $", {holes} transparent pixels" : "")}");
                    if (differing > 0 || holes > 0)
                    {
                        failures++;
                        Save(failureDirectory, $"{stage}-{shot.Name}", cached, fresh);
                    }
                }
            }

            void CheckCulling()
            {
                foreach (var shot in shots)
                {
                    var culled = scene.Render(shot, camera, rasterizer);
                    var meshes = scene.Scheduler.Geometry.MeshDraws.Length;
                    SetCulling(scene.Style, false);
                    var all = scene.Render(shot, camera, rasterizer);
                    var allMeshes = scene.Scheduler.Geometry.MeshDraws.Length;
                    SetCulling(scene.Style, true);

                    var differing = CountDifferences(culled, all, out _);
                    Report(differing == 0, $"{"culling",-26} {shot.Name,-14} culled = unculled ({meshes} of {allMeshes} meshes submitted){(differing > 0 ? $": {differing} pixels differ" : "")}");
                    if (differing > 0)
                    {
                        failures++;
                        Save(failureDirectory, $"culling-{shot.Name}", culled, all);
                    }
                }
            }

            void Report(bool ok, string line) => Console.WriteLine($"{(ok ? "ok  " : "FAIL")} {line}");

            // 1. Built once, drawn again: nothing should rebuild, nothing should differ.
            foreach (var shot in shots)
            {
                scene.Render(shot, camera, rasterizer);
            }
            Check("warm cache");
            CheckCulling();

            // 2. Ore mined out: only the last unit changes the picture.
            var grid = scene.Planet.Grid;
            for (var x = 5; x <= 7; x++)
            {
                while (grid.MineOne(x, 21) != ItemId.None)
                {
                }
            }
            Check("ore mined out");

            // 3. Removals: freed tiles drop out of the chunk mesh at once; the building is drawn
            // see-through by the live pass until the next tick tears it down.
            var removed = 0;
            for (var y = 1; y <= 7 && removed < 6; y++)
            {
                for (var x = 6; x <= 16 && removed < 6; x++)
                {
                    if (scene.Planet.TryRemoveAt(x, y))
                    {
                        removed++;
                    }
                }
            }
            Check($"{removed} removed, pending");
            scene.Step(2);
            Check($"{removed} removed, torn down");

            // 4. Belts across a chunk edge with a curve: the curve marker depends on network shapes,
            // which only update at the next tick - so the mesh built now is provisional.
            for (var x = 29; x <= 35; x++)
            {
                scene.Planet.TryPlace(BuildingKind.Belt, x, 20, Direction.East);
            }
            for (var y = 20; y <= 23; y++)
            {
                scene.Planet.TryPlace(BuildingKind.Belt, 36, y, Direction.South);
            }
            Check("belts placed, before tick");
            scene.Step(1);
            Check("belts placed, after tick");

            // 5. A whole stress block at once.
            FactoryScene.StampStressBlock(scene.Planet, 0);
            scene.Step(120);
            Check("stress block");
            CheckCulling();

            // 6. Time passing: items move, machines craft. Only ore depletion should rebuild anything.
            scene.Step(300);
            Check("300 ticks later");

            failures += CheckSunStaysPut(scene, camera, rasterizer, failureDirectory);

            Console.WriteLine();
            Console.WriteLine(failures == 0 ? "render checks passed" : $"{failures} render checks FAILED");
            return failures;
        }

        /// <summary>
        /// Turns the ship a quarter turn with the camera locked to it, so the deck looks the same on
        /// screen. If lighting were baked in the ship's own space (the spike), the two pictures would
        /// be identical; with the sun fixed in the sky, the lit faces must change.
        /// </summary>
        private static int CheckSunStaysPut(Snapshot.Scene scene, Camera camera, SoftwareRasterizer rasterizer, string? failureDirectory)
        {
            var ship = scene.Ship;
            var saved = ship.Frame;
            var shot = new Snapshot.Shot("ship-turn", 1, new Vector2(11.5f, 8f), 1.9f, 0f);

            ship.Frame.Rotation = 0f;
            var before = scene.Render(shot, camera, rasterizer);
            ship.Frame.Rotation = MathF.PI / 2f;
            var after = scene.Render(shot, camera, rasterizer);
            ship.Frame = saved;

            var differing = CountDifferences(before, after, out _);
            var ok = differing > 0;
            Console.WriteLine($"{(ok ? "ok  " : "FAIL")} {"sun fixed in the sky",-26} ship turned 90 degrees under a locked camera: {differing:N0} pixels relit");
            Save(failureDirectory, "ship-turn", before, after);
            return ok ? 0 : 1;
        }

        private static void DiscardRebuildCounts(RenderStyle style)
        {
            foreach (var renderer in style.StaticRenderers)
            {
                renderer.Meshes.TakeRebuildCount();
            }
        }

        private static void SetCulling(RenderStyle style, bool on)
        {
            foreach (var renderer in style.StaticRenderers)
            {
                renderer.CullChunks = on;
            }
        }

        public static int CountDifferences(CpuImage a, CpuImage b, out int maxChannelDifference)
        {
            var differing = 0;
            maxChannelDifference = 0;
            for (var i = 0; i < a.Pixels.Length; i++)
            {
                var p = a.Pixels[i];
                var q = b.Pixels[i];
                if (p != q)
                {
                    differing++;
                    maxChannelDifference = Math.Max(maxChannelDifference,
                        Math.Max(Math.Abs(p.R - q.R), Math.Max(Math.Abs(p.G - q.G), Math.Abs(p.B - q.B))));
                }
            }

            return differing;
        }

        private static void Save(string? directory, string name, CpuImage a, CpuImage b)
        {
            if (directory is null)
            {
                return;
            }

            Directory.CreateDirectory(directory);
            PngWriter.Write(Path.Combine(directory, $"{name}-a.png"), a);
            PngWriter.Write(Path.Combine(directory, $"{name}-b.png"), b);
        }
    }
}
