using Microsoft.Xna.Framework;

namespace MonoGameLibrary.UI
{
    /// <summary>World to screen: <see cref="WorldProjection"/> lays the world flat, then <see cref="ScreenTransform"/> pans, zooms and rotates it.</summary>
    public class Camera(float minZoom = 0.05f, float maxZoom = 20f)
    {
        public float MinZoom { get; } = minZoom;
        public float MaxZoom { get; } = maxZoom;

        /// <summary>How the world is laid flat: identity for top-down, or e.g. isometric. True perspective breaks picking and culling.</summary>
        public Matrix WorldProjection { get; set; } = Matrix.Identity;

        /// <summary>Pan, zoom and rotation in screen pixels from the top-left; identity shows world (0, 0) there at 1:1.</summary>
        public Matrix ScreenTransform { get; set; } = Matrix.Identity;

        /// <summary>Current scale of <see cref="ScreenTransform"/>.</summary>
        public float Zoom => new Vector2(ScreenTransform.M11, ScreenTransform.M12).Length();

        public Rectangle Viewport { get; private set; }

        public Matrix GetCameraTransform()
        {
            return WorldProjection * ScreenTransform;
        }

        /// <summary>View matrix for the geometry pass. A flat camera's view is its 2D transform: world to screen pixels.</summary>
        public virtual Matrix GetViewMatrix()
        {
            return GetCameraTransform();
        }

        /// <summary>
        /// Projection for the geometry pass: screen pixels to clip space, top-left origin. View-space
        /// z in [-1, 1] is depth, larger z nearer the viewer, so a flat game sorts by writing z.
        /// </summary>
        public virtual Matrix GetProjectionMatrix()
        {
            return Matrix.CreateOrthographicOffCenter(0, Viewport.Width, Viewport.Height, 0, -1, 1);
        }

        public virtual Vector2 WorldToScreen(Vector2 world)
        {
            return Vector2.Transform(world, GetCameraTransform());
        }

        /// <summary>The world point under a screen point, on the ground plane (z = 0). Culling and picking both go through this.</summary>
        public virtual Vector2 ScreenToWorld(Vector2 screen)
        {
            return Vector2.Transform(screen, Matrix.Invert(GetCameraTransform()));
        }

        /// <summary>
        /// The ground under the viewport, in the local space of a world drawn through
        /// <paramref name="worldTransform"/>: each corner goes through <see cref="ScreenToWorld"/>
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
                var local = Vector2.Transform(ScreenToWorld(screen), worldToLocal);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            var left = (int)System.MathF.Floor(min.X);
            var top = (int)System.MathF.Floor(min.Y);
            return new Rectangle(left, top, (int)System.MathF.Ceiling(max.X) - left, (int)System.MathF.Ceiling(max.Y) - top);
        }

        /// <summary>Moves the camera by <paramref name="screenDelta"/> pixels, the world slides the other way.</summary>
        public void Pan(Vector2 screenDelta)
        {
            ScreenTransform *= Matrix.CreateTranslation(-screenDelta.X, -screenDelta.Y, 0);
        }

        /// <summary>Zooms by <paramref name="factor"/>, keeping <paramref name="screenPoint"/> fixed. Clamped to the zoom limits.</summary>
        public void ZoomAt(Vector2 screenPoint, float factor)
        {
            var current = Zoom;
            var step = MathHelper.Clamp(current * factor, MinZoom, MaxZoom) / current;
            ScreenTransform *= Matrix.CreateTranslation(-screenPoint.X, -screenPoint.Y, 0) *
                Matrix.CreateScale(step, step, 1) *
                Matrix.CreateTranslation(screenPoint.X, screenPoint.Y, 0);
        }

        /// <summary>Turns the camera by <paramref name="radians"/> about <paramref name="screenPoint"/>, the world turns the other way.</summary>
        public void RotateAround(Vector2 screenPoint, float radians)
        {
            ScreenTransform *= Matrix.CreateTranslation(-screenPoint.X, -screenPoint.Y, 0) *
                Matrix.CreateRotationZ(-radians) *
                Matrix.CreateTranslation(screenPoint.X, screenPoint.Y, 0);
        }

        internal void Update(Rectangle viewportBounds)
        {
            // On resize, shift by half the size change so the centre stays put rather than the top-left.
            if (!Viewport.IsEmpty && viewportBounds.Size != Viewport.Size)
            {
                ScreenTransform *= Matrix.CreateTranslation((viewportBounds.Width - Viewport.Width) / 2f, (viewportBounds.Height - Viewport.Height) / 2f, 0);
            }

            Viewport = viewportBounds;
        }
    }
}
