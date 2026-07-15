using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Systems;
using MonoGameLibrary.Systems.Infrastructure;
using MonoGameLibrary.UI;
using MonoGameLibrary.World;

namespace MonoGameLibrary
{
    public class Core : Game
    {
        /// <summary>
        /// Gets the graphics device manager to control the presentation of graphics.
        /// </summary>
        protected GraphicsDeviceManager Graphics { get; private set; }

        /// <summary>
        /// Gets the sprite batch used for all 2D rendering.
        /// </summary>
        protected SpriteBatch? SpriteBatch { get; private set; }

        public GameWorld World { get; private set; }

        protected UserInterfaceManager UserInterfaceManager { get; private set; }

        public static Core? Instance { get; private set; }

        public SystemScheduler SystemScheduler { get; private set; }

        /// <summary>
        /// Creates a new Core instance.
        /// </summary>
        /// <param name="title">The title to display in the title bar of the game window.</param>
        /// <param name="width">The initial width, in pixels, of the game window.</param>
        /// <param name="height">The initial height, in pixels, of the game window.</param>
        /// <param name="fullScreen">Indicates if the game should start in fullscreen mode.</param>
        public Core(string title, int width, int height, bool fullScreen)
        {
            Instance = this;
            Graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = width,
                PreferredBackBufferHeight = height,
                IsFullScreen = fullScreen,
            };
            Graphics.ApplyChanges();

            Window.Title = title;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            World = new GameWorld(0, 0, 1000, 1000);
            UserInterfaceManager = new UserInterfaceManager(World);
            SystemScheduler = new SystemScheduler();
        }

        protected override void Initialize()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);

            SystemScheduler.Add(World, new Renderer());
            SystemScheduler.Add(World, new TimerSystem());

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            UserInterfaceManager.Update(GraphicsDevice.Viewport.Bounds, gameTime);

            SystemScheduler.Update(World, gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (SpriteBatch != null)
            {
                SystemScheduler.Draw(SpriteBatch, UserInterfaceManager, World);
            }

            base.Draw(gameTime);
        }
    }
}
