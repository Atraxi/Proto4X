using Microsoft.Xna.Framework;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;
using System;

namespace Proto4X.Factory.Simulation
{
    /// <summary>
    /// Prefabricated layouts, shared by the headless checks, the benchmark and the game's starting
    /// scene and stress stamp. Placement goes through <see cref="Surface.TryPlace"/> like any player
    /// build, so a layout is only ever a list of ordinary commands.
    /// </summary>
    public static class Layouts
    {
        public const int ModuleWidth = 18;
        public const int ModuleHeight = 7;

        /// <summary>
        /// A self-powered smelting module: two drill → belt → inserter → furnace → inserter → chest
        /// lines, a generator fed from a coal chest, and three poles. 28 buildings, all placed on
        /// fresh ground and ore laid under the drills.
        /// </summary>
        /// <remarks>
        /// <code>
        ///   x: 0 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15
        ///   0  D D = = = = = = i F F  i  C     G  G
        ///   1  D D             . F F           G  G
        ///   2      P               P        P  i
        ///   3  D D = = = = = = i F F  i  C     K
        ///   4  D D               F F
        /// </code>
        /// D drill, = belt, i inserter, F furnace, C chest, P pole, G generator, K coal chest.
        /// </remarks>
        public static void StampSmeltingModule(Surface surface, int ox, int oy, int orePerTile, ItemId ore = ItemId.IronOre)
        {
            surface.FillTiles(ox, oy, ModuleWidth, ModuleHeight, surface.Kind == SurfaceKind.Planet ? TileKind.Ground : TileKind.Foundation);

            foreach (var row in (int[])[0, 3])
            {
                var y = oy + row;
                surface.FillOre(ox, y, 2, 2, ore, orePerTile);
                surface.TryPlace(BuildingKind.Drill, ox, y, Direction.East);
                for (var x = ox + 2; x <= ox + 7; x++)
                {
                    surface.TryPlace(BuildingKind.Belt, x, y, Direction.East);
                }

                surface.TryPlace(BuildingKind.Inserter, ox + 8, y, Direction.East);
                surface.TryPlace(BuildingKind.Furnace, ox + 9, y, Direction.East);
                surface.TryPlace(BuildingKind.Inserter, ox + 11, y, Direction.East);
                surface.TryPlace(BuildingKind.Chest, ox + 12, y, Direction.East);
            }

            surface.TryPlace(BuildingKind.Generator, ox + 14, oy, Direction.North);
            surface.TryPlace(BuildingKind.Inserter, ox + 14, oy + 2, Direction.North);
            surface.TryPlace(BuildingKind.Chest, ox + 14, oy + 3, Direction.North);
            Fill(surface, ox + 14, oy + 3, ItemId.Coal, 200);

            surface.TryPlace(BuildingKind.Pole, ox + 2, oy + 2, Direction.North);
            surface.TryPlace(BuildingKind.Pole, ox + 9, oy + 2, Direction.North);
            surface.TryPlace(BuildingKind.Pole, ox + 13, oy + 2, Direction.North);
        }

        public const int BuildingsPerModule = 28;

        /// <summary>Puts items straight into the inventory of whatever is on the tile. Setup only.</summary>
        public static int Fill(Surface surface, int x, int y, ItemId item, int count)
        {
            var slot = surface.Grid.OccupantAt(x, y);
            if (slot < 0)
            {
                return 0;
            }

            var start = surface.Buildings.InventoryStart(slot);
            var length = surface.Buildings.InventoryLength(slot);
            var inserted = 0;
            while (inserted < count && surface.Inventories.TryInsert(start, length, item, 1))
            {
                inserted++;
            }

            return inserted;
        }

        /// <summary>Total of one item across a building's inventory. Diagnostics only.</summary>
        public static int CountIn(Surface surface, int x, int y, ItemId item)
        {
            var slot = surface.Grid.OccupantAt(x, y);
            if (slot < 0)
            {
                return 0;
            }

            return surface.Inventories.CountOf(surface.Buildings.InventoryStart(slot), surface.Buildings.InventoryLength(slot), item);
        }

