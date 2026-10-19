using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;
using System;
using System.Collections.Generic;

namespace Proto4X.Factory.Headless
{
    /// <summary>Behavioural checks of the simulation. Each builds a small layout, runs it, and asserts on the outcome.</summary>
    public static class Scenarios
    {
        private static int failures;
        private static int passes;

        public static int RunAll()
        {
            Run("smelting line conserves ore", SmeltingLineConservesOre);
            Run("assembler turns plates into gears 2:1", AssemblerMakesGears);
            Run("belt flow rate and compression", BeltFlowAndCompression);
            Run("belt loop keeps its items and keeps moving", BeltLoop);
            Run("side-loading joins one lane", SideLoading);
            Run("curve carries both lanes", CurveCarriesBothLanes);
            Run("no power, no progress", NoPowerNoProgress);
            Run("deconstruct and replace re-wires inserters", DeconstructAndReplace);
            Run("removing several same-archetype entities in one tick", MultiRemovalIntegrity);
            Run("recipe change pulled in by the crafter", RecipeChange);
            Run("surface conditions gate placement and recipes", SurfaceConditions);
            Run("launch reaches the ship's receiver", LaunchToShip);
            Run("same commands, same state hash", Determinism);

            Console.WriteLine();
            Console.WriteLine($"{passes} checks passed, {failures} failed");
            return failures;
        }

        private static void Run(string name, Action scenario)
        {
            Console.WriteLine($"-- {name}");
            try
            {
                scenario();
            }
            catch (Exception exception)
            {
                failures++;
                Console.WriteLine($"   FAIL threw {exception.GetType().Name}: {exception.Message}");
                Console.WriteLine(exception.StackTrace);
            }
        }

        private static void Check(bool condition, string what)
        {
            if (condition)
            {
                passes++;
                Console.WriteLine($"   ok   {what}");
            }
            else
            {
                failures++;
                Console.WriteLine($"   FAIL {what}");
            }
        }

        private static (Universe Universe, Surface Planet, Probe Probe) NewPlanet(int width = 64, int height = 48, bool hashing = false)
        {
            var scheduler = new SystemScheduler();
            var universe = new Universe(scheduler, hashing);
            var planet = universe.AddSurface("planet", SurfaceKind.Planet, width, height);
            planet.FillTiles(0, 0, width, height, TileKind.Ground);
            var probe = new Probe();
            probe.Attach(scheduler, planet);
            return (universe, planet, probe);
        }

        private static void RunTicks(Universe universe, int ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                universe.Step();
            }
        }

        private static int OreRemaining(Surface surface, int x, int y, int size)
        {
            var total = 0;
            for (var ty = y; ty < y + size; ty++)
            {
                for (var tx = x; tx < x + size; tx++)
                {
                    total += surface.Grid.OreAmountAt(tx, ty);
                }
            }

            return total;
        }

        private static int CountEverywhere(Surface surface, ItemId item)
        {
            var total = surface.Belts.CountItems(item);
            for (var slot = 0; slot < surface.Buildings.HighWater; slot++)
            {
                if (surface.Buildings.IsAlive(slot) && surface.Buildings.InventoryStart(slot) >= 0)
                {
                    total += surface.Inventories.CountOf(surface.Buildings.InventoryStart(slot), surface.Buildings.InventoryLength(slot), item);
                }
            }

            return total;
        }

        // ------------------------------------------------------------------------------------

        private static void SmeltingLineConservesOre()
        {
            var (universe, planet, probe) = NewPlanet();
            const int orePerTile = 1_000;
            Layouts.StampSmeltingModule(planet, 4, 4, orePerTile);
            Check(planet.Buildings.AliveCount == Layouts.BuildingsPerModule, $"module placed all {Layouts.BuildingsPerModule} buildings (got {planet.Buildings.AliveCount})");

            RunTicks(universe, 3_600);

            var mined = 2 * (4 * orePerTile) - OreRemaining(planet, 4, 4, 2) - OreRemaining(planet, 4, 7, 2);
            var plates = Layouts.CountIn(planet, 16, 4, ItemId.IronPlate) + Layouts.CountIn(planet, 16, 7, ItemId.IronPlate);
            var inTransit = CountEverywhere(planet, ItemId.IronOre)
                + probe.HeldByInsertersOf[(int)ItemId.IronOre] + probe.HeldByInsertersOf[(int)ItemId.IronPlate] + probe.HeldByDrills + probe.CraftsInProgress
                + CountEverywhere(planet, ItemId.IronPlate) - plates;

            Console.WriteLine($"   mined {mined}, plates in chests {plates}, in transit {inTransit}");
            Check(mined >= 50, "two drills mined at least 50 ore in 60 s (expect ~60)");
            Check(plates >= 40, "at least 40 plates reached the chests");
            Check(mined == plates + inTransit, "every mined ore is a plate or still in transit (no duplication, no loss)");
            Check(probe.IntegrityErrors == 0 && probe.EntitiesSeen == Layouts.BuildingsPerModule, "entities agree with the building table");
        }

