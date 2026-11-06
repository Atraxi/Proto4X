using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Proto4X.Factory.Simulation.Warehouses
{
    /// <summary>One building in a blueprint: everything <see cref="Surface.TryPlace"/> needs.</summary>
    public readonly record struct BlueprintEntry(BuildingKind Kind, int X, int Y, Direction Direction, RecipeId Recipe, int Config);

    /// <summary>
    /// A warehouse interior layout. Every interior *is* one: leaving an edited interior snapshots it
    /// back into its blueprint, and every warehouse naming the blueprint shares it (plans/03).
    /// </summary>
    public sealed class Blueprint(int id, string name)
    {
        public int Id { get; } = id;
        public string Name { get; set; } = name;
        public IReadOnlyList<BlueprintEntry> Entries { get; private set; } = [];

        /// <summary>Bumped whenever the layout changes; interiors built from an older version rebuild on entry.</summary>
        public int Version { get; private set; }

        public BlueprintRating Rating { get; private set; } = BlueprintRating.Empty;

        /// <summary>Replaces the layout if it differs and re-rates it. Returns whether anything changed.</summary>
        public bool SetEntries(IEnumerable<BlueprintEntry> entries)
        {
            // Canonical order, so "same layout" compares equal however it was built.
            var sorted = entries.OrderBy(e => e.Y).ThenBy(e => e.X).ToArray();
            if (sorted.SequenceEqual(Entries))
            {
                return false;
            }

            Entries = sorted;
            Version++;
            Rating = BlueprintCalibrator.Rate(this);
            return true;
        }

        /// <summary>Reads a live interior's buildings as entries.</summary>
        public static IEnumerable<BlueprintEntry> Snapshot(Surface interior)
        {
            var buildings = interior.Buildings;
            for (var slot = 0; slot < buildings.HighWater; slot++)
            {
                if (buildings.IsUsable(slot))
                {
                    yield return new BlueprintEntry(buildings.KindOf(slot), buildings.OriginX(slot), buildings.OriginY(slot),
                        buildings.DirectionOf(slot), buildings.RecipeOf(slot), buildings.ConfigOf(slot));
                }
            }
        }
    }

    /// <summary>
    /// What a blueprint does as a black box, measured: per-window input and output counts, reduced by
    /// their GCD to the smallest repeating cycle, plus its mean power draw.
    /// </summary>
    /// <param name="Inputs">Items consumed per cycle, one entry per input-dock item that was used.</param>
    /// <param name="Outputs">Items produced per cycle, one entry per item that reached an output dock.</param>
    /// <param name="CycleTicks">Cycle length at full power.</param>
    /// <param name="ActiveDraw">Mean interior demand while running, J/tick.</param>
    /// <param name="Version">The blueprint version it rates; with the blueprint id, it tells a warehouse its buffer layout is stale.</param>
    public sealed record BlueprintRating((ItemId Item, int Amount)[] Inputs, (ItemId Item, int Amount)[] Outputs, int CycleTicks, long ActiveDraw, int Version)
    {
        public static readonly BlueprintRating Empty = new([], [], 0, 0, 0);

        public bool CanRun => CycleTicks > 0 && Outputs.Length > 0;

        public static double PerMinute(int amount, int cycleTicks) => cycleTicks == 0 ? 0 : amount * 3600.0 / cycleTicks;

        /// <summary>Ticks per abstract slice: a long cycle is replayed in slices this long, so output trickles instead of landing once a minute.</summary>
        public const int SliceTicks = 60;

        /// <summary>How many slices one cycle is replayed in.</summary>
        public int Slices => Math.Max(1, CycleTicks / SliceTicks);

        /// <summary>
        /// Slice <paramref name="slice"/>'s share of <paramref name="total"/> (an amount or the cycle's
        /// ticks), spread Bresenham-style so the shares over a whole cycle sum to exactly the total.
        /// </summary>
        public static int Share(int total, int slice, int slices) => (int)((long)(slice + 1) * total / slices - (long)slice * total / slices);
    }

    /// <summary>
    /// Rates a blueprint by running it: a throwaway universe (its own scheduler, so nothing leaks into
    /// the game) with one interior floor, input docks refilled to full and output docks emptied every
    /// tick, full power. Warm-up, then a measured window.
    /// </summary>
    /// <remarks>
    /// Measured rather than computed from recipes (as calculator mods do) because a measurement is
    /// right by construction: an inserter that can't keep up, or a starved assembler, shows up in the
    /// rating exactly as it would in play. Cost is a few milliseconds per rating for a 24×24 floor.
    /// </remarks>
    public static class BlueprintCalibrator
    {
        public const int InteriorSize = 24;
        public const int WarmupTicks = 1_800;
        public const int MeasureTicks = 3_600;

        public static BlueprintRating Rate(Blueprint blueprint)
        {
            var universe = new Universe(new SystemScheduler());
            var floor = universe.AddSurface("calibration", SurfaceKind.Interior, InteriorSize, InteriorSize);
            floor.FillTiles(0, 0, InteriorSize, InteriorSize, TileKind.Foundation);
            foreach (var entry in blueprint.Entries)
            {
                floor.TryPlace(entry.Kind, entry.X, entry.Y, entry.Direction, entry.Recipe, entry.Config);
            }

            var consumed = new long[Items.Count];
            var produced = new long[Items.Count];
            long demand = 0;

            for (var tick = 0; tick < WarmupTicks + MeasureTicks; tick++)
            {
                var measuring = tick >= WarmupTicks;
                Refill(floor, measuring ? consumed : null);
                universe.Step();
                Drain(floor, measuring ? produced : null);
                if (measuring)
                {
                    demand += floor.Power.LastDemand;
                }
            }

            var inputs = Enumerable.Range(0, Items.Count).Where(i => consumed[i] > 0).Select(i => ((ItemId)i, consumed[i])).ToArray();
            var outputs = Enumerable.Range(0, Items.Count).Where(i => produced[i] > 0).Select(i => ((ItemId)i, produced[i])).ToArray();
            if (outputs.Length == 0)
            {
                return BlueprintRating.Empty with { Version = blueprint.Version };
            }

            var divisor = (long)MeasureTicks;
            foreach (var (_, amount) in inputs.Concat(outputs))
            {
                divisor = Gcd(divisor, amount);
            }

            return new BlueprintRating(
                [.. inputs.Select(p => (p.Item1, (int)(p.Item2 / divisor)))],
                [.. outputs.Select(p => (p.Item1, (int)(p.Item2 / divisor)))],
                (int)(MeasureTicks / divisor),
                demand / MeasureTicks,
                blueprint.Version);
        }

        /// <summary>Tops every input dock up to full; what it took to do so is what was consumed.</summary>
        private static void Refill(Surface floor, long[]? consumed)
        {
            var buildings = floor.Buildings;
            var inventories = floor.Inventories;
            for (var slot = 0; slot < buildings.HighWater; slot++)
            {
                if (!buildings.IsUsable(slot) || buildings.KindOf(slot) != BuildingKind.InputDock)
                {
                    continue;
                }

                var stack = buildings.InventoryStart(slot);
                var item = inventories.ItemAt(stack);
                if (item == ItemId.None)
                {
                    continue;
                }

                var missing = inventories.LimitAt(stack) - inventories.CountAt(stack);
                if (consumed is not null)
                {
                    consumed[(int)item] += missing;
                }

                inventories.SetStack(stack, item, inventories.LimitAt(stack));
            }
        }

        private static void Drain(Surface floor, long[]? produced)
        {
            var buildings = floor.Buildings;
            var inventories = floor.Inventories;
            for (var slot = 0; slot < buildings.HighWater; slot++)
            {
                if (!buildings.IsUsable(slot) || buildings.KindOf(slot) != BuildingKind.OutputDock)
                {
                    continue;
                }

                var start = buildings.InventoryStart(slot);
                for (var stack = start; stack < start + buildings.InventoryLength(slot); stack++)
                {
                    var count = inventories.CountAt(stack);
                    if (count > 0)
                    {
                        if (produced is not null)
                        {
                            produced[(int)inventories.ItemAt(stack)] += count;
                        }

                        inventories.RemoveFromStack(stack, count);
                    }
                }
            }
        }

        private static long Gcd(long a, long b)
        {
            while (b != 0)
            {
                (a, b) = (b, a % b);
            }

            return a;
        }
    }
}
