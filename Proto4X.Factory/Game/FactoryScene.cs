using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;

namespace Proto4X.Factory
{
    /// <summary>
    /// The opening scene, shared by the game and the headless snapshots so both show the same thing.
    /// </summary>
    public static class FactoryScene
    {
        public const int PlanetWidth = 240;
        public const int PlanetHeight = 180;
        public const int StressColumns = 10;
        public const int StressRows = 10;

        public static Universe Create(SystemScheduler scheduler, out Surface planet, out Surface ship)
        {
            var universe = new Universe(scheduler);
            planet = universe.AddSurface("planet", SurfaceKind.Planet, PlanetWidth, PlanetHeight);
            ship = universe.AddSurface("ship", SurfaceKind.Orbit, 24, 16);
            Layouts.BuildStarterScene(planet, ship);
            AddShowcase(planet);
            return universe;
        }

        /// <summary>
        /// Game-only additions to the starter scene, so a style has more to show: a copper smelting
        /// module under the iron one, and a belt loop (four curves) fed plates from a chest. Kept out
        /// of <see cref="Layouts.BuildStarterScene"/>, which the headless checks depend on.
        /// </summary>
        private static void AddShowcase(Surface planet)
        {
            Layouts.StampSmeltingModule(planet, 2, 10, 20_000, ItemId.CopperOre);

            const int left = 21, top = 6, right = 27, bottom = 11;
            for (var x = left; x < right; x++)
            {
                planet.TryPlace(BuildingKind.Belt, x, top, Direction.East);
            }

            for (var y = top; y < bottom; y++)
            {
                planet.TryPlace(BuildingKind.Belt, right, y, Direction.South);
            }

            for (var x = right; x > left; x--)
            {
                planet.TryPlace(BuildingKind.Belt, x, bottom, Direction.West);
            }

            for (var y = bottom; y > top; y--)
            {
                planet.TryPlace(BuildingKind.Belt, left, y, Direction.North);
            }

            planet.TryPlace(BuildingKind.Chest, 19, 8, Direction.East);
            Layouts.Fill(planet, 19, 8, ItemId.IronPlate, 1_600);
            planet.TryPlace(BuildingKind.Inserter, 20, 8, Direction.East);
            planet.TryPlace(BuildingKind.Pole, 19, 7, Direction.North);
        }

        /// <summary>
        /// A 10x10 block of self-powered smelting modules east of the start area, 2,800 buildings per
        /// block - the in-game version of the headless benchmark. Returns false once the planet is full.
        /// </summary>
        /// <remarks>
        /// Debug exception to "commands are the only way input reaches the sim": this calls
        /// <see cref="Surface.TryPlace"/> directly, between ticks. Safe (it's outside Step), but a
        /// replay wouldn't contain it.
        /// </remarks>
        public static bool StampStressBlock(Surface planet, int blockIndex)
        {
            const int originX = 40;
            var originY = 2 + blockIndex * StressRows * Layouts.ModuleHeight;
            if (originY + StressRows * Layouts.ModuleHeight > PlanetHeight)
            {
                return false;
            }

            for (var row = 0; row < StressRows; row++)
            {
                for (var column = 0; column < StressColumns; column++)
                {
                    var ore = (row + column) % 3 == 0 ? ItemId.CopperOre : ItemId.IronOre;
                    Layouts.StampSmeltingModule(planet, originX + column * Layouts.ModuleWidth, originY + row * Layouts.ModuleHeight, 50_000, ore);
                }
            }

            return true;
        }
    }
}