        private static void AssemblerMakesGears()
        {
            var (universe, planet, probe) = NewPlanet();
            planet.TryPlace(BuildingKind.Chest, 10, 11, Direction.East);
            Layouts.Fill(planet, 10, 11, ItemId.IronPlate, 100);
            planet.TryPlace(BuildingKind.Inserter, 11, 11, Direction.East);
            planet.TryPlace(BuildingKind.Assembler, 12, 10, Direction.East, RecipeId.Gear);
            planet.TryPlace(BuildingKind.Inserter, 15, 11, Direction.East);
            planet.TryPlace(BuildingKind.Chest, 16, 11, Direction.East);
            PowerWithGenerator(planet, 10, 14);
            planet.TryPlace(BuildingKind.Pole, 13, 13, Direction.North);

            RunTicks(universe, 7_200);

            var platesLeft = CountEverywhere(planet, ItemId.IronPlate);
            var gearsAnywhere = CountEverywhere(planet, ItemId.Gear);
            var gears = Layouts.CountIn(planet, 16, 11, ItemId.Gear);
            var heldPlates = probe.HeldByInsertersOf[(int)ItemId.IronPlate];
            var heldGears = probe.HeldByInsertersOf[(int)ItemId.Gear];
            Console.WriteLine($"   plates left {platesLeft}, gears in output chest {gears}, crafting {probe.CraftsInProgress}, held {heldPlates}p/{heldGears}g");
            Check(gears >= 20, "at least 20 gears made");
            Check(100 - platesLeft - heldPlates == 2 * (gearsAnywhere + heldGears + probe.CraftsInProgress),
                "plates consumed are exactly twice the gears made (counting crafts in progress)");
        }

        /// <summary>A generator at (x, y), its coal chest and feeding inserter, and a pole over all three.</summary>
        private static void PowerWithGenerator(Surface surface, int x, int y)
        {
            surface.TryPlace(BuildingKind.Generator, x, y, Direction.North);
            surface.TryPlace(BuildingKind.Inserter, x + 2, y, Direction.West);
            surface.TryPlace(BuildingKind.Chest, x + 3, y, Direction.North);
            Layouts.Fill(surface, x + 3, y, ItemId.Coal, 200);
            surface.TryPlace(BuildingKind.Pole, x + 2, y + 1, Direction.North);
        }

        private static void BeltFlowAndCompression()
        {
            var (universe, planet, _) = NewPlanet();
            const int length = 20;
            for (var x = 2; x < 2 + length; x++)
            {
                planet.TryPlace(BuildingKind.Belt, x, 5, Direction.East);
            }

            RunTicks(universe, 2);
            var firstSlot = planet.Grid.OccupantAt(2, 5);

            var inserted = 0;
            for (var tick = 0; tick < 400; tick++)
            {
                if (planet.Belts.TryInsertOnTile(firstSlot, 0, ItemId.IronPlate))
                {
                    inserted++;
                }

                universe.Step();
            }

            Console.WriteLine($"   inserted {inserted} in 400 ticks");
            Check(inserted is >= 48 and <= 51, "free-flowing lane accepts one item per 8 ticks (7.5/s)");

            for (var tick = 0; tick < 3_000; tick++)
            {
                planet.Belts.TryInsertOnTile(firstSlot, 0, ItemId.IronPlate);
                universe.Step();
            }

            var onBelt = planet.Belts.CountAllItems();
            Console.WriteLine($"   compressed lane holds {onBelt}");
            Check(onBelt is >= 78 and <= 80, "a blocked lane compresses to 4 items per tile");
            Check(planet.Belts.SegmentCount == 4, $"20 tiles x 2 lanes split into 16-tile segments (got {planet.Belts.SegmentCount})");
        }

