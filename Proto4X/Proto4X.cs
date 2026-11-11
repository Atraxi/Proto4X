using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Input;
using MonoGameLibrary.UI;
using MonoGameLibrary.World;
using Proto4X.Systems;

namespace Proto4X
{
    public class Proto4X() : Core("Proto4X", 1920, 1080, false)
    {
        protected override GameWorld InitializeGameWorld()
        {
            return new GameWorld(0, 0, 1000, 1000);
        }

        // Sprites are 32 world units; this frames the start area from above and a little behind.
        protected override OrbitCamera InitializeCamera() => new(minDistance: 20f, maxDistance: 1500f) { Target = new Vector3(50, 60, 0), Pitch = 1.2f, Distance = 160f };

        protected override void OnInitialize()
        {
            SystemScheduler.Add(World, new MovementSystem());
            SystemScheduler.Add(World, new PlayerInputSystem(World));
        }

        protected override void LoadContent()
        {
            var scoutId = ModelLibrary.Add(new Model3D(
            [
                new ModelBox(new Vector3(-10, -3, 0), new Vector3(12, 3, 5), new Color(190, 196, 206)),
                new ModelBox(new Vector3(12, -1.5f, 1), new Vector3(16, 1.5f, 4), new Color(140, 146, 156)),
                new ModelBox(new Vector3(-8, -11, 0), new Vector3(0, 11, 2), new Color(140, 146, 156)),
                new ModelBox(new Vector3(-13, -2, 1), new Vector3(-10, 2, 4), new Color(255, 170, 70), Emissive: true, Tinted: false),
            ]));

            World.AddEntity(EntityBuilder.CreateEntity()
                .With(new Position(new Vector2(50, 60), 0f))
                .With(new Drawable(scoutId, Color.White))
                .With(new Motion(new Vector2(20, 40), default, 1f)));
        }

        protected override void OnUpdate(GameTime gameTime)
        {
            if (InputManager.Instance.KeyboardInfo.IsKeyDown(Keys.Escape))
            {
                Exit();
            }
        }

        protected override void OnDraw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
        }
    }
}
