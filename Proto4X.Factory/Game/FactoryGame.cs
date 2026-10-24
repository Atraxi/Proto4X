using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Input;
using MonoGameLibrary.UI;
using MonoGameLibrary.World;
using Proto4X.Factory.Input;
using Proto4X.Factory.Rendering;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Data;
using System.Diagnostics;

namespace Proto4X.Factory
{
    /// <summary>
    /// The playable shell around <see cref="Universe"/>. See ../DESIGN.md.
    /// </summary>
    /// <remarks>
    /// <b>How multi-surface fits a single-world <see cref="Core"/>.</b> <c>Core</c> owns one
    /// <c>World</c> and its <c>Update</c>/<c>Draw</c> are sealed, so the game can't swap which world is
    /// simulated or drawn. Instead, <c>Core.World</c> is an empty "meta" world that carries only the
    /// HUD, and every surface is a separate GameWorld owned by the universe: ticked from
    /// <see cref="OnUpdate"/> and drawn from <see cref="OnDraw"/>, which runs just before Core's own
    /// draw of the meta world, so the HUD lands on top.
    /// </remarks>
    public sealed class FactoryGame() : Core("Factory Slice", 1280, 720, false)
    {
        private const int PlanetWidth = 240;
        private const int PlanetHeight = 180;
        private const int StressColumns = 10;
        private const int StressRows = 10;

        private readonly RenderStyle style = new();
        private Universe universe = null!;
        private Surface planet = null!;
        private BuildController controller = null!;
        private CameraController cameraController = null!;
        private Texture2D pixel = null!;

        private bool paused;
        private int stressBlocksStamped;
        private readonly Stopwatch tickTimer = new();
        private double tickMsSmoothed;

        protected override GameWorld InitializeGameWorld() => new(0, 0, 1, 1);

        protected override Camera InitializeCamera() => style.CreateCamera();

        protected override void OnInitialize()
        {
            universe = new Universe(SystemScheduler);
            planet = universe.AddSurface("planet", SurfaceKind.Planet, PlanetWidth, PlanetHeight);
            var ship = universe.AddSurface("ship", SurfaceKind.Orbit, 24, 16);
            Layouts.BuildStarterScene(planet, ship);
            controller = new BuildController(universe, UserInterfaceManager.Camera);
            cameraController = new CameraController((OrbitCamera)UserInterfaceManager.Camera, universe.Surfaces);
            // Open looking at the starter scene.
            cameraController.SetView(0, new Vector2(15, 8.5f));
        }

        protected override void LoadContent()
        {
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData([Color.White]);

            foreach (var surface in universe.Surfaces)
            {
                // World drawing is the style's; the per-surface screen overlay stays SpriteBatch.
                style.RegisterSurface(SystemScheduler, surface, controller);
                SystemScheduler.Add(surface.World, new SurfaceStatusRenderer(surface, controller, pixel));
            }

            SystemScheduler.Add(World, new HudRenderer(controller, pixel, universe.Surfaces));
        }

        /// <summary>Releases the chunk meshes' GPU buffers. The device would on exit anyway; this keeps ownership honest.</summary>
        protected override void UnloadContent()
        {
            style.Dispose();
            base.UnloadContent();
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            var keyboard = InputManager.Instance.KeyboardInfo;
            if (keyboard.IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            if (keyboard.WasKeyJustPressed(Keys.Space))
            {
                paused = !paused;
            }

            if (keyboard.WasKeyJustPressed(Keys.F5))
            {
                StampStressBlock();
            }

            controller.Update(InputManager.Instance.MouseInfo.Position);

            if (!paused)
            {
                tickTimer.Restart();
                universe.Step();
                tickTimer.Stop();
                tickMsSmoothed = tickMsSmoothed * 0.95 + tickTimer.Elapsed.TotalMilliseconds * 0.05;
            }

            // After picking and the step: see CameraController for why.
            cameraController.Update(controller.ActiveSurface, gameTime);

            Window.Title = BuildStatusLine(gameTime);
        }

        protected override void OnDraw(GameTime gameTime)
        {
            GraphicsDevice.Clear(style.Background);
            if (SpriteBatch is not null)
            {
                var surface = controller.Surface;
                style.PrepareFrame(UserInterfaceManager.Camera);
                SystemScheduler.Draw(SpriteBatch, UserInterfaceManager, surface.World, style.WorldTransform(surface));
            }
        }

        /// <summary>
        /// F5: a 10x10 block of self-powered smelting modules east of the start area, 2,800 buildings
        /// per press - the in-game version of the headless benchmark, for watching it run.
        /// </summary>
        /// <remarks>
        /// Debug exception to "commands are the only way input reaches the sim": this calls
        /// <see cref="Surface.TryPlace"/> directly, between ticks. Safe (it's outside Step), but a
        /// replay wouldn't contain it.
        /// </remarks>
        private void StampStressBlock()
        {
            const int originX = 40;
            var originY = 2 + stressBlocksStamped * StressRows * Layouts.ModuleHeight;
            if (originY + StressRows * Layouts.ModuleHeight > PlanetHeight)
            {
                return;
            }

            for (var row = 0; row < StressRows; row++)
            {
                for (var column = 0; column < StressColumns; column++)
                {
                    Layouts.StampSmeltingModule(planet, originX + column * Layouts.ModuleWidth, originY + row * Layouts.ModuleHeight, 50_000);
                }
            }

            stressBlocksStamped++;
        }

        /// <summary>Debug readout. Numbers live here rather than in the HUD.</summary>
        private string BuildStatusLine(GameTime gameTime)
        {
            var surface = controller.Surface;
            var (x, y) = controller.Hovered;
            var fps = gameTime.ElapsedGameTime.TotalSeconds > 0 ? 1.0 / gameTime.ElapsedGameTime.TotalSeconds : 0;
            return $"{surface.Name} | tick {universe.Tick}{(paused ? " PAUSED" : "")} | sim {tickMsSmoothed:F2} ms"
                + $"{(gameTime.IsRunningSlowly ? " SLOW" : "")} | {fps:F0} fps"
                + $" | buildings {surface.Buildings.AliveCount:N0} | belt items {surface.Belts.CountAllItems():N0}"
                + $" | tool {controller.Tool} {controller.Direction} recipe {controller.Recipe} | ({x},{y})"
                + $" | in flight {universe.InTransit.Count}"
                + " | 1-0,- tools  R rotate  T recipe  Q no tool  Tab surface  F5 stress  Space pause  Z/X turn  PgUp/PgDn tilt  F2 ortho  F3 iso";
        }
    }
}
