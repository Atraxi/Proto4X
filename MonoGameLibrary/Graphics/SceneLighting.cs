using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace MonoGameLibrary.Graphics
{
    /// <summary>
    /// One sun plus flat ambient, in camera world space. Lit layers get
    /// <c>colour × (Ambient + Diffuse × max(0, N·Sun))</c>, evaluated on the GPU per vertex.
    /// </summary>
    /// <remarks>
    /// Lighting is on the GPU, not baked, because a world's geometry is built in its <em>local</em>
    /// space. A turning ship's vertices never change; its world matrix does, and the GPU turns the
    /// normals with it, so the sun stays fixed in the sky with no rebuild.
    /// </remarks>
    public sealed class SceneLighting
    {
        // Default: high, from the north-west.
        private Vector3 sun = Vector3.Normalize(new Vector3(-0.45f, -0.6f, 1f));

        /// <summary>Unit vector towards the sun, in camera world space (x east, y south, z up).</summary>
        public Vector3 Sun
        {
            get => sun;
            set => sun = Vector3.Normalize(value);
        }

        // Ambient + Diffuse = 1, so a face square-on to the sun is exactly its vertex colour.
        public float Ambient { get; set; } = 0.52f;
        public float Diffuse { get; set; } = 0.48f;

        /// <summary>
        /// The sun's direction in the local space of a world drawn through
        /// <paramref name="worldTransform"/> (rotation plus translation).
        /// </summary>
        public Vector3 SunInLocal(in Matrix worldTransform) =>
            // TransformNormal ignores translation, which is right for a direction.
            Vector3.Normalize(Vector3.TransformNormal(sun, Matrix.Invert(worldTransform)));

        /// <summary>
        /// A matrix that flattens local-space geometry onto the ground (z = 0) along
        /// <paramref name="localSun"/>, then lifts it by <paramref name="lift"/> to avoid depth
        /// fighting with the ground.
        /// </summary>
        /// <remarks>
        /// A point slides along the light ray until z = 0: <c>x' = x − z·sx/sz</c>, same for y.
        /// z is scaled by a tiny sliver, not 0, because a zero scale has no inverse and the GPU needs
        /// the world matrix's inverse to transform normals. Give shadow vertices normals pointing
        /// straight up: a horizontal plane stays horizontal under this skew.
        /// </remarks>
        public static Matrix PlanarShadow(Vector3 localSun, float lift)
        {
            const float sliver = 1e-4f;
            // Row-vector convention (v × M): each row says where that input axis ends up.
            return new Matrix(
                1, 0, 0, 0,                                              // x stays x
                0, 1, 0, 0,                                              // y stays y
                -localSun.X / localSun.Z, -localSun.Y / localSun.Z, sliver, 0, // height shifts x and y along the ray
                0, 0, lift, 1);                                          // translate up by lift
        }

        /// <summary>Sets a lit <see cref="BasicEffect"/> to evaluate exactly the formula above and nothing else.</summary>
        internal void Configure(BasicEffect effect)
        {
            effect.LightingEnabled = true;
            effect.PreferPerPixelLighting = false;          // per vertex: identical on flat faces, cheaper
            effect.DiffuseColor = Vector3.One;              // material is just the vertex colour
            effect.SpecularColor = Vector3.Zero;            // no highlights
            effect.EmissiveColor = Vector3.Zero;
            effect.AmbientLightColor = new Vector3(Ambient);
            effect.FogEnabled = false;

            effect.DirectionalLight0.Enabled = true;
            // BasicEffect's Direction is the way light travels, the opposite of our towards-the-sun vector.
            effect.DirectionalLight0.Direction = -sun;
            effect.DirectionalLight0.DiffuseColor = new Vector3(Diffuse);
            effect.DirectionalLight0.SpecularColor = Vector3.Zero;
            // BasicEffect has three light slots; only the first is used.
            effect.DirectionalLight1.Enabled = false;
            effect.DirectionalLight2.Enabled = false;
        }
    }
}
