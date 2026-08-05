using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Input;
using MonoGameLibrary.World;
using Proto4X.Sandbox.Components;
using Proto4X.Sandbox.Game;
using Proto4X.Sandbox.Rendering;
using Proto4X.Sandbox.Systems;
using System;

namespace Proto4X.Sandbox
{
    /// <summary>
    /// A slim space RTS in the Age of Empires mould - mine, build, fight, raze. See DESIGN.md for
    /// the shape of the game and ENGINE_REQUESTS.md for what the engine made awkward on the way.
    /// </summary>
    public class Sandbox() : Core("Void Command", 1280, 720, false)
    {
        private static readonly Vector2 PlayerStationLocation = new(340, 560);
        private static readonly Vector2 EnemyStationLocation = new(2060, 560);
        private const int StartingMinerals = 150;
        private const int StartingMiners = 3;

        private readonly Economy economy = new(StartingMinerals);

        private SpriteSet sprites = null!;
        private StationIndexSystem stationIndex = null!;
        private FleetCensusSystem fleetCensus = null!;
        private ProductionSystem productionSystem = null!;
        private SelectionSystem selectionSystem = null!;

        private FactionId? victor;

        protected override GameWorld InitializeGameWorld()
        {
            return new GameWorld(0, 0, 2400, 1120);
        }

        protected override void OnInitialize()
        {
            // Order matters: the index systems publish the snapshots every later system reads, and
            // input has to be turned into orders before steering acts on them.
            var asteroidIndex = new AsteroidIndexSystem();
            stationIndex = new StationIndexSystem();
            fleetCensus = new FleetCensusSystem();
            selectionSystem = new SelectionSystem(UserInterfaceManager.Camera);
            productionSystem = new ProductionSystem();

            SystemScheduler.Add(World, asteroidIndex);
            SystemScheduler.Add(World, stationIndex);
            SystemScheduler.Add(World, fleetCensus);

            SystemScheduler.Add(World, selectionSystem);
            SystemScheduler.Add(World, new MoveOrderSystem(UserInterfaceManager.Camera));
            SystemScheduler.Add(World, new BuildCommandSystem(economy));

            SystemScheduler.Add(World, new MinerSystem(asteroidIndex, stationIndex, economy));
            SystemScheduler.Add(World, new EnemyProductionAiSystem(economy, fleetCensus));
            SystemScheduler.Add(World, new EnemyFleetAiSystem(stationIndex, fleetCensus));

            SystemScheduler.Add(World, new MoveToTargetSystem());
            SystemScheduler.Add(World, new MovementSystem());

            SystemScheduler.Add(World, new CombatSystem());
            SystemScheduler.Add(World, new DeathSystem());
            SystemScheduler.Add(World, productionSystem);
        }

        protected override void LoadContent()
        {
            sprites = new SpriteSet(Content, GraphicsDevice);

            SystemScheduler.Add(World, new WeaponBeamRenderer(sprites.Pixel));
            SystemScheduler.Add(World, new SelectionOverlayRenderer(sprites.Pixel, UserInterfaceManager.Camera, selectionSystem));
            SystemScheduler.Add(World, new HealthBarRenderer(sprites.Pixel));

            World.AddEntity(ShipBlueprints.CreateStation(PlayerStationLocation, FactionId.Player, sprites.StationSpriteFor(FactionId.Player)));
            World.AddEntity(ShipBlueprints.CreateStation(EnemyStationLocation, FactionId.Enemy, sprites.StationSpriteFor(FactionId.Enemy)));

            SpawnStartingMiners(PlayerStationLocation, FactionId.Player);
            SpawnStartingMiners(EnemyStationLocation, FactionId.Enemy);

            SpawnAsteroidField(PlayerStationLocation + new Vector2(150, -230), 5, seed: 1);
            SpawnAsteroidField(PlayerStationLocation + new Vector2(230, 260), 4, seed: 2);
            SpawnAsteroidField(EnemyStationLocation + new Vector2(-150, -230), 5, seed: 3);
            SpawnAsteroidField(EnemyStationLocation + new Vector2(-230, 260), 4, seed: 4);
            SpawnAsteroidField(new Vector2(1200, 560), 7, seed: 5);
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            if (InputManager.Instance.KeyboardInfo.IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            // Systems describe the ships they finished; only code outside a running system is
            // allowed to actually create entities. See SpawnRequest.
            foreach (var spawn in productionSystem.PendingSpawns)
            {
                World.AddEntity(ShipBlueprints.CreateShip(spawn.ShipType, spawn.Location, spawn.Faction, sprites));
            }
            productionSystem.ClearPendingSpawns();

            UpdateVictoryState();
            Window.Title = BuildStatusLine();
        }

        protected override void OnDraw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(8, 10, 22));
        }

        private void SpawnStartingMiners(Vector2 stationLocation, FactionId faction)
        {
            for (var i = 0; i < StartingMiners; i++)
            {
                var angle = MathHelper.TwoPi * i / StartingMiners;
                var location = stationLocation + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 80f;
                World.AddEntity(ShipBlueprints.CreateMiner(location, faction, sprites.MinerSpriteFor(faction)));
            }
        }

        private void SpawnAsteroidField(Vector2 centre, int count, int seed)
        {
            var random = new Random(seed);
            for (var i = 0; i < count; i++)
            {
                var angle = (float)(random.NextDouble() * MathHelper.TwoPi);
                var distance = 20f + (float)random.NextDouble() * 90f;
                var location = centre + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
                var spriteId = sprites.AsteroidSpriteIds[random.Next(sprites.AsteroidSpriteIds.Length)];
                World.AddEntity(ShipBlueprints.CreateAsteroid(location, spriteId));
            }
        }

        private void UpdateVictoryState()
        {
            if (victor is not null)
            {
                return;
            }

            if (stationIndex.CountFor(FactionId.Enemy) == 0)
            {
                victor = FactionId.Player;
            }
            else if (stationIndex.CountFor(FactionId.Player) == 0)
            {
                victor = FactionId.Enemy;
            }
        }

        /// <summary>
        /// The entire HUD, squeezed into the title bar. The engine can't draw text yet, and a single
        /// always-visible status line is the most this workaround can carry - see ENGINE_REQUESTS.md.
        /// </summary>
        private string BuildStatusLine()
        {
            if (victor == FactionId.Player)
            {
                return "VICTORY - the enemy station is destroyed. Esc to quit.";
            }

            if (victor == FactionId.Enemy)
            {
                return "DEFEAT - your station is destroyed. Esc to quit.";
            }

            return $"Minerals {economy.GetMinerals(FactionId.Player)}"
                + $" | Miners {fleetCensus.MinersFor(FactionId.Player)} Fighters {fleetCensus.FightersFor(FactionId.Player)}"
                + $" | Enemy {fleetCensus.MinersFor(FactionId.Enemy)}/{fleetCensus.FightersFor(FactionId.Enemy)}"
                + $" | [1] Miner {ShipBlueprints.Miner.MineralCost}  [2] Fighter {ShipBlueprints.Fighter.MineralCost}";
        }
    }
}
