using Microsoft.Xna.Framework;

namespace MonoGameLibrary.UI
{
    /// <summary>
    /// What every camera offers the engine: the matrices the geometry pass draws through, and the
    /// ground-plane projection that picking and culling use. <see cref="Camera2D"/> and
    /// <see cref="OrbitCamera"/> are siblings; neither is a special case of the other.
    /// </summary>
    public abstract class CameraBase
    {
        public Rectangle Viewport { get; private set; }

        public abstract Matrix GetViewMatrix();

        public abstract Matrix GetProjectionMatrix();

        /// <summary>Where a ground point (z = 0) lands on screen.</summary>
        public abstract Vector2 GroundToScreen(Vector2 world);

        /// <summary>The point on the ground plane (z = 0) under a screen point. Culling and picking both go through this; for a 3D camera it is a ray-plane intersection, and points above the ground are not recoverable.</summary>
        public abstract Vector2 ScreenToGround(Vector2 screen);

        /// <summary>
        /// The ground under the viewport, in the local space of a world drawn through
        /// <paramref name="worldTransform"/>: each corner goes through <see cref="ScreenToGround"/>
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
                    corner is 1 or 2 ? Viewport.Right : Viewport.Left,
                    corner >= 2 ? Viewport.Bottom : Viewport.Top);
                var local = Vector2.Transform(ScreenToGround(screen), worldToLocal);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            var left = (int)System.MathF.Floor(min.X);
            var top = (int)System.MathF.Floor(min.Y);
            return new Rectangle(left, top, (int)System.MathF.Ceiling(max.X) - left, (int)System.MathF.Ceiling(max.Y) - top);
        }

        internal void Update(Rectangle viewportBounds)
        {
            var previous = Viewport;
            Viewport = viewportBounds;
            if (!previous.IsEmpty && viewportBounds.Size != previous.Size)
            {
                OnResized(previous.Size, viewportBounds.Size);
            }
        }

        protected virtual void OnResized(Point previous, Point current)
        {
        }
    }
}
