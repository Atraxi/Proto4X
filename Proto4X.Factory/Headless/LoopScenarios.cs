using Microsoft.Xna.Framework;
using MonoGameLibrary.Systems.Infrastructure;
using MonoGameLibrary.UI;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;
using Proto4X.Factory.Simulation.Ships;
using Proto4X.Factory.Simulation.Space;
using Proto4X.Factory.Simulation.Systems;
using Proto4X.Factory.Simulation.Warehouses;
using System;
using System.Linq;

namespace Proto4X.Factory.Headless
{
    /// <summary>Checks for the prototype loop (plans/03) and the engine pieces it needs (plans/01).</summary>
    public static partial class Scenarios
    {
        private const int Minute = 60 * Universe.TicksPerSecond;

        private static void RunLoopScenarios()
        {
            Run("pixel font covers the UI's character set", PixelFontCoverage);
            Run("starter blueprints rate to a runnable cycle", StarterBlueprintRatings);
            Run("abstract warehouse output matches live", AbstractMatchesLive);
            Run("entering and leaving a warehouse conserves items", EnterExitConserves);
            Run("ship design stats", ShipDesignStats);
            Run("shipyard builds exactly one ship from one bill", ShipyardBuildsOne);
            Run("combat: lancers beat a skiff, the station beats a skiff", Combat);
            Run("the whole loop: the starting scene builds Pickets unaided", WholeLoop);
            Run("loop determinism: same commands, same hash, with interiors and a fleet", LoopDeterminism);
        }

        private static void PixelFontCoverage()
        {
            const int scale = 2;
            var (pixels, width, height) = PixelFont.RasterizeAtlas(scale);
            var characters = PixelFont.Characters.ToHashSet();
            var cellWidth = (PixelFont.GlyphWidth + PixelFont.Spacing) * scale;
            var glyphCount = width / cellWidth;

            var emptyGlyphs = 0;
            for (var g = 0; g < glyphCount; g++)
            {
                var lit = false;
                for (var y = 0; y < height && !lit; y++)
                {
                    for (var x = g * cellWidth; x < (g + 1) * cellWidth && !lit; x++)
                    {
                        lit = pixels[y * width + x].A > 0;
                    }
                }

                emptyGlyphs += lit ? 0 : 1;
            }

            Check(emptyGlyphs == 1, $"exactly one blank glyph, the space (got {emptyGlyphs} of {glyphCount})");
            var required = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 .,:;!?%/+-=()[]<>#'\"_|";
            var missing = new string([.. required.Where(c => !characters.Contains(c))]);
            Check(missing.Length == 0, $"every UI character has a glyph{(missing.Length > 0 ? $" (missing '{missing}')" : "")}");
            Check(PixelFont.MeasureWidth("ABC", scale) == 3 * cellWidth, "width is the sum of advances");
        }

        private static void StarterBlueprintRatings()
        {
            foreach (var (name, entries) in ((string, BlueprintEntry[])[])
                [("gears & circuits", Layouts.GearsAndCircuits), ("ship parts", Layouts.ShipParts)])
            {
                var blueprint = new Blueprint(0, name);
                blueprint.SetEntries(entries);
                var rating = blueprint.Rating;
                Console.WriteLine($"   {name}: {Describe(rating)}");
                Check(rating.CanRun, $"{name} rates to a runnable cycle");
            }

            var gears = new Blueprint(0, "gears only");
            gears.SetEntries(Layouts.GearsAndCircuits.Take(5));
            var gearRating = gears.Rating;
            Console.WriteLine($"   gears only: {Describe(gearRating)}");
            Check(gearRating.Inputs.Single().Item == ItemId.IronPlate && gearRating.Outputs.Single().Item == ItemId.Gear
                && gearRating.Inputs[0].Amount == 2 * gearRating.Outputs[0].Amount, "a lone gear assembler rates to exactly 2 plates per gear");
            Check(gears.SetEntries(Layouts.GearsAndCircuits.Take(5).Reverse()) == false, "the same entries in another order are the same blueprint (no re-rate)");
        }

        private static string Describe(BlueprintRating rating) =>
            $"{string.Join(" + ", rating.Inputs.Select(p => $"{p.Amount} {p.Item}"))} -> {string.Join(" + ", rating.Outputs.Select(p => $"{p.Amount} {p.Item}"))} every {rating.CycleTicks} ticks ({rating.Slices} slices), draw {rating.ActiveDraw}";

