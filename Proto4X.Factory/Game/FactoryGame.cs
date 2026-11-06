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
using Proto4X.Factory.UI;
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
    /// Gui, and every surface (and the space layer) is a separate GameWorld owned by the universe:
    /// ticked from <see cref="OnUpdate"/> and drawn from <see cref="OnDraw"/>, which runs just before
    /// Core's own draw of the meta world, so the Gui lands on top.
    /// </remarks>
    public sealed class FactoryGame() : Core<OrbitCamera>("Factory Slice", 1280, 720, true)
    {
        private const int PlanetWidth = 240;
        private const int PlanetHeight = 180;
        private const int StressColumns = 10;
        private const int StressRows = 10;

        private readonly RenderStyle style = new();
        private Universe universe = null!;
        private Surface planet = null!;
        private BuildController controller = null!;
        private FleetController fleet = null!;
        private CameraController cameraController = null!;
        private Texture2D pixel = null!;
        private Shaders? shaders;
        private Gui? gui;
        private GameUi? ui;
        private SpaceRenderer? spaceRenderer;

        private bool paused;
        private int stressBlocksStamped;
        private readonly Stopwatch tickTimer = new();
        private double tickMsSmoothed;

        protected override GameWorld InitializeGameWorld() => new(0, 0, 1, 1);

        protected override OrbitCamera InitializeCamera() => style.CreateCamera();

        protected override void OnInitialize()
        {
            universe = new Universe(SystemScheduler);
            planet = universe.AddSurface("planet", SurfaceKind.Planet, PlanetWidth, PlanetHeight);
            var station = universe.AddSurface("station", SurfaceKind.Orbit, 24, 16);
            Layouts.BuildStarterScene(planet, station);
            controller = new BuildController(universe, UserInterfaceManager.Camera);
            fleet = new FleetController(universe, UserInterfaceManager.Camera);
            cameraController = new CameraController(UserInterfaceManager.Camera, universe.Surfaces);
            // Open looking at the starter scene; the station pulled back to show the space around it.
            cameraController.SetView(planet.Index, new Vector2(30, 10f));
            cameraController.SetView(station.Index, new Vector2(12, 8f), 70f);
        }

        protected override void LoadContent()
        {
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData([Color.White]);

            shaders = new Shaders(Content, GraphicsDevice);
            style.Hologram = shaders.Hologram;

            foreach (var surface in universe.Surfaces)
            {
                RegisterSurface(surface);
            }

            // Warehouse interiors appear when first entered.
            universe.SurfaceAdded += RegisterSurface;

            spaceRenderer = new SpaceRenderer(universe.Space, fleet);
            SystemScheduler.Add(universe.Space.World, spaceRenderer);

            gui = new Gui(PixelFont.Create(GraphicsDevice, 2));
            ui = new GameUi(universe, gui, controller, fleet);
            SystemScheduler.Add(World, new GuiRenderer(gui, pixel));
        }

        private void RegisterSurface(Surface surface)
        {
            // World drawing is the style's; the per-surface screen overlay stays SpriteBatch.
            style.RegisterSurface(SystemScheduler, surface, controller);
            SystemScheduler.Add(surface.World, new SurfaceStatusRenderer(surface, controller, pixel));
        }

        /// <summary>Releases the chunk meshes' GPU buffers. The device would on exit anyway; this keeps ownership honest.</summary>
        protected override void UnloadContent()
        {
            style.Dispose();
            shaders?.Dispose();
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

            if (keyboard.WasKeyJustPressed(Keys.F4) && shaders is not null)
            {
                shaders.GradeEnabled = !shaders.GradeEnabled;
            }

            if (keyboard.WasKeyJustPressed(Keys.F6))
            {
                universe.Enqueue(new Command(CommandKind.SetPeaceful, -1, 0, 0, Config: universe.Space.Peaceful ? 0 : 1));
            }

            var mouse = InputManager.Instance.MouseInfo;
            var overUi = false;
            if (gui is not null && ui is not null)
            {
                if (keyboard.WasKeyJustPressed(Keys.F))
                {
                    if (ui.Mode == InputMode.Fleet) ui.Mode = InputMode.Build; else ui.EnterFleetMode();
                }

                if (keyboard.WasKeyJustPressed(Keys.B))
                {
                    ui.Mode = ui.Mode == InputMode.Designer ? InputMode.Build : InputMode.Designer;
                }

                if (ui.Mode == InputMode.Fleet && keyboard.WasKeyJustPressed(Keys.A))
                {
                    fleet.SelectAll();
                }

                // The Gui goes first so world input below can skip the mouse when it is over a panel.
                gui.Begin(mouse, GraphicsDevice.Viewport.Bounds);
                ui.Build();
                gui.End();
                overUi = gui.WantsMouse;
            }

            var mode = ui?.Mode ?? InputMode.Build;
            // The designer owns R, so building only reads keys outside it.
            if (mode != InputMode.Designer)
            {
                controller.Update(mouse.Position, overUi, mode == InputMode.Build);
            }

            if (mode == InputMode.Fleet)
            {
                fleet.Update(mouse, overUi);
            }

            if (!paused)
            {
                tickTimer.Restart();
                universe.Step();
                tickTimer.Stop();
                tickMsSmoothed = tickMsSmoothed * 0.95 + tickTimer.Elapsed.TotalMilliseconds * 0.05;
            }

            // After picking and the step: see CameraController for why.
            cameraController.Update(controller.ActiveSurface, gameTime);

            if (spaceRenderer is not null)
            {
                spaceRenderer.Tick = universe.Tick;
                spaceRenderer.FrameSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            }

            Window.Title = BuildStatusLine(gameTime);
        }

        protected override void OnDraw(GameTime gameTime)
        {
            if (SpriteBatch is null || shaders is null)
            {
                GraphicsDevice.Clear(style.Background);
                return;
            }

            var surface = controller.Surface;
            var time = (float)gameTime.TotalGameTime.TotalSeconds;
            SystemScheduler.Geometry.Time = time;
            shaders.BeginFrame(SpriteBatch, GraphicsDevice, surface.Kind == SurfaceKind.Orbit, style.Background, UserInterfaceManager.Camera, time);
            style.PrepareFrame(UserInterfaceManager.Camera);
            SystemScheduler.Draw(SpriteBatch, UserInterfaceManager, surface.World, style.WorldTransform(surface));
            if (surface == universe.Station)
            {
                // Space coordinates are camera world coordinates: no transform.
                SystemScheduler.Draw(SpriteBatch, UserInterfaceManager, universe.Space.World, Matrix.Identity);
            }
            shaders.EndFrame(SpriteBatch, time);
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
            const int originX = 60; // east of the starter scene
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
                + " | 1-0,-,= tools  Q inspect  R rotate  T recipe  E enter/leave  Tab surface  F fleet  B designer  F6 peaceful  F5 stress  Space pause  F4 grade  Z/X turn  PgUp/PgDn tilt  F2 ortho  F3 iso";
        }
    }
}