        /// <summary>
        /// A Factorio smelter row: drills facing south onto an ore belt along <paramref name="y"/>,
        /// a column of furnaces fed from it, and a plate belt two tiles below the furnaces, all heading
        /// east. Powered by its own coal generator; the plate belt ends at <paramref name="beltEnd"/>.
        /// </summary>
        /// <remarks>
        /// <code>
        ///   y-2  D D D D ...                     drills on ore, output onto the belt below
        ///   y    = = = = = = = = = = = = =       ore belt
        ///   y+1      P    P    P   i P i   i P   inserters into furnaces, poles
        ///   y+2            G G i K F F F F F F
        ///   y+3            G G P   F F F F F F
        ///   y+4                    i P i   i P   inserters out of furnaces
        ///   y+5                    = = = = = = = = ...   plate belt
        /// </code>
        /// </remarks>
        public static void StampSmelterRow(Surface surface, int y, int drills, int furnaces, ItemId ore, int beltEnd)
        {
            const int furnaceX = 16;
            surface.FillOre(2, y - 2, drills * 2, 2, ore, 50_000);
            for (var d = 0; d < drills; d++)
            {
                surface.TryPlace(BuildingKind.Drill, 2 + d * 2, y - 2, Direction.South);
            }

            var lastFurnace = furnaceX + furnaces * 2 - 1;
            for (var x = 2; x <= lastFurnace; x++)
            {
                surface.TryPlace(BuildingKind.Belt, x, y, Direction.East);
            }

            for (var f = 0; f < furnaces; f++)
            {
                var x = furnaceX + f * 2;
                surface.TryPlace(BuildingKind.Inserter, x, y + 1, Direction.South);
                surface.TryPlace(BuildingKind.Furnace, x, y + 2, Direction.South);
                surface.TryPlace(BuildingKind.Inserter, x, y + 4, Direction.South);
            }

            for (var x = furnaceX; x <= beltEnd; x++)
            {
                surface.TryPlace(BuildingKind.Belt, x, y + 5, Direction.East);
            }

            surface.TryPlace(BuildingKind.Generator, 10, y + 2, Direction.North);
            surface.TryPlace(BuildingKind.Inserter, 12, y + 2, Direction.West);
            surface.TryPlace(BuildingKind.Chest, 13, y + 2, Direction.North);
            Fill(surface, 13, y + 2, ItemId.Coal, 800);
            surface.TryPlace(BuildingKind.Pole, 12, y + 3, Direction.North);
            foreach (var x in (int[])[4, 9, 14])
            {
                surface.TryPlace(BuildingKind.Pole, x, y + 1, Direction.North);
            }

            for (var x = furnaceX + 1; x <= lastFurnace + 1; x += 4)
            {
                surface.TryPlace(BuildingKind.Pole, x, y + 1, Direction.North);
                surface.TryPlace(BuildingKind.Pole, x, y + 4, Direction.North);
            }
        }

