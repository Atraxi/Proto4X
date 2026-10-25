using Microsoft.Xna.Framework;

namespace MonoGameLibrary.UI
{
    /// <summary>World to screen: <see cref="WorldProjection"/> lays the world flat, then <see cref="ScreenTransform"/> pans, zooms and rotates it.</summary>
    public class Camera2D(float minZoom = 0.05f, float maxZoom = 20f) : CameraBase
    {
        public float MinZoom { get; } = minZoom;
        public float MaxZoom { get; } = maxZoom;

        /// <summary>How the world is laid flat: identity for top-down, or e.g. isometric. True perspective breaks picking and culling.</summary>
        public Matrix WorldProjection { get; set; } = Matrix.Identity;

        /// <summary>Pan, zoom and rotation in screen pixels from the top-left; identity shows world (0, 0) there at 1:1.</summary>
        public Matrix ScreenTransform { get; set; } = Matrix.Identity;

        /// <summary>Current scale of <see cref="ScreenTransform"/>.</summary>
        public float Zoom => new Vector2(ScreenTransform.M11, ScreenTransform.M12).Length();

        public Matrix GetCameraTransform()
        {
            return WorldProjection * ScreenTransform;
        }

        /// <summary>View matrix for the geometry pass. A flat camera's view is its 2D transform: world to screen pixels.</summary>
        public override Matrix GetViewMatrix()
        {
            return GetCameraTransform();
        }

        /// <summary>
        /// Projection for the geometry pass: screen pixels to clip space, top-left origin. View-space
        /// z in [-1, 1] is depth, larger z nearer the viewer, so a flat game sorts by writing z.
        /// </summary>
        public override Matrix GetProjectionMatrix()
        {
            return Matrix.CreateOrthographicOffCenter(0, Viewport.Width, Viewport.Height, 0, -1, 1);
        }

        public override Vector2 GroundToScreen(Vector2 world)
        {
            return Vector2.Transform(world, GetCameraTransform());
        }

        public override Vector2 ScreenToGround(Vector2 screen)
        {
            return Vector2.Transform(screen, Matrix.Invert(GetCameraTransform()));
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

        protected override void OnResized(Point previous, Point current)
        {
            // Shift by half the size change so the centre stays put rather than the top-left.
            ScreenTransform *= Matrix.CreateTranslation((current.X - previous.X) / 2f, (current.Y - previous.Y) / 2f, 0);
        }
    }
}