        private static void BeltLoop()
        {
            var (universe, planet, _) = NewPlanet();
            // A 6x6 ring, clockwise.
            for (var i = 0; i < 5; i++)
            {
                planet.TryPlace(BuildingKind.Belt, 5 + i, 5, Direction.East);
                planet.TryPlace(BuildingKind.Belt, 10, 5 + i, Direction.South);
                planet.TryPlace(BuildingKind.Belt, 10 - i, 10, Direction.West);
                planet.TryPlace(BuildingKind.Belt, 5, 10 - i, Direction.North);
            }

            RunTicks(universe, 1);
            for (var i = 0; i < 5; i++)
            {
                planet.Belts.TryInsertOnTile(planet.Grid.OccupantAt(5 + i, 5), 0, ItemId.Gear);
                planet.Belts.TryInsertOnTile(planet.Grid.OccupantAt(5 + i, 5), 1, ItemId.Wire);
            }

            var before = planet.Belts.CountAllItems();
            var segment = planet.Belts.Segments[0];
            var frontBefore = segment.Count > 0 ? segment.PositionOf(0) : -1;
            RunTicks(universe, 1_000);
            var after = planet.Belts.CountAllItems();

            Console.WriteLine($"   {planet.Belts.SegmentCount} segments, items {before} -> {after}");
            Check(before == 10 && after == 10, "loop neither loses nor gains items");
            Check(planet.Belts.SegmentCount == 4, "20-tile ring is two segments per lane");
            Check(segment.Count == 0 || segment.PositionOf(0) != frontBefore, "items are still circulating");
        }

        private static void SideLoading()
        {
            var (universe, planet, _) = NewPlanet();
            // Main line east along y=10; a feeder coming south into its side at x=8.
            for (var x = 4; x < 14; x++)
            {
                planet.TryPlace(BuildingKind.Belt, x, 10, Direction.East);
            }

            for (var y = 5; y < 10; y++)
            {
                planet.TryPlace(BuildingKind.Belt, 8, y, Direction.South);
            }

            RunTicks(universe, 1);
            var feederStart = planet.Grid.OccupantAt(8, 5);
            for (var tick = 0; tick < 600; tick++)
            {
                planet.Belts.TryInsertOnTile(feederStart, 0, ItemId.Coal);
                planet.Belts.TryInsertOnTile(feederStart, 1, ItemId.Coal);
                universe.Step();
            }

            // The feeder sits on the main line's north side, which is its left: lane 0.
            var lane0 = 0;
            var lane1 = 0;
            foreach (var segment in planet.Belts.Segments)
            {
                if (planet.Grid.OccupantAt(13, 10) == segment.TileSlots[segment.TileCount - 1])
                {
                    if (segment.Lane == 0)
                    {
                        lane0 += segment.Count;
                    }
                    else
                    {
                        lane1 += segment.Count;
                    }
                }
            }

            Console.WriteLine($"   main line items: left lane {lane0}, right lane {lane1}");
            Check(lane0 > 0 && lane1 == 0, "both feeder lanes merged onto the main line's near lane");
            Check(planet.Belts.ShapeOf(planet.Grid.OccupantAt(8, 10)) == Simulation.Belts.BeltShape.Straight, "a belt fed from behind and the side stays straight");
        }

        private static void CurveCarriesBothLanes()
        {
            var (universe, planet, _) = NewPlanet();
            for (var x = 4; x < 8; x++)
            {
                planet.TryPlace(BuildingKind.Belt, x, 10, Direction.East);
            }

            // Turns south at x=8: nothing feeds (8,10) from behind, one belt from its side.
            for (var y = 10; y < 15; y++)
            {
                planet.TryPlace(BuildingKind.Belt, 8, y, Direction.South);
            }

            RunTicks(universe, 1);
            Check(planet.Belts.ShapeOf(planet.Grid.OccupantAt(8, 10)) != Simulation.Belts.BeltShape.Straight, "the corner tile is a curve");
            Check(planet.Belts.SegmentCount == 2, "a run through a curve is one segment per lane");

            var start = planet.Grid.OccupantAt(4, 10);
            for (var tick = 0; tick < 400; tick++)
            {
                planet.Belts.TryInsertOnTile(start, 0, ItemId.Gear);
                planet.Belts.TryInsertOnTile(start, 1, ItemId.Wire);
                universe.Step();
            }

            var gears = planet.Belts.CountItems(ItemId.Gear);
            var wires = planet.Belts.CountItems(ItemId.Wire);
            Console.WriteLine($"   gears {gears}, wire {wires}");
            Check(gears > 20 && wires > 20, "both lanes carried items around the corner");
        }

