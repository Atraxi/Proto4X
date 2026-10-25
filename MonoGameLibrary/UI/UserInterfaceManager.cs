using Microsoft.Xna.Framework;
using MonoGameLibrary.Input;

namespace MonoGameLibrary.UI
{
    /// <summary>What the scheduler needs from the UI layer, whatever the camera type. Games hold the typed <see cref="UserInterfaceManager{TCamera}"/>.</summary>
    public abstract class UserInterfaceManager(CameraBase camera)
    {
        private Rectangle currentViewport = Rectangle.Empty;

        public CameraBase Camera { get; } = camera;

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

        /// <summary>See <see cref="CameraBase.GetVisibleLocalBounds"/>.</summary>
        public Rectangle GetVisibleLocalBounds(Matrix worldTransform)
        {
            return Camera.GetVisibleLocalBounds(worldTransform);
        }
    }

    /// <summary>The same manager with its camera typed, so a game needs no casts.</summary>
    public sealed class UserInterfaceManager<TCamera>(TCamera camera) : UserInterfaceManager(camera) where TCamera : CameraBase
    {
        public new TCamera Camera { get; } = camera;
    }
}
