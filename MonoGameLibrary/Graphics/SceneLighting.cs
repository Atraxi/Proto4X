using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace MonoGameLibrary.Graphics
{
    /// <summary>
    /// One sun and a flat ambient term, in camera world space. Lit layers get
    /// <c>colour × (Ambient + Diffuse × max(0, N·Sun))</c>, evaluated on the GPU per vertex.
    /// </summary>
    /// <remarks>
    /// Lighting happens on the GPU, not baked into vertex colours, because normals are only known in
    /// a world's <em>local</em> space when geometry is built. A turning ship's geometry never
    /// changes; its world matrix does, and the GPU turns its normals with it, so the sun stays put
    /// in the sky while the ship turns under it - with no rebuild.
    /// <para>
    /// <see cref="Configure"/> pins <see cref="BasicEffect"/> down to exactly that formula (no
    /// specular, one light, no fog).
    /// </para>
    /// </remarks>
    public sealed class SceneLighting
    {
        private Vector3 sun = Vector3.Normalize(new Vector3(-0.45f, -0.6f, 1f));

        /// <summary>Unit vector towards the sun, in camera world space (x east, y south, z up).</summary>
        public Vector3 Sun
        {
            get => sun;
            set => sun = Vector3.Normalize(value);
        }

        public float Ambient { get; set; } = 0.52f;
        public float Diffuse { get; set; } = 0.48f;

        /// <summary>
        /// The sun's direction in the local space of a world drawn through
        /// <paramref name="worldTransform"/> (a rotation and a translation). For anything that
        /// has to aim at the sun from local space, such as a planar shadow.
        /// </summary>
        public Vector3 SunInLocal(in Matrix worldTransform) =>
            Vector3.Normalize(Vector3.TransformNormal(sun, Matrix.Invert(worldTransform)));

        /// <summary>
        /// Flattens local-space geometry onto the ground plane (z = 0) along <paramref name="localSun"/>,
        /// then lifts it by <paramref name="lift"/> so it doesn't fight the ground for depth.
        /// </summary>
        /// <remarks>
        /// A true projection would squash z to exactly zero, but that matrix has no inverse, and the
        /// GPU's lighting needs the inverse of the world matrix to transform normals. Keeping a
        /// sliver of z (×0.0001, under a hundredth of a tile for anything we build) makes it
        /// invertible, and the visible difference is nil. Draw shadow geometry with every normal
        /// straight up: a horizontal plane stays horizontal under this matrix, so it lights exactly
        /// like the ground it lies on.
        /// </remarks>
        public static Matrix PlanarShadow(Vector3 localSun, float lift)
        {
            const float sliver = 1e-4f;
            return new Matrix(
                1, 0, 0, 0,
                0, 1, 0, 0,
                -localSun.X / localSun.Z, -localSun.Y / localSun.Z, sliver, 0,
                0, 0, lift, 1);
        }

        /// <summary>Sets a lit <see cref="BasicEffect"/> to evaluate exactly the formula above.</summary>
        internal void Configure(BasicEffect effect)
        {
            effect.LightingEnabled = true;
            effect.PreferPerPixelLighting = false;
            effect.DiffuseColor = Vector3.One;
            effect.SpecularColor = Vector3.Zero;
            effect.EmissiveColor = Vector3.Zero;
            effect.AmbientLightColor = new Vector3(Ambient);
            effect.FogEnabled = false;

            // BasicEffect's lights shine *along* Direction, the opposite of our towards-the-sun vector.
            effect.DirectionalLight0.Enabled = true;
            effect.DirectionalLight0.Direction = -sun;
            effect.DirectionalLight0.DiffuseColor = new Vector3(Diffuse);
            effect.DirectionalLight0.SpecularColor = Vector3.Zero;
            effect.DirectionalLight1.Enabled = false;
            effect.DirectionalLight2.Enabled = false;
        }
    }
}