        private static void NoPowerNoProgress()
        {
            var (universe, planet, probe) = NewPlanet();
            planet.FillOre(4, 4, 2, 2, ItemId.IronOre, 100);
            planet.TryPlace(BuildingKind.Drill, 4, 4, Direction.East);
            planet.TryPlace(BuildingKind.Chest, 6, 4, Direction.East);
            RunTicks(universe, 600);
            Check(Layouts.CountIn(planet, 6, 4, ItemId.IronOre) == 0, "an unpowered drill mines nothing");

            // Add a generator with an empty coal chest: it runs on its starter charge, then stops.
            planet.TryPlace(BuildingKind.Generator, 4, 7, Direction.North);
            planet.TryPlace(BuildingKind.Pole, 6, 6, Direction.North);
            RunTicks(universe, 3_000);
            var afterCharge = Layouts.CountIn(planet, 6, 4, ItemId.IronOre);
            RunTicks(universe, 1_200);
            var later = Layouts.CountIn(planet, 6, 4, ItemId.IronOre);
            Console.WriteLine($"   ore mined on the starter charge: {afterCharge}, after another 20 s: {later}");
            Check(afterCharge > 0, "the starter charge powers some mining");
            Check(later == afterCharge, "once the charge is spent, mining stops");
        }

        private static void DeconstructAndReplace()
        {
            var (universe, planet, probe) = NewPlanet();
            Layouts.StampSmeltingModule(planet, 4, 4, 5_000);
            RunTicks(universe, 900);

            var furnaceX = 4 + 9;
            Check(planet.TryRemoveAt(furnaceX, 4), "furnace accepted for removal");
            RunTicks(universe, 120);
            Check(probe.IntegrityErrors == 0, "no integrity errors after removal");
            Check(probe.EntitiesByKind.GetValueOrDefault(BuildingKind.Furnace) == 1, "one furnace entity left");

            Check(planet.TryPlace(BuildingKind.Chest, furnaceX, 4, Direction.East) == PlacementResult.Placed, "a chest fits where the furnace was");
            RunTicks(universe, 1_200);
            var ore = Layouts.CountIn(planet, furnaceX, 4, ItemId.IronOre);
            Console.WriteLine($"   ore delivered into the replacement chest: {ore}");
            Check(ore > 0, "the upstream inserter re-wired itself to the new chest");
            Check(probe.IntegrityErrors == 0 && probe.EntitiesSeen == planet.Buildings.AliveCount, "entities agree with the building table");
        }

        private static void MultiRemovalIntegrity()
        {
            var (universe, planet, probe) = NewPlanet();
            for (var x = 2; x < 30; x++)
            {
                planet.TryPlace(BuildingKind.Belt, x, 5, Direction.East);
                planet.TryPlace(BuildingKind.Chest, x, 8, Direction.East);
            }

            RunTicks(universe, 2);
            // Includes the last two entities: if removals were applied in ascending order, the earlier ones
            // would swap those out of range before their own turn, which is what corrupts swap-remove.
            foreach (var x in (int[])[3, 7, 11, 28, 29])
            {
                planet.TryRemoveAt(x, 5);
                planet.TryRemoveAt(x, 8);
            }

            RunTicks(universe, 2);
            Check(probe.IntegrityErrors == 0, "no duplicated, dead or mismatched entities");
            Check(probe.EntitiesByKind.GetValueOrDefault(BuildingKind.Belt) == 23 && probe.EntitiesByKind.GetValueOrDefault(BuildingKind.Chest) == 23,
                "exactly the right 23 belts and 23 chests survive");
        }

        private static void RecipeChange()
        {
            var (universe, planet, probe) = NewPlanet();
            planet.TryPlace(BuildingKind.Chest, 10, 11, Direction.East);
            Layouts.Fill(planet, 10, 11, ItemId.CopperPlate, 100);
            planet.TryPlace(BuildingKind.Inserter, 11, 11, Direction.East);
            planet.TryPlace(BuildingKind.Assembler, 12, 10, Direction.East, RecipeId.Gear);
            planet.TryPlace(BuildingKind.Inserter, 15, 11, Direction.East);
            planet.TryPlace(BuildingKind.Chest, 16, 11, Direction.East);
            PowerWithGenerator(planet, 10, 14);
            planet.TryPlace(BuildingKind.Pole, 13, 13, Direction.North);

            RunTicks(universe, 600);
            Check(Layouts.CountIn(planet, 16, 11, ItemId.Wire) == 0, "a gear assembler ignores copper");

            universe.Enqueue(new Command(CommandKind.SetRecipe, planet.Index, 13, 11, Recipe: RecipeId.Wire));
            RunTicks(universe, 1_800);
            var wire = Layouts.CountIn(planet, 16, 11, ItemId.Wire);
            Console.WriteLine($"   wire made after the switch: {wire}");
            Check(wire > 0, "after the change it makes wire");
        }

