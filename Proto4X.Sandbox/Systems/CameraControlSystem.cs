using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Input;
using MonoGameLibrary.Systems.Infrastructure;
using MonoGameLibrary.UI;
using System;
using System.Collections.Generic;

namespace Proto4X.Sandbox.Systems
{
    /// <summary>
    /// Arrow keys pan, the wheel zooms, Z/X turn, Page Up/Down tilt. The engine camera has no
    /// input bindings, so they live here.
    /// </summary>
    /// <remarks>
    /// Panning is in screen terms: Up always moves the view towards the top of the screen, at a
    /// speed proportional to the distance, so it feels the same at every zoom.
    /// </remarks>
    public class CameraControlSystem(OrbitCamera camera) : ISystem, IUpdateSystem
    {
        private const float PanScreensPerSecond = 0.9f;
        private const float TurnDegreesPerSecond = 90f;
        private const float TiltDegreesPerSecond = 50f;
        private const float ZoomStep = 1.12f;

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
                pan = Vector2.Normalize(pan) * PanScreensPerSecond * seconds * camera.Distance;
                var right = new Vector2(MathF.Cos(camera.Yaw), MathF.Sin(camera.Yaw));
                var ground = right * pan.X - camera.Forward * pan.Y;
                camera.Target += new Vector3(ground, 0f);
            }

            if (keyboard.IsKeyDown(Keys.Z)) camera.Yaw += MathHelper.ToRadians(TurnDegreesPerSecond) * seconds;
            if (keyboard.IsKeyDown(Keys.X)) camera.Yaw -= MathHelper.ToRadians(TurnDegreesPerSecond) * seconds;
            if (keyboard.IsKeyDown(Keys.PageUp)) camera.Pitch += MathHelper.ToRadians(TiltDegreesPerSecond) * seconds;
            if (keyboard.IsKeyDown(Keys.PageDown)) camera.Pitch -= MathHelper.ToRadians(TiltDegreesPerSecond) * seconds;

            if (mouse.ScrollWheelDelta != 0)
            {
                camera.Distance *= mouse.ScrollWheelDelta > 0 ? 1f / ZoomStep : ZoomStep;
            }
        }
    }
}
