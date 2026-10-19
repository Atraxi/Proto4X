using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Input;
using MonoGameLibrary.UI;
using Proto4X.Factory.Simulation;
using System;
using System.Collections.Generic;

namespace Proto4X.Factory.Input
{
    /// <summary>
    /// Orbit controls, one remembered view per surface: arrows pan, the wheel zooms, Z/X turn,
    /// Page Up/Down tilt, F2 toggles perspective/orthographic, F3 snaps to true isometric.
    /// </summary>
    /// <remarks>
    /// The view is kept relative to the surface on show (target in grid-local tiles, yaw relative to
    /// the frame), so on a moving ship the camera rides along and the deck stays put on screen -
    /// the same contract as the 2D controller, in orbit terms.
    /// </remarks>
    public sealed class CameraController(OrbitCamera camera, IReadOnlyList<Surface> surfaces)
    {
        private const float PanScreensPerSecond = 0.9f;
        private const float TurnDegreesPerSecond = 90f;
        private const float TiltDegreesPerSecond = 50f;
        private const float ZoomStep = 1.12f;

        private readonly View?[] savedViews = new View?[surfaces.Count];
        private int shownSurface = -1;
        private View current;

        private struct View
        {
            public Vector2 LocalTarget;
            public float LocalYaw;
            public float Pitch;
            public float Distance;
            public bool Orthographic;
        }

        public void SetView(int surface, Vector2 localTile)
        {
            savedViews[surface] = new View { LocalTarget = localTile, LocalYaw = 0f, Pitch = 0.95f, Distance = 26f };
        }

        public void Update(int activeSurface, GameTime gameTime)
        {
            if (activeSurface != shownSurface)
            {
                if (shownSurface >= 0)
                {
                    savedViews[shownSurface] = current;
                }

                if (savedViews[activeSurface] is null)
                {
                    var grid = surfaces[activeSurface].Grid;
                    SetView(activeSurface, new Vector2(grid.Width / 2f, grid.Height / 2f));
                }

                current = savedViews[activeSurface]!.Value;
                shownSurface = activeSurface;
            }

            ApplyInput(gameTime);

            var frame = surfaces[activeSurface].Frame;
            camera.Target = new Vector3(frame.LocalToWorld(current.LocalTarget), 0f);
            camera.Yaw = frame.Rotation + current.LocalYaw;
            camera.Pitch = current.Pitch;
            camera.Distance = current.Distance;
            camera.Orthographic = current.Orthographic;
            current.Pitch = camera.Pitch;
            current.Distance = camera.Distance;
        }

        private void ApplyInput(GameTime gameTime)
        {
            var keyboard = InputManager.Instance.KeyboardInfo;
            var mouse = InputManager.Instance.MouseInfo;
            var seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Pan in screen terms: up moves the view towards the top of the screen, at a speed
            // proportional to how much ground is on screen.
            var pan = Vector2.Zero;
            if (keyboard.IsKeyDown(Keys.Up)) pan.Y -= 1;
            if (keyboard.IsKeyDown(Keys.Down)) pan.Y += 1;
            if (keyboard.IsKeyDown(Keys.Left)) pan.X -= 1;
            if (keyboard.IsKeyDown(Keys.Right)) pan.X += 1;
            if (pan != Vector2.Zero)
            {
                pan = Vector2.Normalize(pan) * PanScreensPerSecond * seconds * current.Distance;
                var forward = new Vector2(MathF.Sin(current.LocalYaw), -MathF.Cos(current.LocalYaw));
                var right = new Vector2(MathF.Cos(current.LocalYaw), MathF.Sin(current.LocalYaw));
                current.LocalTarget += right * pan.X - forward * pan.Y;
            }

            if (keyboard.IsKeyDown(Keys.Z)) current.LocalYaw += MathHelper.ToRadians(TurnDegreesPerSecond) * seconds;
            if (keyboard.IsKeyDown(Keys.X)) current.LocalYaw -= MathHelper.ToRadians(TurnDegreesPerSecond) * seconds;
            if (keyboard.IsKeyDown(Keys.PageUp)) current.Pitch += MathHelper.ToRadians(TiltDegreesPerSecond) * seconds;
            if (keyboard.IsKeyDown(Keys.PageDown)) current.Pitch -= MathHelper.ToRadians(TiltDegreesPerSecond) * seconds;

            if (keyboard.WasKeyJustPressed(Keys.F2))
            {
                current.Orthographic = !current.Orthographic;
            }

            if (keyboard.WasKeyJustPressed(Keys.F3))
            {
                current.Orthographic = true;
                current.LocalYaw = -MathF.PI / 4f;
                current.Pitch = MathF.Atan(1f / MathF.Sqrt(2f));
            }

            if (mouse.ScrollWheelDelta != 0)
            {
                current.Distance *= mouse.ScrollWheelDelta > 0 ? 1f / ZoomStep : ZoomStep;
            }
        }
    }
}