        /// <summary>A warehouse fed from two big chests and emptied into a third, powered, on a fresh planet.</summary>
        private static (Universe Universe, Surface Planet) WarehouseBench(bool hashing = false)
        {
            var scheduler = new SystemScheduler();
            var universe = new Universe(scheduler, hashing);
            var planet = universe.AddSurface("planet", SurfaceKind.Planet, 40, 30);
            planet.FillTiles(0, 0, 40, 30, TileKind.Ground);
            var blueprint = universe.AddBlueprint("Gears & circuits", Layouts.GearsAndCircuits);
            planet.TryPlace(BuildingKind.Warehouse, 10, 10, Direction.North, config: blueprint.Id);
            planet.TryPlace(BuildingKind.Inserter, 16, 11, Direction.East);
            planet.TryPlace(BuildingKind.Chest, 17, 11, Direction.North);
            planet.TryPlace(BuildingKind.Inserter, 16, 12, Direction.East);
            planet.TryPlace(BuildingKind.Chest, 17, 12, Direction.North);
            planet.TryPlace(BuildingKind.Inserter, 16, 13, Direction.East);
            planet.TryPlace(BuildingKind.Chest, 17, 13, Direction.North);
            PowerWithGenerator(planet, 10, 18);
            planet.TryPlace(BuildingKind.Pole, 9, 15, Direction.North);
            planet.TryPlace(BuildingKind.Pole, 14, 15, Direction.North);
            planet.TryPlace(BuildingKind.Pole, 16, 14, Direction.North);
            planet.TryPlace(BuildingKind.Pole, 9, 12, Direction.North);
            return (universe, planet);
        }

        private static void AbstractMatchesLive()
        {
            // Inputs are topped up every tick, as the calibrator does, so this compares the two modes
            // and not two ways of sharing out a starved feed.
            (int Gears, int Circuits) Made(bool live)
            {
                var (universe, planet) = WarehouseBench();
                if (live)
                {
                    universe.Enqueue(new Command(CommandKind.EnterInterior, planet.Index, 11, 11));
                }

                var slot = planet.Grid.OccupantAt(10, 10);
                (int, int) Out() => (Enumerable.Range(11, 3).Sum(y => Layouts.CountIn(planet, 17, y, ItemId.Gear)), Enumerable.Range(11, 3).Sum(y => Layouts.CountIn(planet, 17, y, ItemId.Circuit)));
                void Run(int ticks)
                {
                    for (var t = 0; t < ticks; t++)
                    {
                        var start = planet.Buildings.InventoryStart(slot);
                        for (var stack = start; start >= 0 && stack < start + planet.Buildings.InputStacks(slot); stack++)
                        {
                            planet.Inventories.SetStack(stack, planet.Inventories.ItemAt(stack), planet.Inventories.LimitAt(stack));
                        }

                        universe.Step();
                    }
                }

                Run(2 * Minute);
                var before = Out();
                Run(10 * Minute);
                var after = Out();
                Check(!live || universe.Live is not null, "the live run stayed live");
                return (after.Item1 - before.Item1, after.Item2 - before.Item2);
            }

            var abstractMade = Made(live: false);
            var liveMade = Made(live: true);
            Console.WriteLine($"   out over 10 min: abstract {abstractMade.Gears} gears, {abstractMade.Circuits} circuits; live {liveMade.Gears} gears, {liveMade.Circuits} circuits");
            Check(liveMade.Gears > 0 && liveMade.Circuits > 0, "live produces both");
            Check(Math.Abs(abstractMade.Gears - liveMade.Gears) <= 0.05 * liveMade.Gears, "abstract gears within 5% of live");
            Check(Math.Abs(abstractMade.Circuits - liveMade.Circuits) <= 0.05 * liveMade.Circuits, "abstract circuits within 5% of live");
        }