        /// <summary>
        /// The game's opening scene (plans/03): iron and copper smelter rows, steel furnaces off the
        /// iron belt, two pre-built warehouses (gears and circuits; ship parts) chained into a launch
        /// silo, and a solar-powered station whose shipyard mass-produces Pickets. Adds the two
        /// blueprints to the universe and puts the station, the bastion and two Pickets into space.
        /// </summary>
        /// <remarks>
        /// <code>
        ///   x:   16..25        28..31      34 35 36..41 42 43..48 49 50..52
        ///   y4                 = = = = = = = = = = = = = = = = = v           steel belt
        ///   y5                 i P i                            i  P
        ///   y6-7 furnaces      S S S S           W1           i    W2         silo
        ///   y8   i P i P       i P i         P   (gears and   P  (ship parts) i
        ///   y9   = iron plates = = = = = = = > i  circuits)   i               P
        ///   y12                                 P i               P i
        ///   y13                                 > = copper plates = = =
        /// </code>
        /// </remarks>
        public static void BuildStarterScene(Surface planet, Surface station)
        {
            var universe = planet.Universe;
            planet.FillTiles(0, 0, planet.Grid.Width, planet.Grid.Height, TileKind.Ground);

            // Iron: 6 drills, 5 furnaces; copper: 4 drills, 3 furnaces.
            StampSmelterRow(planet, 4, drills: 6, furnaces: 5, ItemId.IronOre, beltEnd: 34);
            StampSmelterRow(planet, 14, drills: 4, furnaces: 3, ItemId.CopperOre, beltEnd: 33);

            // Steel furnaces above the iron plate belt; steel belt along y=4 to the ship-parts warehouse.
            foreach (var x in (int[])[28, 30])
            {
                planet.TryPlace(BuildingKind.Inserter, x, 8, Direction.North);
                planet.TryPlace(BuildingKind.Furnace, x, 6, Direction.North);
                planet.TryPlace(BuildingKind.Inserter, x, 5, Direction.North);
            }

            for (var x = 28; x <= 43; x++)
            {
                planet.TryPlace(BuildingKind.Belt, x, 4, Direction.East);
            }

            planet.TryPlace(BuildingKind.Belt, 44, 4, Direction.South);
            planet.TryPlace(BuildingKind.Inserter, 44, 5, Direction.South);

            // Copper plates: up the side at x=34, then east under both warehouses.
            for (var y = 19; y >= 14; y--)
            {
                planet.TryPlace(BuildingKind.Belt, 34, y, Direction.North);
            }

            for (var x = 34; x <= 46; x++)
            {
                planet.TryPlace(BuildingKind.Belt, x, 13, Direction.East);
            }

            var gearsAndCircuits = universe.AddBlueprint("Gears & circuits", GearsAndCircuits);
            var shipParts = universe.AddBlueprint("Ship parts", ShipParts);
            planet.TryPlace(BuildingKind.Warehouse, 36, 6, Direction.North, config: gearsAndCircuits.Id);
            planet.TryPlace(BuildingKind.Warehouse, 43, 6, Direction.North, config: shipParts.Id);
            planet.TryPlace(BuildingKind.Inserter, 35, 9, Direction.East);   // iron into W1
            planet.TryPlace(BuildingKind.Inserter, 37, 12, Direction.North); // copper into W1
            planet.TryPlace(BuildingKind.Inserter, 45, 12, Direction.North); // copper into W2
            planet.TryPlace(BuildingKind.Inserter, 42, 7, Direction.East);   // gears and circuits, W1 to W2
            planet.TryPlace(BuildingKind.Inserter, 42, 9, Direction.East);
            planet.TryPlace(BuildingKind.LaunchSilo, 50, 7, Direction.North);
            planet.TryPlace(BuildingKind.Inserter, 49, 8, Direction.East);   // ship parts into the silo

            // The warehouses' own power plant, wired into the iron row's grid.
            planet.TryPlace(BuildingKind.Generator, 50, 11, Direction.North);
            planet.TryPlace(BuildingKind.Inserter, 52, 11, Direction.West);
            planet.TryPlace(BuildingKind.Chest, 53, 11, Direction.North);
            Fill(planet, 53, 11, ItemId.Coal, 800);
            foreach (var (x, y) in ((int, int)[])[(29, 5), (29, 8), (35, 8), (42, 8), (43, 5), (49, 6), (36, 12), (44, 12), (52, 12)])
            {
                planet.TryPlace(BuildingKind.Pole, x, y, Direction.North);
            }

            // The station: receiver -> inserter -> shipyard, on solar.
            station.FillTiles(1, 1, Math.Min(20, station.Grid.Width - 2), Math.Min(10, station.Grid.Height - 2), TileKind.Foundation);
            station.TryPlace(BuildingKind.Receiver, 3, 3, Direction.North);
            station.TryPlace(BuildingKind.Inserter, 5, 3, Direction.East);
            station.TryPlace(BuildingKind.Shipyard, 6, 2, Direction.North, config: Systems.ShipyardOrder.Pack(0, Systems.ShipyardMode.Repeat));
            for (var x = 12; x <= 18; x += 2)
            {
                station.TryPlace(BuildingKind.SolarPanel, x, 2, Direction.North);
                station.TryPlace(BuildingKind.SolarPanel, x, 5, Direction.North);
            }

            foreach (var (x, y) in ((int, int)[])[(5, 5), (9, 6), (13, 4), (17, 4)])
            {
                station.TryPlace(BuildingKind.Pole, x, y, Direction.North);
            }

            station.Frame.Position = new Vector2(60f, 150f);
            universe.SetUpSpace(new Vector2(420f, 160f));
            universe.SpawnPlayerShip(universe.Designs[0]!, universe.Space.StationPosition + new Vector2(14f, -6f));
            universe.SpawnPlayerShip(universe.Designs[0]!, universe.Space.StationPosition + new Vector2(14f, 6f));
        }

        // ---------------------------------------------------------------- the prototype loop (plans/03)

