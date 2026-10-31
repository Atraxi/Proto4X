using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Graphics
{
    /// <summary>
    /// How a primitive is composited. Layers draw in this order, each over everything submitted to
    /// the batch, retained meshes first and then immediate geometry.
    /// </summary>
    public enum GeometryLayer
    {
        /// <summary>Lit by <see cref="SceneLighting"/>, depth-tested and depth-written. Order doesn't matter, depth decides.</summary>
        Opaque = 0,
        /// <summary>Like <see cref="Opaque"/> but unlit: drawn at its vertex colour. Glow panels, screens, indicator fills.</summary>
        Emissive = 1,
        /// <summary>Lit, premultiplied alpha blend, depth-tested but not written, in submission order.</summary>
        Translucent = 2,
        /// <summary>Unlit, additive (glow), depth-tested but not written, in submission order.</summary>
        Additive = 3,
    }

    /// <summary>
    /// Triangles on the CPU, sorted by layer: the common format every geometry producer writes into,
    /// whether the result is drawn this frame (<see cref="GeometryBatch.Immediate"/>) or kept on the
    /// GPU (<see cref="StaticMesh.CopyFrom"/>).
    /// </summary>
    /// <remarks>
    /// Storage is a list of fixed-size pages per layer, reused across <see cref="Clear"/>s, so filling
    /// it allocates nothing once the high-water mark is reached. A page holds at most
    /// <see cref="MaxVerticesPerPage"/> vertices so its indices fit 16 bits, which every graphics
    /// profile can draw; each page is one draw call. Pages are big (about 1.3 MB) - fine for a few
    /// shared scratch buffers, wrong for anything kept per chunk, which is why
    /// <see cref="StaticMesh"/> copies out into exact-size arrays.
    /// </remarks>
    public sealed class GeometryBuffer
    {
        public const int MaxVerticesPerPage = 32_764;
        public const int MaxIndicesPerPage = MaxVerticesPerPage / 4 * 6;
        public const int LayerCount = 4;

        private readonly LayerPages[] layers = [new(), new(), new(), new()];

        /// <summary>A texture coordinate on a white texel of the batch's texture, used by the untextured overloads.</summary>
        public Vector2 WhiteUv { get; set; } = new(0.5f, 0.5f);

        public int PageCount(GeometryLayer layer) => layers[(int)layer].Used;

        internal (MeshVertex[] Vertices, int VertexCount, short[] Indices, int IndexCount) RawPage(GeometryLayer layer, int page)
        {
            var p = layers[(int)layer].Pages[page];
            return (p.Vertices, p.VertexCount, p.Indices, p.IndexCount);
        }

        public bool IsEmpty => layers[0].Used == 0 && layers[1].Used == 0 && layers[2].Used == 0 && layers[3].Used == 0;

        public void Clear()
        {
            foreach (var layer in layers)
            {
                layer.Clear();
            }
        }

        /// <summary>A quad a-b-c-d, in order around its edge, one colour, untextured.</summary>
        public void AddQuad(GeometryLayer layer, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color colour)
        {
            var uv = WhiteUv;
            AddQuad(layer, a, b, c, d, normal, colour, colour, colour, colour, uv, uv, uv, uv);
        }

        /// <summary>
        /// A quad a-b-c-d with a colour and a texture coordinate per corner. <paramref name="normal"/>
        /// is the face's normal in the same space as the positions; unlit layers ignore it.
        /// </summary>
        public void AddQuad(GeometryLayer layer, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal,
            Color colourA, Color colourB, Color colourC, Color colourD, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
        {
            var page = layers[(int)layer].Reserve(4);
            var v = page.VertexCount;
            var vertices = page.Vertices;
            vertices[v] = new MeshVertex(a, normal, colourA, uvA);
            vertices[v + 1] = new MeshVertex(b, normal, colourB, uvB);
            vertices[v + 2] = new MeshVertex(c, normal, colourC, uvC);
            vertices[v + 3] = new MeshVertex(d, normal, colourD, uvD);

            // Two triangles sharing the a-c diagonal.
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

        public void AddTriangle(GeometryLayer layer, Vector3 a, Vector3 b, Vector3 c, Vector3 normal, Color colour)
        {
            var page = layers[(int)layer].Reserve(3);
            var v = page.VertexCount;
            var vertices = page.Vertices;
            var uv = WhiteUv;
            vertices[v] = new MeshVertex(a, normal, colour, uv);
            vertices[v + 1] = new MeshVertex(b, normal, colour, uv);
            vertices[v + 2] = new MeshVertex(c, normal, colour, uv);

            var i = page.IndexCount;
            var indices = page.Indices;
            indices[i] = (short)v;
            indices[i + 1] = (short)(v + 1);
            indices[i + 2] = (short)(v + 2);

            page.VertexCount = v + 3;
            page.IndexCount = i + 3;
        }

        private sealed class Page
        {
            public readonly MeshVertex[] Vertices = new MeshVertex[MaxVerticesPerPage];
            public readonly short[] Indices = new short[MaxIndicesPerPage];
            public int VertexCount;
            public int IndexCount;
        }

        private sealed class LayerPages
        {
            public readonly List<Page> Pages = [];

            /// <summary>Pages in use; the last one is the one being filled.</summary>
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
