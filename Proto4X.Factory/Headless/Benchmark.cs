using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Proto4X.Factory.Headless
{
    /// <summary>
    /// Stamps a grid of smelting modules onto one big planet and measures tick cost: total, per system,
    /// engine overhead with every system body removed, and allocations.
    /// </summary>
    public static class Benchmark
    {
        public static void Run(int modules, int warmupTicks, int measuredTicks, bool idleFirst = false)
        {
            Console.WriteLine($"Benchmark: {modules} modules ({modules * Layouts.BuildingsPerModule:N0} buildings), {warmupTicks} warm-up + {measuredTicks} measured ticks");
            Console.WriteLine();

            // Throwaway pass so tiered JIT has promoted every system before anything is timed. Without it
            // the first variant measured in the process reads ~2x slow at small sizes, whichever it is.
            Measure(Math.Min(modules, 500), 1_000_000, 1_500, 10, bodiesRemoved: false);
            Measure(Math.Min(modules, 500), 1, 1_500, 10, bodiesRemoved: false);

            (string, int)[] variants = [("busy (ore everywhere)", 1_000_000), ("idle (ore runs out at once)", 1)];
            if (idleFirst)
            {
                System.Array.Reverse(variants);
            }

            foreach (var (label, orePerTile) in variants)
            {
                Console.WriteLine($"== {label}");
                var real = Measure(modules, orePerTile, warmupTicks, measuredTicks, bodiesRemoved: false);
                var hollow = Measure(modules, orePerTile, warmupTicks, measuredTicks, bodiesRemoved: true);

                Console.WriteLine($"   tick: {real.MsPerTick:F3} ms  ({1000.0 / real.MsPerTick:F0} UPS ceiling), alloc {real.BytesPerTick:N0} B/tick");
                Console.WriteLine($"   same declarations, empty bodies: {hollow.MsPerTick:F3} ms, alloc {hollow.BytesPerTick:N0} B/tick  <- scheduler + query + chunk building");
                Console.WriteLine($"   {"system",-22} {"ms/tick",9} {"ns/entity",10} {"entities",9}");
                foreach (var (name, ms, entities) in real.PerSystem)
                {
                    var perEntity = entities > 0 ? $"{ms * 1e6 / entities,10:F1}" : $"{"-",10}";
                    Console.WriteLine($"   {name,-22} {ms,9:F4} {perEntity} {entities,9:N0}");
                }

                Console.WriteLine($"   items on belts at end: {real.ItemsOnBelts:N0}, segments: {real.Segments:N0}");
                Console.WriteLine();
            }
        }

        private sealed record Result(double MsPerTick, double BytesPerTick, List<(string Name, double Ms, long Entities)> PerSystem, int ItemsOnBelts, int Segments);

        private static Result Measure(int modules, int orePerTile, int warmupTicks, int measuredTicks, bool bodiesRemoved)
        {
            var timers = new List<TimedSystem>();
            var scheduler = new SystemScheduler();
            var universe = new Universe(scheduler, decorate: system =>
            {
                var timed = new TimedSystem(system, bodiesRemoved);
                timers.Add(timed);
                return timed;
            });

            var columns = (int)Math.Ceiling(Math.Sqrt(modules * (double)Layouts.ModuleHeight / Layouts.ModuleWidth));
            var rows = (modules + columns - 1) / columns;
            var planet = universe.AddSurface("bench", SurfaceKind.Planet, columns * Layouts.ModuleWidth + 2, rows * Layouts.ModuleHeight + 2);
            for (var m = 0; m < modules; m++)
            {
                Layouts.StampSmeltingModule(planet, 1 + (m % columns) * Layouts.ModuleWidth, 1 + (m / columns) * Layouts.ModuleHeight, orePerTile);
            }

            for (var i = 0; i < warmupTicks; i++)
            {
                universe.Step();
            }

            foreach (var timer in timers)
            {
                timer.Reset();
            }

            GC.Collect();
            var bytesBefore = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < measuredTicks; i++)
            {
                universe.Step();
            }

            stopwatch.Stop();
            var bytes = GC.GetAllocatedBytesForCurrentThread() - bytesBefore;

            var perSystem = timers
                .Select(t => (t.Name, t.Elapsed.TotalMilliseconds / measuredTicks, t.EntitiesVisited / measuredTicks))
                .ToList();

            return new Result(stopwatch.Elapsed.TotalMilliseconds / measuredTicks, (double)bytes / measuredTicks, perSystem,
                planet.Belts.CountAllItems(), planet.Belts.SegmentCount);
        }

        /// <summary>
        /// Wraps a system with a stopwatch. One interface call and two timestamps per system per tick -
        /// nothing per entity - so it doesn't distort the loops it measures.
        /// </summary>
        private sealed class TimedSystem(IUpdateSystem inner, bool bodyRemoved) : IUpdateSystem
        {
            private readonly Stopwatch stopwatch = new();

            public string Name { get; } = inner.GetType().Name.Split('`')[0]
                + (inner.GetType().IsGenericType ? $"<{inner.GetType().GetGenericArguments()[0].Name}>" : "");

            public long EntitiesVisited { get; private set; }
            public TimeSpan Elapsed => stopwatch.Elapsed;

            public IReadOnlyList<ComponentRef> RequiredComponentProviders => inner.RequiredComponentProviders;

            public void Reset()
            {
                stopwatch.Reset();
                EntitiesVisited = 0;
            }

            public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
            {
                foreach (var chunk in archetypeChunks)
                {
                    EntitiesVisited += chunk.EntityCount;
                }

                if (bodyRemoved)
                {
                    return;
                }

                stopwatch.Start();
                inner.Update(gameTime, archetypeChunks);
                stopwatch.Stop();
            }
        }
    }
}
