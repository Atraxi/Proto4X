using Microsoft.Xna.Framework;
using MonoGameLibrary.Input;
using MonoGameLibrary.Utils;
using System;

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

        public Rectangle GetVisibleScreenBoundsAABB()
        {
            return GetVisibleLocalBounds(Matrix.Identity);
        }

        /// <summary>
        /// The ground under the viewport, in the local space of a world drawn through
        /// <paramref name="worldTransform"/>: each corner goes through <see cref="Camera.ScreenToWorld"/>
        /// (so a projected camera culls correctly), then the inverse world transform. Whole units,
        /// rounded outwards.
        /// </summary>
        public Rectangle GetVisibleLocalBounds(Matrix worldTransform)
        {
            var worldToLocal = Matrix.Invert(worldTransform);
            var min = new Vector2(float.MaxValue);
            var max = new Vector2(float.MinValue);
            for (var corner = 0; corner < 4; corner++)
            {
                var screen = new Vector2(
                    corner is 1 or 2 ? currentViewport.Right : currentViewport.Left,
                    corner >= 2 ? currentViewport.Bottom : currentViewport.Top);
                var local = Vector2.Transform(Camera.ScreenToWorld(screen), worldToLocal);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            var left = (int)MathF.Floor(min.X);
            var top = (int)MathF.Floor(min.Y);
            return new Rectangle(left, top, (int)MathF.Ceiling(max.X) - left, (int)MathF.Ceiling(max.Y) - top);
        }
    }
}
