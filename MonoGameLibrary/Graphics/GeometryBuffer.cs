using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Graphics
{
    /// <summary>
    /// How a primitive is composited. Layers draw in this order; within one, retained meshes draw
    /// before immediate geometry.
    /// </summary>
    public enum GeometryLayer
    {
        /// <summary>Lit, depth-tested, depth-written. Submission order doesn't matter.</summary>
        Opaque = 0,
        /// <summary>Unlit: drawn at vertex colour. Glow panels, indicator fills.</summary>
        Emissive = 1,
        /// <summary>Lit, alpha-blended, depth-tested but not written. Order matters (submission order).</summary>
        Translucent = 2,
        /// <summary>Unlit, additive (glow), depth-tested but not written.</summary>
        Additive = 3,
    }

    /// <summary>
    /// Triangles on the CPU, sorted by layer. Every geometry producer writes here, whether the
    /// result is drawn this frame (<see cref="GeometryBatch.Immediate"/>) or kept on the GPU
    /// (<see cref="StaticMesh.CopyFrom"/>).
    /// </summary>
    /// <remarks>
    /// Storage is fixed-size pages per layer, reused across <see cref="Clear"/>, so filling allocates
    /// nothing at steady state. A page is one draw call. Pages are ~1.3 MB: fine for a few shared
    /// scratch buffers, too big to keep per chunk (<see cref="StaticMesh"/> copies out compactly).
    /// </remarks>
    public sealed class GeometryBuffer
    {
        // 32,764 is the largest multiple of 4 below 32,767 (short.MaxValue), so whole quads fit
        // and every index fits a signed 16-bit value.
        public const int MaxVerticesPerPage = 32_764;
        // A quad is 4 vertices and 6 indices.
        public const int MaxIndicesPerPage = MaxVerticesPerPage / 4 * 6;
        public const int LayerCount = 4;

        // Indexed by (int)GeometryLayer.
        private readonly LayerPages[] layers = [new(), new(), new(), new()];

        /// <summary>UV of a white texel in the batch's texture, so untextured geometry samples white.</summary>
        public Vector2 WhiteUv { get; set; } = new(0.5f, 0.5f);

        public int PageCount(GeometryLayer layer) => layers[(int)layer].Used;

        // Raw arrays are only valid up to the returned counts; the rest is stale data from earlier use.
        internal (MeshVertex[] Vertices, int VertexCount, short[] Indices, int IndexCount) RawPage(GeometryLayer layer, int page)
        {
            var p = layers[(int)layer].Pages[page];
            return (p.Vertices, p.VertexCount, p.Indices, p.IndexCount);
        }

        public bool IsEmpty => layers[0].Used == 0 && layers[1].Used == 0 && layers[2].Used == 0 && layers[3].Used == 0;

        /// <summary>Forgets the contents but keeps the pages, so refilling allocates nothing.</summary>
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
        /// A quad with a colour and UV per corner. <paramref name="normal"/> is the face normal in the
        /// same space as the positions; unlit layers ignore it.
        /// </summary>
        public void AddQuad(GeometryLayer layer, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal,
            Color colourA, Color colourB, Color colourC, Color colourD, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
        {
            // Four vertices of a quad must land in one page, since indices are page-local.
            var page = layers[(int)layer].Reserve(4);
            var v = page.VertexCount;
            var vertices = page.Vertices;

            // All four corners share the face normal, so the face shades flat.
            vertices[v] = new MeshVertex(a, normal, colourA, uvA);
            vertices[v + 1] = new MeshVertex(b, normal, colourB, uvB);
            vertices[v + 2] = new MeshVertex(c, normal, colourC, uvC);
            vertices[v + 3] = new MeshVertex(d, normal, colourD, uvD);

            // Two triangles sharing the a-c diagonal, same winding as the corner order.
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

            /// <summary>The page with room for <paramref name="vertices"/> more, starting a new (or recycled) one if the current is full.</summary>
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

                // Allocate only when every existing page is in use; otherwise recycle the next one.
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
                // Counts are reset lazily, in Reserve, when a page is picked up again.
                Used = 0;
            }
        }
    }
}