        private static Warehouses.BlueprintEntry E(BuildingKind kind, int x, int y, Direction direction = Direction.North, RecipeId recipe = RecipeId.None, ItemId dock = ItemId.None) =>
            new(kind, x, y, direction, recipe, (int)dock);

        private static Warehouses.BlueprintEntry In(int x, int y, ItemId item) => E(BuildingKind.InputDock, x, y, dock: item);

        private static Warehouses.BlueprintEntry Out(int x, int y) => E(BuildingKind.OutputDock, x, y);

        private static Warehouses.BlueprintEntry Ins(int x, int y, Direction direction) => E(BuildingKind.Inserter, x, y, direction);

        private static Warehouses.BlueprintEntry Asm(int x, int y, RecipeId recipe) => E(BuildingKind.Assembler, x, y, Direction.East, recipe);

        /// <summary>Plates in, gears and circuits out. Wire is made and used inside, so it never shows on the outside.</summary>
        public static Warehouses.BlueprintEntry[] GearsAndCircuits =>
        [
            In(0, 1, ItemId.IronPlate), Ins(1, 1, Direction.East), Asm(2, 0, RecipeId.Gear), Ins(5, 1, Direction.East), Out(6, 1),
            In(0, 5, ItemId.CopperPlate), Ins(1, 5, Direction.East), Asm(2, 4, RecipeId.Wire), Ins(5, 5, Direction.East),
            Asm(6, 4, RecipeId.Circuit), In(7, 8, ItemId.IronPlate), Ins(7, 7, Direction.North), Ins(9, 5, Direction.East), Out(10, 5),
        ];

        /// <summary>Steel, gears and circuits in; frames and engines out.</summary>
        public static Warehouses.BlueprintEntry[] FramesAndEngines =>
        [
            In(0, 0, ItemId.Steel), Ins(1, 0, Direction.East), In(0, 2, ItemId.Gear), Ins(1, 2, Direction.East),
            Asm(2, 0, RecipeId.Frame), Ins(5, 1, Direction.East), Out(6, 1),
            In(0, 5, ItemId.Steel), Ins(1, 5, Direction.East), In(0, 6, ItemId.Gear), Ins(1, 6, Direction.East), In(0, 7, ItemId.Circuit), Ins(1, 7, Direction.East),
            Asm(2, 5, RecipeId.Engine), Ins(5, 6, Direction.East), Out(6, 6),
        ];

        /// <summary>The second starter warehouse: <see cref="FramesAndEngines"/> on top, <see cref="Electronics"/> 12 tiles below it.</summary>
        public static Warehouses.BlueprintEntry[] ShipParts => [.. FramesAndEngines, .. Array.ConvertAll(Electronics, e => e with { Y = e.Y + 12 })];

        /// <summary>
        /// Circuits, copper and steel in; processors, power cells and emitters out, as three separate
        /// lines. Emitters get their own power-cell assembler: fed from the output line instead, a
        /// backed-up emitter would stop power cells too (one island, oil-refinery rule).
        /// </summary>
        public static Warehouses.BlueprintEntry[] Electronics =>
        [
            In(0, 1, ItemId.CopperPlate), Ins(1, 1, Direction.East), Asm(2, 0, RecipeId.Wire), Ins(5, 1, Direction.East),
            Asm(6, 0, RecipeId.Processor), In(7, 4, ItemId.Circuit), Ins(7, 3, Direction.North), Ins(9, 1, Direction.East), Out(10, 1),
            In(0, 6, ItemId.Circuit), Ins(1, 6, Direction.East), In(0, 8, ItemId.CopperPlate), Ins(1, 8, Direction.East),
            Asm(2, 6, RecipeId.PowerCell), Ins(5, 7, Direction.East), Out(6, 7),
            In(10, 6, ItemId.Circuit), Ins(11, 6, Direction.East), In(10, 8, ItemId.CopperPlate), Ins(11, 8, Direction.East),
            Asm(12, 6, RecipeId.PowerCell), Ins(15, 7, Direction.East),
            Asm(16, 6, RecipeId.Emitter), In(16, 4, ItemId.Steel), Ins(16, 5, Direction.South), In(17, 10, ItemId.Circuit), Ins(17, 9, Direction.North),
            Ins(19, 7, Direction.East), Out(20, 7),
        ];
    }
}
