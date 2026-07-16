using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components;
using MonoGameLibrary.Input;
using Proto4X.Sandbox.Components;
using Proto4X.Sandbox.Systems;

namespace Proto4X.Sandbox
{
    public class Sandbox() : Core("Proto4X Sandbox", 1280, 720, false)
    {
        // The player ship is spawned first, so it's guaranteed entity id 0 — matching
        // UserInterfaceManager's own hardcoded selectedEntityId default.
        private const int PlayerEntityId = 0;
        private const float GatherRadius = 40f;

        private GatherSystem gatherSystem = null!;

        protected override void Initialize()
        {
            SystemScheduler.Add(World, new MovementSystem());
            gatherSystem = new GatherSystem(PlayerEntityId, GatherRadius);
            SystemScheduler.Add(World, gatherSystem);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            var scout = Content.Load<Texture2D>("Ships/ship_scout_32x32");
            var scoutId = SpriteLibrary.Add(scout);
            var rock = Content.Load<Texture2D>("Resources/rock1_24x24");
            var rockId = SpriteLibrary.Add(rock);

            World.AddEntity(EntityBuilder.CreateEntity()
                .With(new Position(new Vector2(640, 360)))
                .With(new Drawable(scoutId, null))
                .With(new Motion(Vector2.Zero)));

            Vector2[] resourceNodeLocations =
            [
                new(300, 200),
                new(950, 200),
                new(300, 550),
                new(950, 550),
                new(640, 150),
                new(640, 570),
            ];

            foreach (var location in resourceNodeLocations)
            {
                World.AddEntity(EntityBuilder.CreateEntity()
                    .With(new Position(location))
                    .With(new Drawable(rockId, null))
                    .With(new Gatherable(10)));
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (InputManager.Instance.KeyboardInfo.IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            base.Update(gameTime);

            Window.Title = $"Proto4X Sandbox — Stockpile: {gatherSystem.Stockpile}";
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            base.Draw(gameTime);
        }
    }
}
