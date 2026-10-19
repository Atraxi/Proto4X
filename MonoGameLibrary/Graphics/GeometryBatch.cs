using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Graphics
{
    /// <summary>
    /// How a primitive is composited. Layers flush in this order, each over the whole batch.
    /// </summary>
    public enum GeometryLayer
    {
        /// <summary>Depth-tested and depth-written, alpha-tested at 50%. Order doesn't matter, depth decides.</summary>
        Opaque = 0,
        /// <summary>Premultiplied alpha blend, depth-tested but not written, in submission order.</summary>
        Translucent = 1,
        /// <summary>Additive (glow), depth-tested but not written, in submission order.</summary>
        Additive = 2,
    }

    /// <summary>
    /// Coloured, textured triangles, built on the CPU each frame and drawn in one pass per layer
    /// through a world/view/projection - the thing SpriteBatch can't do: arbitrary quads
    /// (parallelograms, extruded faces), real depth, and 3D.
    /// </summary>
    /// <remarks>
    /// Storage is a list of fixed-size pages per layer, reused across frames, so building a frame
    /// allocates nothing once the high-water mark is reached. A page holds at most
    /// <see cref="MaxVerticesPerPage"/> vertices so its indices fit a signed 16-bit index buffer on
    /// any graphics profile; each page is one draw call. The CPU-side data is public read-only so a
    /// non-GPU consumer (a software rasterizer, a test) can see exactly what would be drawn.
    /// </remarks>
    public sealed class GeometryBatch
    {
        public const int MaxVerticesPerPage = 32_764;
        public const int MaxIndicesPerPage = MaxVerticesPerPage / 4 * 6;
        public const int LayerCount = 3;

        private static readonly BlendState premultipliedAdditive = new()
        {
            Name = "GeometryBatch.PremultipliedAdditive",
            ColorSourceBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
        };

        private readonly LayerPages[] layers = [new(), new(), new()];

        private AlphaTestEffect? opaqueEffect;
        private BasicEffect? blendedEffect;
        private Texture2D? fallbackTexture;

        /// <summary>One texture for the whole batch (an atlas). Null draws everything untextured white.</summary>
        public Texture2D? Texture { get; set; }

        /// <summary>A texture coordinate on a white texel of <see cref="Texture"/>, used by the untextured overloads.</summary>
        public Vector2 WhiteUv { get; set; } = new(0.5f, 0.5f);

        public SamplerState Sampler { get; set; } = SamplerState.PointClamp;

        public int PageCount(GeometryLayer layer) => layers[(int)layer].Used;

        public ReadOnlySpan<VertexPositionColorTexture> PageVertices(GeometryLayer layer, int page)
        {
            var p = layers[(int)layer].Pages[page];
            return new ReadOnlySpan<VertexPositionColorTexture>(p.Vertices, 0, p.VertexCount);
        }

        public ReadOnlySpan<short> PageIndices(GeometryLayer layer, int page)
        {
            var p = layers[(int)layer].Pages[page];
            return new ReadOnlySpan<short>(p.Indices, 0, p.IndexCount);
        }

        public int TriangleCount
        {
            get
            {
                var total = 0;
                foreach (var layer in layers)
                {
                    for (var i = 0; i < layer.Used; i++)
                    {
                        total += layer.Pages[i].IndexCount / 3;
                    }
                }
                return total;
            }
        }

        public void Clear()
        {
            foreach (var layer in layers)
            {
                layer.Clear();
            }
        }

        /// <summary>A quad a-b-c-d, in winding order around its edge, untextured.</summary>
        public void AddQuad(GeometryLayer layer, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color colour)
        {
            var uv = WhiteUv;
            AddQuad(layer, a, b, c, d, colour, colour, colour, colour, uv, uv, uv, uv);
        }

        /// <summary>A quad a-b-c-d with one colour and a texture coordinate per corner.</summary>
        public void AddQuad(GeometryLayer layer, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color colour, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
        {
            AddQuad(layer, a, b, c, d, colour, colour, colour, colour, uvA, uvB, uvC, uvD);
        }

        public void AddQuad(GeometryLayer layer, Vector3 a, Vector3 b, Vector3 c, Vector3 d,
            Color colourA, Color colourB, Color colourC, Color colourD, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
        {
            var page = layers[(int)layer].Reserve(4);
            var v = page.VertexCount;
            var vertices = page.Vertices;
            vertices[v] = new VertexPositionColorTexture(a, colourA, uvA);
            vertices[v + 1] = new VertexPositionColorTexture(b, colourB, uvB);
            vertices[v + 2] = new VertexPositionColorTexture(c, colourC, uvC);
            vertices[v + 3] = new VertexPositionColorTexture(d, colourD, uvD);

            var i = page.IndexCount;
            var indices = page.Indices;
            indices[i] = (short)v;
            indices[i + 1] = (short)(v + 1);
            indices[i + 2] = (short)(v + 2);
            indices[i + 3] = (short)v;
            indices[i + 4] = (short)(v + 2);
            indices[i + 5] = (short)(v + 3);

            page.VertexCount = v + 4;
            page.IndexCount = i + 6;
        }

        public void AddTriangle(GeometryLayer layer, Vector3 a, Vector3 b, Vector3 c, Color colour)
        {
            var uv = WhiteUv;
            AddTriangle(layer, a, b, c, colour, colour, colour, uv, uv, uv);
        }

        public void AddTriangle(GeometryLayer layer, Vector3 a, Vector3 b, Vector3 c, Color colourA, Color colourB, Color colourC, Vector2 uvA, Vector2 uvB, Vector2 uvC)
        {
            var page = layers[(int)layer].Reserve(3);
            var v = page.VertexCount;
            var vertices = page.Vertices;
            vertices[v] = new VertexPositionColorTexture(a, colourA, uvA);
            vertices[v + 1] = new VertexPositionColorTexture(b, colourB, uvB);
            vertices[v + 2] = new VertexPositionColorTexture(c, colourC, uvC);

            var i = page.IndexCount;
            var indices = page.Indices;
            indices[i] = (short)v;
            indices[i + 1] = (short)(v + 1);
            indices[i + 2] = (short)(v + 2);

            page.VertexCount = v + 3;
            page.IndexCount = i + 3;
        }

        /// <summary>
        /// Draws every layer. Clears depth first, so each call (one per world) composites on its own
        /// and then over whatever was drawn before it.
        /// </summary>
        /// <remarks>
        /// Every device state the pass depends on is set here, because SpriteBatch leaves its own
        /// behind (no depth, back-face culling). Culling is off on purpose: winding is then never a
        /// reason for geometry to vanish.
        /// </remarks>
        public void Flush(GraphicsDevice device, Matrix world, Matrix view, Matrix projection)
        {
            if (layers[0].Used == 0 && layers[1].Used == 0 && layers[2].Used == 0)
            {
                return;
            }

            opaqueEffect ??= new AlphaTestEffect(device)
            {
                VertexColorEnabled = true,
                AlphaFunction = CompareFunction.Greater,
                ReferenceAlpha = 127,
            };
            blendedEffect ??= new BasicEffect(device)
            {
                TextureEnabled = true,
                VertexColorEnabled = true,
                LightingEnabled = false,
            };
            if (fallbackTexture is null)
            {
                fallbackTexture = new Texture2D(device, 1, 1);
                fallbackTexture.SetData([Color.White]);
            }

            var texture = Texture ?? fallbackTexture;
            device.Clear(ClearOptions.DepthBuffer, Color.Transparent, 1f, 0);
            device.RasterizerState = RasterizerState.CullNone;

            opaqueEffect.World = world;
            opaqueEffect.View = view;
            opaqueEffect.Projection = projection;
            opaqueEffect.Texture = texture;
            blendedEffect.World = world;
            blendedEffect.View = view;
            blendedEffect.Projection = projection;
            blendedEffect.Texture = texture;

            DrawLayer(device, layers[(int)GeometryLayer.Opaque], opaqueEffect, BlendState.Opaque, DepthStencilState.Default);
            DrawLayer(device, layers[(int)GeometryLayer.Translucent], blendedEffect, BlendState.AlphaBlend, DepthStencilState.DepthRead);
            DrawLayer(device, layers[(int)GeometryLayer.Additive], blendedEffect, premultipliedAdditive, DepthStencilState.DepthRead);
        }

        private void DrawLayer(GraphicsDevice device, LayerPages layer, Effect effect, BlendState blend, DepthStencilState depth)
        {
            if (layer.Used == 0)
            {
                return;
            }

            device.BlendState = blend;
            device.DepthStencilState = depth;
            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.SamplerStates[0] = Sampler;
                for (var i = 0; i < layer.Used; i++)
                {
                    var page = layer.Pages[i];
                    if (page.IndexCount > 0)
                    {
                        device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, page.Vertices, 0, page.VertexCount, page.Indices, 0, page.IndexCount / 3);
                    }
                }
            }
        }

        private sealed class Page
        {
            public readonly VertexPositionColorTexture[] Vertices = new VertexPositionColorTexture[MaxVerticesPerPage];
            public readonly short[] Indices = new short[MaxIndicesPerPage];
            public int VertexCount;
            public int IndexCount;
        }

        private sealed class LayerPages
        {
            public readonly List<Page> Pages = [];

            /// <summary>Pages in use this frame; the last one is the one being filled.</summary>
            public int Used;

            public Page Reserve(int vertices)
            {
                if (Used > 0)
                {
                    var current = Pages[Used - 1];
                    if (current.VertexCount + vertices <= MaxVerticesPerPage)
                    {
                        return current;
                    }
                }

                if (Used == Pages.Count)
                {
                    Pages.Add(new Page());
                }

                var next = Pages[Used++];
                next.VertexCount = 0;
                next.IndexCount = 0;
                return next;
            }

            public void Clear()
            {
                Used = 0;
            }
        }
    }
}
