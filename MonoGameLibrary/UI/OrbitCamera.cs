using Microsoft.Xna.Framework;
using System;

namespace MonoGameLibrary.UI
{
    /// <summary>
    /// A 3D camera orbiting a point on the ground: yaw around it, pitch above it, at a distance.
    /// Perspective or orthographic. Picking and culling use <see cref="ScreenToGround"/>, a ray cast
    /// from a screen point to the ground plane (z = 0).
    /// </summary>
    /// <remarks>
    /// <b>Handedness.</b> Ground axes are x east, y south, z up, which is left-handed, so a plain
    /// look-at would show east on the left. The view mirrors x afterwards to fix that. The flipped
    /// winding is harmless because geometry is drawn without back-face culling.
    /// <para>
    /// <b>Pitch is clamped</b> so the horizon never enters the view. Every screen corner's ray then
    /// hits the ground, which keeps culling a bounded box on the ground.
    /// </para>
    /// </remarks>
    public class OrbitCamera : CameraBase
    {
        public OrbitCamera(float minDistance = 4f, float maxDistance = 400f, float minPitch = 0.45f, float maxPitch = 1.45f)
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
                // Spherical to Cartesian. Cos(Pitch) is the share of Distance lying along the ground;
                // Sin(Pitch) is the height. Yaw 0 puts the eye south of the target (+y), looking north.
                var horizontal = MathF.Cos(Pitch);
                return Target + Distance * new Vector3(-MathF.Sin(Yaw) * horizontal, MathF.Cos(Yaw) * horizontal, MathF.Sin(Pitch));
            }
        }

        /// <summary>Ground direction the camera faces (unit, z = 0): screen-up as it lands on the ground.</summary>
        public Vector2 Forward => new(MathF.Sin(Yaw), -MathF.Cos(Yaw));

        public override Matrix GetViewMatrix()
        {
            // Orthographic has no perspective, so the eye can sit far back along the same line,
            // keeping everything near the target in front of the near plane. Apparent size comes from
            // the projection, not the distance.
            var eye = Orthographic ? Target + (Eye - Target) * (OrthographicEyeDistance / Distance) : Eye;

            // Look-at moves the world so the eye is at the origin looking down -z. The x mirror
            // (scale -1) then puts east on the right again (see the class remarks).
            return Matrix.CreateLookAt(eye, Target, Vector3.UnitZ) * Matrix.CreateScale(-1f, 1f, 1f);
        }

        public override Matrix GetProjectionMatrix()
        {
            var aspect = Viewport.Height > 0 ? Viewport.Width / (float)Viewport.Height : 1f;
            if (Orthographic)
            {
                // The view volume's height is what the perspective frustum would span at the target's
                // distance, so switching modes keeps the target the same size on screen.
                var height = 2f * Distance * MathF.Tan(FieldOfView * 0.5f);
                return Matrix.CreateOrthographic(height * aspect, height, 1f, OrthographicEyeDistance * 2f);
            }

            // Near and far scale with distance so depth precision stays proportionate across the zoom
            // range (depth precision is worst when near is tiny compared with far).
            return Matrix.CreatePerspectiveFieldOfView(FieldOfView, aspect, MathF.Max(0.1f, Distance * 0.05f), Distance * 12f + 100f);
        }

        private float OrthographicEyeDistance => MathF.Max(200f, Distance * 4f);

        public override Vector2 GroundToScreen(Vector2 world)
        {
            // Ground point (z = 0) to clip space, then divide by w (perspective) and map
            // [-1, 1] to pixels. Screen y grows downward, so y is flipped.
            var clip = Vector4.Transform(new Vector4(world, 0f, 1f), GetViewMatrix() * GetProjectionMatrix());
            return new Vector2((clip.X / clip.W + 1f) * 0.5f * Viewport.Width, (1f - clip.Y / clip.W) * 0.5f * Viewport.Height);
        }

        /// <summary>The ground point (z = 0) under a screen point. A ray that misses the ground is clamped to the far plane's footprint.</summary>
        public override Vector2 ScreenToGround(Vector2 screen)
        {
            var (origin, direction) = ScreenRay(screen);

            // A ray that points level or upward never reaches the ground; use a far point instead.
            if (direction.Z > -1e-4f)
            {
                var flat = new Vector2(direction.X, direction.Y);
                flat = flat == Vector2.Zero ? Forward : Vector2.Normalize(flat);
                return new Vector2(Target.X, Target.Y) + flat * Distance * 10f;
            }

            // Ray-plane intersection with z = 0: origin.z + t·direction.z = 0.
            var t = -origin.Z / direction.Z;
            var hit = origin + direction * t;
            return new Vector2(hit.X, hit.Y);
        }

        /// <summary>A world-space ray through a screen pixel, from the near plane.</summary>
        public (Vector3 Origin, Vector3 Direction) ScreenRay(Vector2 screen)
        {
            // Run the projection chain backwards: unproject the pixel at depth 0 (near plane) and
            // depth 1 (far plane); the line through those two world points is the ray.
            var inverse = Matrix.Invert(GetViewMatrix() * GetProjectionMatrix());
            // Pixels to normalised device coordinates, [-1, 1] on both axes with y up.
            var ndc = new Vector2(screen.X / Viewport.Width * 2f - 1f, 1f - screen.Y / Viewport.Height * 2f);
            var near = Vector4.Transform(new Vector4(ndc, 0f, 1f), inverse);
            var far = Vector4.Transform(new Vector4(ndc, 1f, 1f), inverse);
            // Homogeneous coordinates: divide by w to get the actual 3D point.
            var nearPoint = new Vector3(near.X, near.Y, near.Z) / near.W;
            var farPoint = new Vector3(far.X, far.Y, far.Z) / far.W;
            return (nearPoint, Vector3.Normalize(farPoint - nearPoint));
        }
    }
}
