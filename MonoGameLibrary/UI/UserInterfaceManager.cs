using Microsoft.Xna.Framework;
using MonoGameLibrary.Input;
using MonoGameLibrary.Utils;

namespace MonoGameLibrary.UI
{
    public class UserInterfaceManager(Camera camera)
    {
        private Rectangle currentViewport = Rectangle.Empty;

        public Camera Camera { get; internal set; } = camera;

        public void Update(Rectangle viewportBounds, GameTime gameTime)
        {
            InputManager.Instance.Update();
            currentViewport = viewportBounds;

            Camera.Update(viewportBounds);
        }

        public Rectangle GetViewport()
        {
            return new Rectangle(
                currentViewport.X,
                currentViewport.Y,
                currentViewport.Width,
                currentViewport.Height);
        }

        /// <summary>See <see cref="Camera.GetVisibleLocalBounds"/>.</summary>
        public Rectangle GetVisibleLocalBounds(Matrix worldTransform)
        {
            return Camera.GetVisibleLocalBounds(worldTransform);
        }
    }
}
