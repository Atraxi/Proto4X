using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Input;
using MonoGameLibrary.UI;
using Proto4X.Factory.Rendering;
using Proto4X.Factory.Simulation;
using System.Collections.Generic;

namespace Proto4X.Factory.Input
{
    /// <summary>
    /// Arrow keys pan, the wheel zooms about the cursor, Z/X rotate - one remembered view per surface.
    /// </summary>
    /// <remarks>
    /// The view is kept as "grid-local pixels to screen" for the surface on show, and the camera's
    /// world view is rebuilt from it against the surface's frame every frame. Two things fall out of
    /// that: the view rides along with a moving ship, so its deck stays put and upright on screen
    /// while it drifts and turns, and switching surfaces returns to where you left each one.
    /// <para>
    /// Run after <see cref="BuildController.Update"/> and the sim step: picking then resolves against
    /// the frame that was on screen, and drawing follows where the ship has moved to this tick.
    /// </para>
    /// </remarks>
    public sealed class CameraController(Camera camera, IReadOnlyList<Surface> surfaces)
    {
        private const float PanPixelsPerSecond = 800f;
        private const float RotationDegreesPerSecond = 100f;
        private const float ZoomStep = 1.1f;

        // Grid-local pixels to screen, relative to the screen centre so a resize while a surface is
        // hidden doesn't shift it.
        private readonly Matrix?[] savedViews = new Matrix?[surfaces.Count];
        private int shownSurface = -1;
        private Matrix shownFrame;

        /// <summary>Where a surface's view starts: this tile centred, 1:1, upright. Otherwise its grid centre.</summary>
        public void SetView(int surface, Vector2 localTile)
        {
            savedViews[surface] = Matrix.CreateTranslation(-localTile.X * SurfaceView.TileSize, -localTile.Y * SurfaceView.TileSize, 0);
        }

        public void Update(int activeSurface, GameTime gameTime)
        {
            var centre = Matrix.CreateTranslation(camera.Viewport.Width / 2f, camera.Viewport.Height / 2f, 0);

            Matrix localToScreen;
            if (activeSurface == shownSurface)
            {
                // Last frame's mapping, including this frame's resize adjustment from the engine.
                localToScreen = shownFrame * camera.ScreenTransform;
            }
            else
            {
                if (shownSurface >= 0)
                {
                    savedViews[shownSurface] = shownFrame * camera.ScreenTransform * Matrix.Invert(centre);
                }

                if (savedViews[activeSurface] is null)
                {
                    var grid = surfaces[activeSurface].Grid;
                    SetView(activeSurface, new Vector2(grid.Width / 2f, grid.Height / 2f));
                }

                localToScreen = savedViews[activeSurface]!.Value * centre;
                shownSurface = activeSurface;
            }

            var frame = FrameMatrix(surfaces[activeSurface].Frame);
            camera.ScreenTransform = Matrix.Invert(frame) * localToScreen;
            shownFrame = frame;

            ApplyInput(gameTime);
        }

        /// <summary>Grid-local pixels to world pixels; the matrix form of <see cref="SurfaceView.TileToWorld"/>.</summary>
        private static Matrix FrameMatrix(in SurfaceFrame frame)
        {
            return Matrix.CreateRotationZ(frame.Rotation) *
                Matrix.CreateTranslation(frame.Position.X * SurfaceView.TileSize, frame.Position.Y * SurfaceView.TileSize, 0);
        }

        private void ApplyInput(GameTime gameTime)
        {
            var keyboard = InputManager.Instance.KeyboardInfo;
            var mouse = InputManager.Instance.MouseInfo;
            var seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Screen terms: Up always moves the view towards the top of the screen, at the same
            // on-screen speed whatever the rotation and zoom.
            var pan = Vector2.Zero;
            if (keyboard.IsKeyDown(Keys.Up)) pan.Y -= 1;
            if (keyboard.IsKeyDown(Keys.Down)) pan.Y += 1;
            if (keyboard.IsKeyDown(Keys.Left)) pan.X -= 1;
            if (keyboard.IsKeyDown(Keys.Right)) pan.X += 1;
            if (pan != Vector2.Zero)
            {
                camera.Pan(Vector2.Normalize(pan) * PanPixelsPerSecond * seconds);
            }

            var screenCentre = camera.Viewport.Center.ToVector2();
            if (keyboard.IsKeyDown(Keys.Z)) camera.RotateAround(screenCentre, MathHelper.ToRadians(RotationDegreesPerSecond) * seconds);
            if (keyboard.IsKeyDown(Keys.X)) camera.RotateAround(screenCentre, -MathHelper.ToRadians(RotationDegreesPerSecond) * seconds);

            if (mouse.ScrollWheelDelta != 0)
            {
                camera.ZoomAt(mouse.Position.ToVector2(), mouse.ScrollWheelDelta > 0 ? ZoomStep : 1f / ZoomStep);
            }
        }
    }
}
