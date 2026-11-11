using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameLibrary.Graphics
{
    /// <summary>
    /// A custom <see cref="Effect"/> plus the device state it draws with, for the geometry pass.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Standard parameters are bound by name</b> (after Unity's built-in shader variables): if
    /// the effect declares any of <c>World</c>, <c>View</c>, <c>Projection</c>,
    /// <c>WorldViewProjection</c>, <c>Time</c>, <c>SunDirection</c> or <c>CameraPosition</c>, the
    /// batch sets them for every draw. A shader that doesn't declare one simply doesn't get it. The
    /// lookups happen once, here; a draw costs the matrix sets plus one <c>Apply</c>, like
    /// <see cref="BasicEffect"/>.
    /// </para>
    /// <para>
    /// <b>Vertex input</b> must be <see cref="MeshVertex"/>: <c>POSITION0</c> (float3),
    /// <c>NORMAL0</c> (float3), <c>COLOR0</c> (float4), <c>TEXCOORD0</c> (float2). A mismatch fails
    /// only when drawn, never at build.
    /// </para>
    /// Game-specific parameters go through <see cref="Effect"/>'s own <c>Parameters</c>.
    /// </remarks>
    public sealed class Material
    {
        private readonly EffectParameter? world;
        private readonly EffectParameter? view;
        private readonly EffectParameter? projection;
        private readonly EffectParameter? worldViewProjection;
        private readonly EffectParameter? time;
        private readonly EffectParameter? sunDirection;
        private readonly EffectParameter? cameraPosition;

        public Material(Effect effect, BlendState? blend = null, DepthStencilState? depth = null, RasterizerState? rasterizer = null)
        {
            Effect = effect;
            Blend = blend ?? BlendState.Opaque;
            // A blended material reads depth but doesn't write it, like the Translucent layer.
            Depth = depth ?? (Blend == BlendState.Opaque ? DepthStencilState.Default : DepthStencilState.DepthRead);
            Rasterizer = rasterizer ?? RasterizerState.CullNone;

            var parameters = effect.Parameters;
            world = parameters["World"];
            view = parameters["View"];
            projection = parameters["Projection"];
            worldViewProjection = parameters["WorldViewProjection"];
            time = parameters["Time"];
            sunDirection = parameters["SunDirection"];
            cameraPosition = parameters["CameraPosition"];
        }

        public Effect Effect { get; }
        public BlendState Blend { get; }
        public DepthStencilState Depth { get; }
        public RasterizerState Rasterizer { get; }

        /// <summary>Drawn after the translucent layer (blended) or after the emissive one (opaque).</summary>
        public bool IsBlended => Blend != BlendState.Opaque;

        /// <summary>Sets whichever standard parameters the effect declares. The caller applies the passes.</summary>
        internal void Bind(in Matrix worldMatrix, in Matrix viewMatrix, in Matrix projectionMatrix, float seconds, Vector3 sun, Vector3 camera)
        {
            world?.SetValue(worldMatrix);
            view?.SetValue(viewMatrix);
            projection?.SetValue(projectionMatrix);
            worldViewProjection?.SetValue(worldMatrix * viewMatrix * projectionMatrix);
            time?.SetValue(seconds);
            sunDirection?.SetValue(sun);
            cameraPosition?.SetValue(camera);
        }
    }
}