        private static void EnterExitConserves()
        {
            // Unpowered, so nothing crafts: every item is somewhere, and only enter/exit moves things.
            var scheduler = new SystemScheduler();
            var universe = new Universe(scheduler);
            var planet = universe.AddSurface("planet", SurfaceKind.Planet, 30, 30);
            planet.FillTiles(0, 0, 30, 30, TileKind.Ground);
            var blueprint = universe.AddBlueprint("Gears & circuits", Layouts.GearsAndCircuits);
            planet.TryPlace(BuildingKind.Warehouse, 10, 10, Direction.North, config: blueprint.Id);
            var planetProbe = new Probe();
            planetProbe.Attach(scheduler, planet);
            var probe = new Probe();
            universe.SurfaceAdded += surface => probe.Attach(scheduler, surface);
            RunTicks(universe, 2);
            var slot = planet.Grid.OccupantAt(10, 10);
            var start = planet.Buildings.InventoryStart(slot);
            planet.Inventories.SetStack(start, ItemId.IronPlate, 150);
            planet.Inventories.SetStack(start + 1, ItemId.CopperPlate, 90);

            int Total()
            {
                var total = 0;
                foreach (var surface in universe.Surfaces)
                {
                    total += CountEverywhere(surface, ItemId.IronPlate) + CountEverywhere(surface, ItemId.CopperPlate);
                }

                return total + probe.HeldByInsertersOf[(int)ItemId.IronPlate] + probe.HeldByInsertersOf[(int)ItemId.CopperPlate]
                    + planetProbe.HeldByWarehousesOf[(int)ItemId.IronPlate] + planetProbe.HeldByWarehousesOf[(int)ItemId.CopperPlate];
            }

            var first = Total();
            for (var round = 0; round < 3; round++)
            {
                universe.Enqueue(new Command(CommandKind.EnterInterior, planet.Index, 12, 12));
                RunTicks(universe, 30);
                universe.Enqueue(new Command(CommandKind.ExitInterior, planet.Index, 0, 0));
                RunTicks(universe, 30);
            }

            var interiorPlates = CountEverywhere(universe.Surfaces[^1], ItemId.IronPlate);
            Console.WriteLine($"   plates before {first}, after three round trips {Total()} ({interiorPlates} iron in the interior's docks)");
            Check(interiorPlates > 0, "entering moved plates into the interior's input docks");
            Check(Total() == first, "no item created or lost across live and abstract");
            Check(universe.Surfaces.Count == 2, "re-entering reuses the same interior");
        }

        private static PartPlacement P(PartKind kind, int x, int y) => new(kind, x, y, Direction.North);

        private static void ShipDesignStats()
        {
            foreach (var design in (ShipDesign[])[ShipPresets.Picket(), ShipPresets.Lancer(), ShipPresets.Bulwark(), ShipPresets.Skiff(), ShipPresets.Brute()])
            {
                var stats = design.Stats;
                Console.WriteLine($"   {design.Name}: mass {stats.Mass}, hp {stats.Hp}, fwd {stats.Acceleration[(int)Direction.North]:0.0}, turn {stats.AngularAcceleration:0.00}, dps {stats.Dps}, eff {stats.Efficiency:0.00}, build {stats.BuildTicks}t, cost {string.Join(", ", stats.Cost.Select(c => $"{c.Count} {c.Item}"))}{(stats.IsValid ? "" : " INVALID: " + string.Join("; ", stats.Errors))}");
                Check(stats.IsValid, $"{design.Name} is valid");
            }

            var basic = new ShipDesign("basic",
            [
                P(PartKind.Core, 0, 0), P(PartKind.Reactor, 2, 0), P(PartKind.Gyro, 0, 2), P(PartKind.Gyro, 3, 2),
                P(PartKind.Thruster, 1, 2), P(PartKind.Thruster, 2, 2),
            ]);
            var b = basic.Stats;
            Check(b.IsValid, $"symmetric test ship is valid{(b.IsValid ? "" : ": " + string.Join("; ", b.Errors))}");
            Check(Math.Abs(b.Acceleration[(int)Direction.North] * b.Mass - 2 * 80f) < 0.01f * 160f, "forward acceleration is total thrust over mass");
            Check(b.Cost.Sum(c => c.Count) == basic.Parts.Sum(p => Parts.Spec(p.Kind).Cost.Sum(c => c.Count)), "the bill is the sum of the parts' costs");

            var blocked = basic.With(parts: [.. basic.Parts, P(PartKind.Hull, 1, 4)]);
            Check(blocked.Stats.BlockedThrusters.Count == 1 && blocked.Stats.Acceleration[(int)Direction.North] < b.Acceleration[(int)Direction.North],
                "a part behind a thruster blocks its exhaust (Cosmoteer)");

            var floating = basic.With(parts: [.. basic.Parts, P(PartKind.Hull, 10, 10)]);
            Check(!floating.Stats.IsValid, "a disconnected part makes the design invalid");

            var hungry = new ShipDesign("hungry",
            [
                P(PartKind.Core, 0, 0), P(PartKind.Laser, 2, 0), P(PartKind.Laser, 3, 0), P(PartKind.Laser, 4, 0), P(PartKind.Laser, 5, 0),
                P(PartKind.Thruster, 0, 2), P(PartKind.Gyro, 1, 2),
            ]);
            Check(hungry.Stats.Efficiency < 1f, $"a power deficit cuts efficiency (got {hungry.Stats.Efficiency:0.00})");
        }

