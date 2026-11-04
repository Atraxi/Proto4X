using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Input;
using MonoGameLibrary.Systems.Infrastructure;
using MonoGameLibrary.UI;
using System.Collections.Generic;

namespace Proto4X.Sandbox.Systems
{
    /// <summary>
    /// Arrow keys pan, the wheel zooms about the cursor, Z/X rotate. The engine camera has no
    /// input bindings, so they live here.
    /// </summary>
    /// <remarks>
    /// Everything is in screen terms: Up always moves the view towards the top of the screen, at the
    /// same on-screen speed whatever the rotation and zoom.
    /// </remarks>
    public class CameraControlSystem(Camera2D camera) : ISystem, IUpdateSystem
    {
        private const float PanPixelsPerSecond = 600f;
        private const float RotationDegreesPerSecond = 100f;
        private const float ZoomStep = 1.1f;

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => [];

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            var keyboard = InputManager.Instance.KeyboardInfo;
            var mouse = InputManager.Instance.MouseInfo;
            var seconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

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