        private static void SurfaceConditions()
        {
            var scheduler = new SystemScheduler();
            var universe = new Universe(scheduler);
            var planet = universe.AddSurface("planet", SurfaceKind.Planet, 32, 32);
            var ship = universe.AddSurface("ship", SurfaceKind.Orbit, 32, 32);
            planet.FillTiles(0, 0, 32, 32, TileKind.Ground);
            ship.FillTiles(4, 4, 10, 10, TileKind.Foundation);

            Check(planet.TryPlace(BuildingKind.Assembler, 2, 2, Direction.North, RecipeId.HullSegment) == PlacementResult.RecipeNotAllowedHere, "hull recipe refused on a planet");
            Check(ship.TryPlace(BuildingKind.Assembler, 5, 5, Direction.North, RecipeId.HullSegment) == PlacementResult.Placed, "hull recipe allowed on the ship");
            Check(ship.TryPlace(BuildingKind.Belt, 1, 1, Direction.North) == PlacementResult.WrongTerrain, "nothing builds off the ship's deck");
            Check(planet.TryPlace(BuildingKind.Collector, 10, 10, Direction.North) == PlacementResult.WrongSurface, "collectors are ship-only");
            Check(ship.TryPlace(BuildingKind.LaunchSilo, 9, 9, Direction.North) == PlacementResult.WrongSurface, "silos are planet-only");
            Check(planet.TryPlace(BuildingKind.Drill, 20, 20, Direction.North) == PlacementResult.NoOreUnderneath, "drills need ore");
        }

        private static void LaunchToShip()
        {
            var scheduler = new SystemScheduler();
            var universe = new Universe(scheduler);
            var planet = universe.AddSurface("planet", SurfaceKind.Planet, 48, 48);
            var ship = universe.AddSurface("ship", SurfaceKind.Orbit, 32, 32);
            Layouts.BuildStarterScene(planet, ship);

            RunTicks(universe, 60 * 60 * 3);

            var gearsOnShip = CountEverywhere(ship, ItemId.Gear);
            var ironOnShip = CountEverywhere(ship, ItemId.IronOre);
            Console.WriteLine($"   gears on the ship after 3 min: {gearsOnShip}; crushed ore made on board: {ironOnShip}; ship drifted to {ship.Frame.Position}");
            Check(gearsOnShip >= Buildings.LaunchPayload, "at least one payload of gears landed");
            Check(gearsOnShip % Buildings.LaunchPayload == 0 || universe.InTransit.Count == 0, "payloads land whole");
            Check(ironOnShip > 0, "the ship's own collector line ran while it moved");
        }

        private static void Determinism()
        {
            var first = BuildAndHash(1_000);
            var second = BuildAndHash(1_000);
            var shifted = BuildAndHash(1_001);
            Console.WriteLine($"   hashes {first:X16} / {second:X16} / one-tick-later removal {shifted:X16}");
            Check(first == second, "two runs from the same commands agree");
            Check(first != shifted, "the hash notices a command landing one tick later");

            static ulong BuildAndHash(int removalTick)
            {
                var scheduler = new SystemScheduler();
                var universe = new Universe(scheduler, includeStateHashing: true);
                var planet = universe.AddSurface("planet", SurfaceKind.Planet, 64, 48);
                var ship = universe.AddSurface("ship", SurfaceKind.Orbit, 32, 32);
                Layouts.BuildStarterScene(planet, ship);
                Layouts.StampSmeltingModule(planet, 30, 30, 2_000, ItemId.CopperOre);

                for (var tick = 0; tick < 4_000; tick++)
                {
                    if (tick == removalTick)
                    {
                        universe.Enqueue(new Command(CommandKind.Remove, planet.Index, 30 + 9, 30));
                    }

                    if (tick == 1_500)
                    {
                        universe.Enqueue(new Command(CommandKind.Place, planet.Index, 30 + 9, 30, BuildingKind.Chest, Direction.East));
                    }

                    universe.Step();
                }

                return universe.ComputeStateHash();
            }
        }
    }
}