        private static void ShipyardBuildsOne()
        {
            var scheduler = new SystemScheduler();
            var universe = new Universe(scheduler);
            var station = universe.AddSurface("station", SurfaceKind.Orbit, 24, 16);
            station.FillTiles(0, 0, 24, 16, TileKind.Foundation);
            station.TryPlace(BuildingKind.Shipyard, 2, 2, Direction.North, config: ShipyardOrder.Pack(0, ShipyardMode.Repeat));
            for (var x = 8; x <= 14; x += 2)
            {
                station.TryPlace(BuildingKind.SolarPanel, x, 2, Direction.North);
            }

            station.TryPlace(BuildingKind.Pole, 7, 4, Direction.North);
            station.TryPlace(BuildingKind.Pole, 12, 4, Direction.North);
            universe.SetUpSpace(new Vector2(400f, 160f));
            universe.Space.Peaceful = true;

            var slot = station.Grid.OccupantAt(2, 2);
            var start = station.Buildings.InventoryStart(slot);
            foreach (var (item, count) in universe.Designs[0]!.Stats.Cost)
            {
                station.Inventories.SetStack(start + Array.IndexOf(Buildings.ShipyardStacks, item), item, count);
            }

            RunTicks(universe, 3 * Minute);
            var ships = universe.Space.Snapshot.Count(v => v.Faction == Faction.Player && v.Role == BodyRole.Ship);
            var left = Enumerable.Range(start, Buildings.ShipyardStacks.Length).Sum(station.Inventories.CountAt);
            Console.WriteLine($"   ships {ships}, components left {left}");
            Check(ships == 1, "exactly one ship launched");
            Check(left == 0, "the yard's stock is empty");
        }

        private static void Combat()
        {
            static (int Player, int Raider, bool StationAlive) Fight(Action<Universe> setup, int seconds)
            {
                var universe = new Universe(new SystemScheduler());
                var station = universe.AddSurface("station", SurfaceKind.Orbit, 8, 8);
                station.Frame.Position = new Vector2(60f, 150f);
                universe.SetUpSpace(new Vector2(420f, 160f));
                universe.Space.Peaceful = true;
                setup(universe);
                RunTicks(universe, seconds * Universe.TicksPerSecond);
                var snapshot = universe.Space.Snapshot;
                return (snapshot.Count(v => v.Faction == Faction.Player && v.Role == BodyRole.Ship),
                    snapshot.Count(v => v.Faction == Faction.Raider && v.Role == BodyRole.Ship),
                    snapshot.Any(v => v.Role == BodyRole.Station));
            }

            var duel = Fight(u =>
            {
                var meet = new Vector2(240f, 155f);
                u.Space.Spawn(ShipPresets.Lancer(), Faction.Player, meet - new Vector2(12f, 4f), FlightSystem.Heading(new Vector2(1, 0)), meet - new Vector2(12f, 4f));
                u.Space.Spawn(ShipPresets.Lancer(), Faction.Player, meet - new Vector2(12f, -4f), FlightSystem.Heading(new Vector2(1, 0)), meet - new Vector2(12f, -4f));
                u.Space.Spawn(ShipPresets.Skiff(), Faction.Raider, meet + new Vector2(14f, 0f), FlightSystem.Heading(new Vector2(-1, 0)), meet - new Vector2(12f, 0f));
            }, 60);
            Console.WriteLine($"   two lancers vs a skiff: {duel.Player} lancers, {duel.Raider} skiffs left");
            Check(duel.Raider == 0 && duel.Player == 2, "two lancers kill a skiff and both survive");

            var raid = Fight(u => u.Space.Spawn(ShipPresets.Skiff(), Faction.Raider, u.Space.StationPosition + new Vector2(60f, 0f), FlightSystem.Heading(new Vector2(-1, 0)), u.Space.StationPosition), 90);
            Console.WriteLine($"   a lone skiff vs the station: {raid.Raider} skiffs left, station alive {raid.StationAlive}");
            Check(raid.Raider == 0 && raid.StationAlive, "the station kills a lone skiff");
        }

