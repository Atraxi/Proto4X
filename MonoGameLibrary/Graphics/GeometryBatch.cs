using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Graphics
{
    /// <summary>A retained mesh submitted for this frame, with its transform into the world's local space.</summary>
    internal readonly record struct MeshDraw(StaticMesh Mesh, Matrix Transform);

    /// <summary>
    /// One frame's 3D drawing for one world: retained <see cref="StaticMesh"/>es submitted by
    /// reference, plus <see cref="Immediate"/> triangles rebuilt every frame, drawn through
    /// world/view/projection with depth and <see cref="Lighting"/>.
    /// </summary>
    /// <remarks>
    /// A draw call needs vertices, an <see cref="Effect"/> (shaders plus parameters), and device state:
    /// <see cref="BlendState"/> (how a pixel mixes with what's there), <see cref="DepthStencilState"/>
    /// (depth test and write), <see cref="RasterizerState"/> (culling), <see cref="SamplerState"/>
    /// (texture filtering). Two stock <see cref="BasicEffect"/>s cover every layer, so there is no
    /// content pipeline step.
    /// </remarks>
    public sealed class GeometryBatch
    {
        // Additive glow for premultiplied colour: result = src + dst. (AlphaBlend and Opaque are built in.)
        private static readonly BlendState premultipliedAdditive = new()
        {
            Name = "GeometryBatch.PremultipliedAdditive",
            ColorSourceBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
        };

        private readonly List<MeshDraw> meshDraws = [];

        // Created on first Flush, when a device is available.
        private BasicEffect? litEffect;
        private BasicEffect? unlitEffect;
        private Texture2D? fallbackTexture;

        /// <summary>Geometry rebuilt every frame: anything that moves or changes too often to retain.</summary>
        public GeometryBuffer Immediate { get; } = new();

        public SceneLighting Lighting { get; } = new();

        /// <summary>One texture (an atlas) for the whole batch. Null draws everything untextured white.</summary>
        public Texture2D? Texture { get; set; }

        // Point sampling keeps the white texel exact.
        public SamplerState Sampler { get; set; } = SamplerState.PointClamp;

        public void Clear()
        {
            Immediate.Clear();
            meshDraws.Clear();
        }

        /// <summary>Queues <paramref name="mesh"/> for this frame, through <paramref name="transform"/> into the world's local space.</summary>
        public void Draw(StaticMesh mesh, in Matrix transform)
        {
            if (!mesh.IsEmpty)
            {
                meshDraws.Add(new MeshDraw(mesh, transform));
            }
        }

        private static bool IsLit(GeometryLayer layer) => layer is GeometryLayer.Opaque or GeometryLayer.Translucent;

        /// <summary>
        /// Draws every layer through the given matrices. Clears depth first, so each world
        /// composites on its own, over whatever was drawn before.
        /// </summary>
        /// <remarks>
        /// Sets every device state it depends on: SpriteBatch leaves its own behind (no depth,
        /// back-face culling). Culling stays off so winding can never make geometry vanish.
        /// </remarks>
        public void Flush(GraphicsDevice device, Matrix world, Matrix view, Matrix projection)
        {
            if (Immediate.IsEmpty && meshDraws.Count == 0)
            {
                return;
            }

            litEffect ??= new BasicEffect(device) { TextureEnabled = true, VertexColorEnabled = true };
            unlitEffect ??= new BasicEffect(device) { TextureEnabled = true, VertexColorEnabled = true, LightingEnabled = false };
            // 1x1 white, so "texture × vertex colour" is just the vertex colour when no atlas is set.
            if (fallbackTexture is null)
            {
                fallbackTexture = new Texture2D(device, 1, 1);
                fallbackTexture.SetData([Color.White]);
            }

            Lighting.Configure(litEffect);
            var texture = Texture ?? fallbackTexture;
            // View, projection and texture are the same for every draw this frame; World varies per mesh.
            foreach (var effect in (ReadOnlySpan<BasicEffect>)[litEffect, unlitEffect])
            {
                effect.View = view;
                effect.Projection = projection;
                effect.Texture = texture;
            }

            device.Clear(ClearOptions.DepthBuffer, Color.Transparent, 1f, 0);
            device.RasterizerState = RasterizerState.CullNone;

            // Opaque before translucent: blending needs everything behind it already drawn, and the
            // depth test needs the solids' depth. Translucent and additive read depth but don't write it.
            DrawLayer(device, GeometryLayer.Opaque, world, BlendState.Opaque, DepthStencilState.Default);
            DrawLayer(device, GeometryLayer.Emissive, world, BlendState.Opaque, DepthStencilState.Default);
            DrawLayer(device, GeometryLayer.Translucent, world, BlendState.AlphaBlend, DepthStencilState.DepthRead);
            DrawLayer(device, GeometryLayer.Additive, world, premultipliedAdditive, DepthStencilState.DepthRead);
        }

        private void DrawLayer(GraphicsDevice device, GeometryLayer layer, in Matrix world, BlendState blend, DepthStencilState depth)
        {
            var effect = IsLit(layer) ? litEffect! : unlitEffect!;
            device.BlendState = blend;
            device.DepthStencilState = depth;

            // Retained meshes first. Each has its own transform, so its own World and its own Apply:
            // setting World only changes a C# field, and Apply is what pushes it (and, when lit, its
            // inverse-transpose for normals) to the GPU.
            foreach (var draw in meshDraws)
            {
                if (!draw.Mesh.HasLayer(layer))
                {
                    continue;
                }

                // Row-vector order: mesh space → world-local space → camera world.
                effect.World = draw.Transform * world;
                effect.CurrentTechnique.Passes[0].Apply();
                device.SamplerStates[0] = Sampler;
                draw.Mesh.DrawLayer(device, layer);
            }

            // Then this frame's immediate geometry, streamed up from the CPU arrays.
            var pages = Immediate.PageCount(layer);
            if (pages == 0)
            {
                return;
            }

            effect.World = world;
            effect.CurrentTechnique.Passes[0].Apply();
            device.SamplerStates[0] = Sampler;
            for (var i = 0; i < pages; i++)
            {
                var (vertices, vertexCount, indices, indexCount) = Immediate.RawPage(layer, i);
                if (indexCount > 0)
                {
                    // Copies the arrays into a hidden dynamic buffer on every call.
                    device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertexCount, indices, 0, indexCount / 3);
                }
            }
        }
    }
}
