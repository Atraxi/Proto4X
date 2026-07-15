using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components;
using MonoGameLibrary.Input;
using MonoGameLibrary.Systems;
using Proto4X.Systems;

namespace Proto4X
{
    public class Proto4X() : Core("Proto4X", 1920, 1080, false)
    {
        protected override void Initialize()
        {
            SystemScheduler.Add(World, new MovementSystem());

            base.Initialize();
        }

        protected override void LoadContent()
        {
            var scout = Content.Load<Texture2D>("Ships/ship_scout_32x32");
            var scoutId = SpriteLibrary.Add(scout);
            var station = Content.Load<Texture2D>("Stations/station_core_64x64");
            var stationId = SpriteLibrary.Add(station);

            World.AddEntity(EntityBuilder.CreateEntity()
                .With(new Position(new Vector2(50, 60), float.DegreesToRadians(90)))
                .With(new Drawable(scoutId, null))
                .With(new Motion(new Vector2(20, 40), default, 1f)));
        }

        protected override void Update(GameTime gameTime)
        {
            if (InputManager.Instance.KeyboardInfo.IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            base.Draw(gameTime);
        }
    }
}
