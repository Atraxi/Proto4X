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

        public Vector2 WorldToScreen(Vector2 world)
        {
            return Vector2.Transform(world, GetCameraTransform());
        }

        public Vector2 ScreenToWorld(Vector2 screen)
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
