using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;

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
        /// The game's opening scene: a smelting module, a gear assembler fed from its plates, and a
        /// launch silo, plus a ship carrying a receiver, a collector line and a hull assembler.
        /// </summary>
        public static void BuildStarterScene(Surface planet, Surface ship)
        {
            planet.FillTiles(0, 0, planet.Grid.Width, planet.Grid.Height, TileKind.Ground);
            planet.FillOre(4, 20, 8, 6, ItemId.CopperOre, 5_000);
            planet.FillOre(14, 20, 6, 6, ItemId.Coal, 5_000);
            StampSmeltingModule(planet, 2, 2, 20_000);

            // Plates from the module's top chest go north onto a belt, into a gear assembler, and
            // the gears into a launch silo that ships them to the ship's receiver.
            planet.TryPlace(BuildingKind.Inserter, 14, 1, Direction.North);
            for (var x = 14; x <= 18; x++)
            {
                planet.TryPlace(BuildingKind.Belt, x, 0, Direction.East);
            }

            planet.TryPlace(BuildingKind.Inserter, 19, 0, Direction.East);
            planet.TryPlace(BuildingKind.Assembler, 20, 0, Direction.East, RecipeId.Gear);
            planet.TryPlace(BuildingKind.Inserter, 23, 1, Direction.East);
            planet.TryPlace(BuildingKind.LaunchSilo, 24, 0, Direction.East);
            planet.TryPlace(BuildingKind.Pole, 13, 1, Direction.North);
            planet.TryPlace(BuildingKind.Pole, 18, 2, Direction.North);
            planet.TryPlace(BuildingKind.Pole, 23, 3, Direction.North);

            // The ship: a small deck in its own grid, which the frame carries around.
            ship.FillTiles(2, 2, 20, 12, TileKind.Foundation);
            ship.TryPlace(BuildingKind.Receiver, 3, 3, Direction.North);
            ship.TryPlace(BuildingKind.Inserter, 5, 3, Direction.East);
            ship.TryPlace(BuildingKind.Belt, 6, 3, Direction.East);
            ship.TryPlace(BuildingKind.Belt, 7, 3, Direction.East);
            ship.TryPlace(BuildingKind.Inserter, 8, 3, Direction.East);
            ship.TryPlace(BuildingKind.Chest, 9, 3, Direction.East);

            ship.TryPlace(BuildingKind.Collector, 3, 8, Direction.East);
            ship.TryPlace(BuildingKind.Belt, 5, 8, Direction.East);
            ship.TryPlace(BuildingKind.Belt, 6, 8, Direction.East);
            ship.TryPlace(BuildingKind.Inserter, 7, 8, Direction.East);
            ship.TryPlace(BuildingKind.Assembler, 8, 7, Direction.East, RecipeId.CrushChunk);
            ship.TryPlace(BuildingKind.Inserter, 11, 8, Direction.East);
            ship.TryPlace(BuildingKind.Chest, 12, 8, Direction.East);

            ship.TryPlace(BuildingKind.Generator, 16, 3, Direction.North);
            ship.TryPlace(BuildingKind.Inserter, 16, 5, Direction.North);
            ship.TryPlace(BuildingKind.Chest, 16, 6, Direction.North);
            Fill(ship, 16, 6, ItemId.Coal, 400);
            foreach (var (x, y) in ((int, int)[])[(5, 5), (6, 7), (8, 5), (12, 6), (15, 5)])
            {
                ship.TryPlace(BuildingKind.Pole, x, y, Direction.North);
            }

            // Placed at an angle and drifting, to prove the factory doesn't care.
            ship.Frame.Position = new Microsoft.Xna.Framework.Vector2(34f, 2f);
            ship.Frame.Rotation = 0.15f;
            ship.Frame.AngularVelocity = 0.0004f;
            ship.Frame.Velocity = new Microsoft.Xna.Framework.Vector2(0.002f, 0.001f);
        }
    }
}