        private static void WholeLoop()
        {
            var universe = new Universe(new SystemScheduler());
            var planet = universe.AddSurface("planet", SurfaceKind.Planet, 64, 48);
            var station = universe.AddSurface("station", SurfaceKind.Orbit, 24, 16);
            Layouts.BuildStarterScene(planet, station);
            universe.Space.Peaceful = true;

            int Ships() => universe.Space.Snapshot.Count(v => v.Faction == Faction.Player && v.Role == BodyRole.Ship);
            int Sum(Surface surface, params ItemId[] items) => items.Sum(i => CountEverywhere(surface, i));
            RunTicks(universe, 2); // spawns land in the snapshot on the second tick
            var starters = Ships();
            var firstPicketMinute = -1;
            for (var minute = 1; minute <= 20; minute++)
            {
                RunTicks(universe, Minute);
                if (firstPicketMinute < 0 && Ships() > starters)
                {
                    firstPicketMinute = minute;
                }

                if (minute % 4 == 0)
                {
                    Console.WriteLine($"   {minute,2} min: iron {Sum(planet, ItemId.IronPlate)}, copper {Sum(planet, ItemId.CopperPlate)}, steel {Sum(planet, ItemId.Steel)}, gears {Sum(planet, ItemId.Gear)}, circuits {Sum(planet, ItemId.Circuit)}"
                        + $" | parts on planet {Sum(planet, ItemId.Frame, ItemId.Engine, ItemId.Processor, ItemId.PowerCell, ItemId.Emitter)}, in flight {universe.InTransit.Count}"
                        + $" | station frames {CountEverywhere(station, ItemId.Frame)}, engines {CountEverywhere(station, ItemId.Engine)}, processors {CountEverywhere(station, ItemId.Processor)}, cells {CountEverywhere(station, ItemId.PowerCell)}, emitters {CountEverywhere(station, ItemId.Emitter)} | ships {Ships()}");
                }
            }

            Console.WriteLine($"   first Picket at minute {firstPicketMinute}; {Ships() - starters} built in 20 min");
            Check(planet.Buildings.AliveCount > 100 && station.Buildings.AliveCount > 10, "the starting scene placed");
            Check(firstPicketMinute > 0 && firstPicketMinute <= 15, "the shipyard launches a Picket within 15 minutes (the loop closes)");
            Check(Ships() - starters >= 2, "and keeps launching them");
        }

        private static void LoopDeterminism()
        {
            static ulong Hash(int exitTick)
            {
                var universe = new Universe(new SystemScheduler(), includeStateHashing: true);
                var planet = universe.AddSurface("planet", SurfaceKind.Planet, 64, 48);
                var station = universe.AddSurface("station", SurfaceKind.Orbit, 24, 16);
                Layouts.BuildStarterScene(planet, station);
                var serials = universe.Space.Snapshot.Count; // nothing ticked yet: empty
                for (var tick = 0; tick < 3_000; tick++)
                {
                    switch (tick)
                    {
                        case 300: universe.Enqueue(new Command(CommandKind.EnterInterior, planet.Index, 37, 7)); break;
                        case 400: universe.Enqueue(new Command(CommandKind.Place, universe.Surfaces.Count - 1, 15, 15, BuildingKind.Chest)); break;
                        case 500: universe.Enqueue(new Command(CommandKind.MoveShips, -1, 120, 150, Payload: universe.Space.Snapshot.Where(v => v.Faction == Faction.Player && v.Role == BodyRole.Ship).Select(v => v.Serial).ToArray())); break;
                    }

                    if (tick == exitTick)
                    {
                        universe.Enqueue(new Command(CommandKind.ExitInterior, -1, 0, 0));
                    }

                    universe.Step();
                }

                return universe.ComputeStateHash() ^ (ulong)serials;
            }

            var first = Hash(1_200);
            var second = Hash(1_200);
            var shifted = Hash(1_201);
            Console.WriteLine($"   hashes {first:X16} / {second:X16} / exit-one-tick-later {shifted:X16}");
            Check(first == second, "two runs from the same commands agree");
            Check(first != shifted, "the hash notices an exit one tick later");
        }
    }
}
