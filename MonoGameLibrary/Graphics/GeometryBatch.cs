using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Graphics
{
    /// <summary>A retained mesh submitted for this frame, with the transform from its own space into the world's local space.</summary>
    public readonly record struct MeshDraw(StaticMesh Mesh, Matrix Transform);

    /// <summary>
    /// One frame's 3D drawing for one world: retained <see cref="StaticMesh"/>es submitted by
    /// reference, plus <see cref="Immediate"/> triangles rebuilt every frame, all drawn through a
    /// world/view/projection with depth and <see cref="Lighting"/>.
    /// </summary>
    /// <remarks>
    /// <b>The GPU side, briefly.</b> A draw call needs: vertices (a buffer, or an array streamed up),
    /// an <see cref="Effect"/> (the shader pair plus its parameters: matrices, texture, lights), and
    /// device state - <see cref="BlendState"/> (how a pixel combines with what's there),
    /// <see cref="DepthStencilState"/> (whether it tests and writes depth), <see cref="RasterizerState"/>
    /// (culling), <see cref="SamplerState"/> (texture filtering). <c>pass.Apply()</c> uploads the
    /// effect's parameters; the draw call then runs the shaders over the triangles.
    /// <para>
    /// Two stock effects cover every layer: a lit <see cref="BasicEffect"/> configured by
    /// <see cref="SceneLighting.Configure"/>, and an unlit one. No custom shader means no content
    /// pipeline step, at the cost of what <c>BasicEffect</c> can't do (see RENDER_STYLE.md).
    /// </para>
    /// </remarks>
    public sealed class GeometryBatch
    {
        private static readonly BlendState premultipliedAdditive = new()
        {
            Name = "GeometryBatch.PremultipliedAdditive",
            ColorSourceBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
        };

        private readonly List<MeshDraw> meshDraws = [];

        private BasicEffect? litEffect;
        private BasicEffect? unlitEffect;
        private Texture2D? fallbackTexture;

        /// <summary>Geometry rebuilt every frame: anything that moves or changes too often to be worth keeping.</summary>
        public GeometryBuffer Immediate { get; } = new();

        public SceneLighting Lighting { get; } = new();

        /// <summary>One texture for the whole batch (an atlas). Null draws everything untextured white.</summary>
        public Texture2D? Texture { get; set; }

        public SamplerState Sampler { get; set; } = SamplerState.PointClamp;

        /// <summary>
        /// Draw retained meshes from GPU vertex buffers (true) or stream their CPU copies like
        /// immediate geometry (false). The false path is the simpler, older one - a debugging
        /// fallback if the buffers ever draw nothing.
        /// </summary>
        public bool UseVertexBuffers { get; set; } = true;

        public ReadOnlySpan<MeshDraw> MeshDraws => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(meshDraws);

        public int TriangleCount
        {
            get
            {
                var total = Immediate.TriangleCount;
                foreach (var draw in meshDraws)
                {
                    total += draw.Mesh.TriangleCount;
                }
                return total;
            }
        }

        public void Clear()
        {
            Immediate.Clear();
            meshDraws.Clear();
        }

        /// <summary>Draws <paramref name="mesh"/> this frame, through <paramref name="transform"/> into the world's local space.</summary>
        public void Draw(StaticMesh mesh, in Matrix transform)
        {
            if (!mesh.IsEmpty)
            {
                meshDraws.Add(new MeshDraw(mesh, transform));
            }
        }

        /// <summary>Whether a layer's geometry is lit. The software rasterizer asks this too, so both agree.</summary>
        public static bool IsLit(GeometryLayer layer) => layer is GeometryLayer.Opaque or GeometryLayer.Translucent;

        /// <summary>
        /// Draws every layer. Clears depth first, so each call (one per world) composites on its own
        /// and then over whatever was drawn before it.
        /// </summary>
        /// <remarks>
        /// Every device state the pass depends on is set here, because SpriteBatch leaves its own
        /// behind (no depth, back-face culling). Culling is off on purpose: winding is then never a
        /// reason for geometry to vanish (see RENDER_STYLE.md for what turning it on involves).
        /// </remarks>
        public void Flush(GraphicsDevice device, Matrix world, Matrix view, Matrix projection)
        {
            if (Immediate.IsEmpty && meshDraws.Count == 0)
            {
                return;
            }

            litEffect ??= new BasicEffect(device) { TextureEnabled = true, VertexColorEnabled = true };
            unlitEffect ??= new BasicEffect(device) { TextureEnabled = true, VertexColorEnabled = true, LightingEnabled = false };
            if (fallbackTexture is null)
            {
                fallbackTexture = new Texture2D(device, 1, 1);
                fallbackTexture.SetData([Color.White]);
            }

            Lighting.Configure(litEffect);
            var texture = Texture ?? fallbackTexture;
            foreach (var effect in (ReadOnlySpan<BasicEffect>)[litEffect, unlitEffect])
            {
                effect.View = view;
                effect.Projection = projection;
                effect.Texture = texture;
            }

            device.Clear(ClearOptions.DepthBuffer, Color.Transparent, 1f, 0);
            device.RasterizerState = RasterizerState.CullNone;

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

            // Each mesh has its own transform, so its own World and its own Apply: Apply is what
            // pushes the changed matrix (and, when lit, its inverse-transpose for normals) to the GPU.
            foreach (var draw in meshDraws)
            {
                if (!draw.Mesh.HasLayer(layer))
                {
                    continue;
                }

                effect.World = draw.Transform * world;
                effect.CurrentTechnique.Passes[0].Apply();
                device.SamplerStates[0] = Sampler;
                draw.Mesh.DrawLayer(device, layer, UseVertexBuffers);
            }

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
                    device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertexCount, indices, 0, indexCount / 3);
                }
            }
        }
    }
}
