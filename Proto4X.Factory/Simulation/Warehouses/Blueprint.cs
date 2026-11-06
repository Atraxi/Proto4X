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
    /// One independent production line inside a blueprint (an "island": buildings joined by
    /// inserters, belts and drill outputs), measured on its own and run on its own while abstract.
    /// Stack indices point into the warehouse buffer's input and output runs.
    /// </summary>
    /// <remarks>
    /// Within an island the outputs are coupled: one cycle makes all of them, so if one backs up the
    /// whole island stops. That is Factorio's oil-refinery rule, kept on purpose: separate lines in
    /// one warehouse don't block each other, a line with two products does.
    /// </remarks>
    public sealed record IslandRating(int[] InputStacks, int[] InputAmounts, int[] OutputStacks, int[] OutputAmounts, int CycleTicks, long ActiveDraw)
    {
        /// <summary>How many slices one cycle is replayed in.</summary>
        public int Slices => Math.Max(1, CycleTicks / BlueprintRating.SliceTicks);
    }

    /// <summary>
    /// What a blueprint does as a black box, measured per island. <see cref="Inputs"/> and
    /// <see cref="Outputs"/> are the merged item lists (the warehouse buffer's layout), with amounts
    /// per minute for display and stack sizing.
    /// </summary>
    /// <param name="Version">The blueprint version it rates; with the blueprint id, it tells a warehouse its buffer layout is stale.</param>
    public sealed record BlueprintRating((ItemId Item, int Amount)[] Inputs, (ItemId Item, int Amount)[] Outputs, IslandRating[] Islands, int Version)
    {
        public static readonly BlueprintRating Empty = new([], [], [], 0);

        /// <summary>Most islands a warehouse runs independently; extra islands are merged into the last (coupled).</summary>
        public const int MaxIslands = 6;

        /// <summary>Ticks per abstract slice: a long cycle is replayed in slices this long, so output trickles instead of landing once a minute.</summary>
        public const int SliceTicks = 60;

        public bool CanRun => Islands.Length > 0;

        /// <summary>Mean draw with every island running.</summary>
        public long ActiveDraw => Islands.Sum(i => i.ActiveDraw);

        /// <summary>
        /// Slice <paramref name="slice"/>'s share of <paramref name="total"/> (an amount or the cycle's
        /// ticks), spread Bresenham-style so the shares over a whole cycle sum to exactly the total.
        /// </summary>
        public static int Share(int total, int slice, int slices) => (int)((long)(slice + 1) * total / slices - (long)slice * total / slices);
    }

    /// <summary>
    /// Rates a blueprint by running it: a throwaway universe (its own scheduler, so nothing leaks into
    /// the game) with one interior floor, input docks refilled to full and output docks emptied every
    /// tick, full power. Warm-up, then a measured window. Each island is run alone.
    /// </summary>
    /// <remarks>
    /// Measured rather than computed from recipes (as calculator mods do) because a measurement is
    /// right by construction: an inserter that can't keep up, or a starved assembler, shows up in the
    /// rating exactly as it would in play. Cost is a few milliseconds per island for a 24×24 floor,
    /// paid synchronously when a blueprint changes (shortcut: a visible hitch on leaving an edited
    /// interior with many islands; move to a background rating if it shows).
    /// </remarks>
    public static class BlueprintCalibrator
    {
        public const int InteriorSize = 24;
        public const int WarmupTicks = 1_800;
        public const int MeasureTicks = 3_600;

        private sealed record Measurement((ItemId Item, int Amount)[] Inputs, (ItemId Item, int Amount)[] Outputs, int CycleTicks, long ActiveDraw);

        public static BlueprintRating Rate(Blueprint blueprint)
        {
            // Islands that make nothing (a stray pole, an unfed chain) aren't lines.
            var islands = Islands(blueprint.Entries).Select(i => (Entries: i, Measured: Measure(i))).Where(p => p.Measured is not null).ToList();
            var measured = islands.Select(p => p.Measured!).ToList();

            // Beyond the cap, re-measure the leftover islands together as one coupled line.
            if (islands.Count > BlueprintRating.MaxIslands)
            {
                var rest = Measure([.. islands.Skip(BlueprintRating.MaxIslands - 1).SelectMany(p => p.Entries)]);
                measured = [.. measured.Take(BlueprintRating.MaxIslands - 1), .. rest is null ? Array.Empty<Measurement>() : [rest]];
            }

            if (measured.Count == 0)
            {
                return BlueprintRating.Empty with { Version = blueprint.Version };
            }

            static (ItemId Item, int Amount)[] Merge(IEnumerable<((ItemId Item, int Amount)[] Items, int Cycle)> runs) =>
                [.. runs.SelectMany(r => r.Items.Select(p => (p.Item, PerMinute: p.Amount * 3600.0 / r.Cycle)))
                    .GroupBy(p => p.Item).OrderBy(g => g.Key)
                    .Select(g => (g.Key, (int)Math.Ceiling(g.Sum(p => p.PerMinute))))];

            var inputs = Merge(measured.Select(m => (m.Inputs, m.CycleTicks)));
            var outputs = Merge(measured.Select(m => (m.Outputs, m.CycleTicks)));
            var islandRatings = measured.Select(m => new IslandRating(
                [.. m.Inputs.Select(p => Array.FindIndex(inputs, q => q.Item == p.Item))], [.. m.Inputs.Select(p => p.Amount)],
                [.. m.Outputs.Select(p => Array.FindIndex(outputs, q => q.Item == p.Item))], [.. m.Outputs.Select(p => p.Amount)],
                m.CycleTicks, m.ActiveDraw)).ToArray();
            return new BlueprintRating(inputs, outputs, islandRatings, blueprint.Version);
        }

        /// <summary>
        /// Splits entries into islands: inserters join what's behind them to what's in front, belts
        /// join to the tile they feed, drills to their output tile. In first-entry order.
        /// </summary>
        public static List<List<BlueprintEntry>> Islands(IReadOnlyList<BlueprintEntry> entries)
        {
            var parent = Enumerable.Range(0, entries.Count).ToArray();
            int Find(int i) => parent[i] == i ? i : parent[i] = Find(parent[i]);
            void Union(int a, int b)
            {
                if (b >= 0)
                {
                    var (ra, rb) = (Find(a), Find(b));
                    parent[Math.Max(ra, rb)] = Math.Min(ra, rb);
                }
            }

            var occupant = new Dictionary<(int, int), int>();
            for (var i = 0; i < entries.Count; i++)
            {
                var size = Buildings.Size(entries[i].Kind);
                for (var dy = 0; dy < size; dy++)
                {
                    for (var dx = 0; dx < size; dx++)
                    {
                        occupant[(entries[i].X + dx, entries[i].Y + dy)] = i;
                    }
                }
            }

            int At(int x, int y) => occupant.TryGetValue((x, y), out var i) ? i : -1;
            for (var i = 0; i < entries.Count; i++)
            {
                var (kind, x, y, direction) = (entries[i].Kind, entries[i].X, entries[i].Y, entries[i].Direction);
                switch (kind)
                {
                    case BuildingKind.Inserter:
                        Union(i, At(x - direction.Dx(), y - direction.Dy()));
                        Union(i, At(x + direction.Dx(), y + direction.Dy()));
                        break;
                    case BuildingKind.Belt:
                        Union(i, At(x + direction.Dx(), y + direction.Dy()));
                        break;
                    case BuildingKind.Drill:
                        var (ox, oy) = Surface.DrillOutputTile(x, y, direction);
                        Union(i, At(ox, oy));
                        break;
                }
            }

            return [.. Enumerable.Range(0, entries.Count).GroupBy(Find).OrderBy(g => g.Key).Select(g => g.Select(i => entries[i]).ToList())];
        }

        private static Measurement? Measure(IReadOnlyList<BlueprintEntry> entries)
        {
            var universe = new Universe(new SystemScheduler());
            var floor = universe.AddSurface("calibration", SurfaceKind.Interior, InteriorSize, InteriorSize);
            floor.FillTiles(0, 0, InteriorSize, InteriorSize, TileKind.Foundation);
            foreach (var entry in entries)
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
                return null;
            }

            var divisor = (long)MeasureTicks;
            foreach (var (_, amount) in inputs.Concat(outputs))
            {
                divisor = Gcd(divisor, amount);
            }

            return new Measurement(
                [.. inputs.Select(p => (p.Item1, (int)(p.Item2 / divisor)))],
                [.. outputs.Select(p => (p.Item1, (int)(p.Item2 / divisor)))],
                (int)(MeasureTicks / divisor),
                demand / MeasureTicks);
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
