using Microsoft.Xna.Framework;
using System;

namespace MonoGameLibrary.UI
{
    /// <summary>
    /// A 3D camera orbiting a point on the ground: yaw around it, pitch above it, at a distance.
    /// Perspective or orthographic. Picking and culling go through <see cref="ScreenToWorld"/>, which
    /// casts a ray from the screen point to the ground plane (z = 0).
    /// </summary>
    /// <remarks>
    /// <b>Handedness.</b> The ground uses screen-like axes, x east and y south, with z up - which is a
    /// left-handed frame, so a plain look-at would show east on the left. The view mirrors x after the
    /// look-at to put it back. Geometry is drawn without back-face culling, so the flipped winding
    /// costs nothing.
    /// <para>
    /// <b>Pitch is clamped</b> so the horizon never enters the view. That keeps every screen corner's
    /// ray on the ground, which is what lets culling stay a bounded box on the ground.
    /// </para>
    /// </remarks>
    public class OrbitCamera : Camera
    {
        public OrbitCamera(float minDistance = 4f, float maxDistance = 400f, float minPitch = 0.45f, float maxPitch = 1.45f)
            : base(minZoom: 1f, maxZoom: 1f)
        {
            MinDistance = minDistance;
            MaxDistance = maxDistance;
            MinPitch = minPitch;
            MaxPitch = maxPitch;
        }

        public float MinDistance { get; }
        public float MaxDistance { get; }
        public float MinPitch { get; }
        public float MaxPitch { get; }

        /// <summary>The ground point looked at.</summary>
        public Vector3 Target { get; set; }

        /// <summary>Radians. 0 looks north from the south; positive turns the camera clockwise around the target, seen from above.</summary>
        public float Yaw { get; set; }

        private float pitch = 0.95f;
        /// <summary>Radians above the ground, clamped.</summary>
        public float Pitch
        {
            get => pitch;
            set => pitch = MathHelper.Clamp(value, MinPitch, MaxPitch);
        }

        private float distance = 30f;
        public float Distance
        {
            get => distance;
            set => distance = MathHelper.Clamp(value, MinDistance, MaxDistance);
        }

        public float FieldOfView { get; set; } = MathHelper.ToRadians(40f);

        public bool Orthographic { get; set; }

        /// <summary>Where the eye is: <see cref="Distance"/> from the target, up <see cref="Pitch"/>, around by <see cref="Yaw"/>.</summary>
        public Vector3 Eye
        {
            get
            {
                var horizontal = MathF.Cos(Pitch);
                return Target + Distance * new Vector3(-MathF.Sin(Yaw) * horizontal, MathF.Cos(Yaw) * horizontal, MathF.Sin(Pitch));
            }
        }

        /// <summary>Ground direction the camera faces (unit, z = 0): screen-up as it lands on the ground.</summary>
        public Vector2 Forward => new(MathF.Sin(Yaw), -MathF.Cos(Yaw));

        public override Matrix GetViewMatrix()
        {
            // An orthographic eye sits far back along the same line, so nothing near the target is
            // behind the near plane; its apparent size comes from the projection, not the distance.
            var eye = Orthographic ? Target + (Eye - Target) * (OrthographicEyeDistance / Distance) : Eye;
            return Matrix.CreateLookAt(eye, Target, Vector3.UnitZ) * Matrix.CreateScale(-1f, 1f, 1f);
        }

        public override Matrix GetProjectionMatrix()
        {
            var aspect = Viewport.Height > 0 ? Viewport.Width / (float)Viewport.Height : 1f;
            if (Orthographic)
            {
                // Same apparent size at the target as the perspective view would have.
                var height = 2f * Distance * MathF.Tan(FieldOfView * 0.5f);
                return Matrix.CreateOrthographic(height * aspect, height, 1f, OrthographicEyeDistance * 2f);
            }

            return Matrix.CreatePerspectiveFieldOfView(FieldOfView, aspect, MathF.Max(0.1f, Distance * 0.05f), Distance * 12f + 100f);
        }

        private float OrthographicEyeDistance => MathF.Max(200f, Distance * 4f);

        public override Vector2 WorldToScreen(Vector2 world)
        {
            var clip = Vector4.Transform(new Vector4(world, 0f, 1f), GetViewMatrix() * GetProjectionMatrix());
            return new Vector2((clip.X / clip.W + 1f) * 0.5f * Viewport.Width, (1f - clip.Y / clip.W) * 0.5f * Viewport.Height);
        }

        /// <summary>The ground point (z = 0) under a screen point. A ray that misses the ground is clamped to the far plane's footprint.</summary>
        public override Vector2 ScreenToWorld(Vector2 screen)
        {
            var (origin, direction) = ScreenRay(screen);
            if (direction.Z > -1e-4f)
            {
                var flat = new Vector2(direction.X, direction.Y);
                flat = flat == Vector2.Zero ? Forward : Vector2.Normalize(flat);
                return new Vector2(Target.X, Target.Y) + flat * Distance * 10f;
            }

            var t = -origin.Z / direction.Z;
            var hit = origin + direction * t;
            return new Vector2(hit.X, hit.Y);
        }

        /// <summary>A world-space ray through a screen pixel, from the near plane.</summary>
        public (Vector3 Origin, Vector3 Direction) ScreenRay(Vector2 screen)
        {
            var inverse = Matrix.Invert(GetViewMatrix() * GetProjectionMatrix());
            var ndc = new Vector2(screen.X / Viewport.Width * 2f - 1f, 1f - screen.Y / Viewport.Height * 2f);
            var near = Vector4.Transform(new Vector4(ndc, 0f, 1f), inverse);
            var far = Vector4.Transform(new Vector4(ndc, 1f, 1f), inverse);
            var nearPoint = new Vector3(near.X, near.Y, near.Z) / near.W;
            var farPoint = new Vector3(far.X, far.Y, far.Z) / far.W;
            return (nearPoint, Vector3.Normalize(farPoint - nearPoint));
        }
    }
}
