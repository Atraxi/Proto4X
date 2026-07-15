using Microsoft.Xna.Framework;
using MonoGameLibrary;
using Proto4X.Sandbox.Systems;

namespace Proto4X.Sandbox
{
    public class Sandbox() : Core("Proto4X Sandbox", 1280, 720, false)
    {
        protected override void Initialize()
        {
            SystemScheduler.Add(World, new PlaceholderSystem());

            base.Initialize();
        }

        protected override void LoadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            base.Draw(gameTime);
        }
    }
}
