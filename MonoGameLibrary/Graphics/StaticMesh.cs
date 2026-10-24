using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Graphics
{
    /// <summary>
    /// Geometry built once and drawn many frames: a compact copy of a <see cref="GeometryBuffer"/>,
    /// uploaded to GPU vertex and index buffers the first time it is drawn.
    /// </summary>
    /// <remarks>
    /// <b>Why it exists.</b> <see cref="GeometryBatch.Immediate"/> geometry is rebuilt on the CPU
    /// and re-sent to the GPU every frame. Anything that doesn't change between frames - terrain,
    /// buildings, their shadows - should be built once, live in GPU memory, and cost one draw call
    /// per frame. That's this.
    /// <para>
    /// <b>CPU copy.</b> The vertices and indices stay in managed arrays after upload. That doubles
    /// the memory (36 bytes a vertex, twice), but it lets the software rasterizer draw exactly what
    /// the GPU draws, lets <see cref="GeometryBatch.UseVertexBuffers"/> fall back to the
    /// immediate draw path for debugging, and makes a device reset recoverable.
    /// </para>
    /// <para>
    /// <b>Reuse.</b> <see cref="CopyFrom"/> refills an existing mesh, keeping its arrays and GPU
    /// buffers when the new content fits, so a chunk that is rebuilt often doesn't churn memory.
    /// </para>
    /// </remarks>
    public sealed class StaticMesh : IDisposable
    {
        private readonly List<Segment>[] layers = [[], [], [], []];
        private bool disposed;

        /// <summary>Bounds of every vertex, in the mesh's own space. Empty mesh: a zero box at the origin.</summary>
        public BoundingBox Bounds { get; private set; }

        public int VertexCount { get; private set; }
        public int TriangleCount { get; private set; }
        public bool IsEmpty => VertexCount == 0;

        /// <summary>Bytes of vertex and index data held on the CPU (the GPU holds the same again once uploaded).</summary>
        public long CpuBytes
        {
            get
            {
                long total = 0;
                foreach (var layer in layers)
                {
                    foreach (var segment in layer)
                    {
                        total += segment.Vertices.Length * 36L + segment.Indices.Length * 2L;
                    }
                }
                return total;
            }
        }

        /// <summary>Replaces the contents with a copy of <paramref name="source"/>. The source can be cleared and reused straight after.</summary>
        public void CopyFrom(GeometryBuffer source)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            var vertexCount = 0;
            var triangleCount = 0;

            for (var layer = 0; layer < GeometryBuffer.LayerCount; layer++)
            {
                var segments = layers[layer];
                var pages = source.PageCount((GeometryLayer)layer);
                for (var page = 0; page < pages; page++)
                {
                    var (vertices, vCount, indices, iCount) = source.RawPage((GeometryLayer)layer, page);
                    if (page == segments.Count)
                    {
                        segments.Add(new Segment());
                    }

                    segments[page].Fill(vertices, vCount, indices, iCount);
                    for (var v = 0; v < vCount; v++)
                    {
                        min = Vector3.Min(min, vertices[v].Position);
                        max = Vector3.Max(max, vertices[v].Position);
                    }

                    vertexCount += vCount;
                    triangleCount += iCount / 3;
                }

                // Segments past the new page count are emptied, not freed, so a mesh that shrinks
                // and grows back reuses them.
                for (var unused = pages; unused < segments.Count; unused++)
                {
                    segments[unused].Empty();
                }
            }

            VertexCount = vertexCount;
            TriangleCount = triangleCount;
            Bounds = vertexCount > 0 ? new BoundingBox(min, max) : default;
        }

        public bool HasLayer(GeometryLayer layer)
        {
            foreach (var segment in layers[(int)layer])
            {
                if (segment.IndexCount > 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>One layer's triangles, for CPU consumers: (vertices, indices) per segment.</summary>
        public int SegmentCount(GeometryLayer layer) => layers[(int)layer].Count;
        public ReadOnlySpan<MeshVertex> SegmentVertices(GeometryLayer layer, int segment) => layers[(int)layer][segment].VertexSpan;
        public ReadOnlySpan<short> SegmentIndices(GeometryLayer layer, int segment) => layers[(int)layer][segment].IndexSpan;

        /// <summary>
        /// Issues the draw calls for one layer. The caller has already applied the effect pass. With
        /// <paramref name="useVertexBuffers"/> the data comes from GPU buffers, uploaded here if
        /// stale; without, it's streamed from the CPU copy like immediate geometry.
        /// </summary>
        internal void DrawLayer(GraphicsDevice device, GeometryLayer layer, bool useVertexBuffers)
        {
            foreach (var segment in layers[(int)layer])
            {
                if (segment.IndexCount == 0)
                {
                    continue;
                }

                if (useVertexBuffers)
                {
                    segment.Upload(device);
                    device.SetVertexBuffer(segment.VertexBuffer);
                    device.Indices = segment.IndexBuffer;
                    device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, segment.IndexCount / 3);
                }
                else
                {
                    device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, segment.Vertices, 0, segment.VertexCount, segment.Indices, 0, segment.IndexCount / 3);
                }
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            foreach (var layer in layers)
            {
                foreach (var segment in layer)
                {
                    segment.ReleaseGpu();
                }
                layer.Clear();
            }
        }

        /// <summary>One page's worth of a layer: at most 32,764 vertices, so 16-bit indices.</summary>
        private sealed class Segment
        {
            public MeshVertex[] Vertices = [];
            public short[] Indices = [];
            public int VertexCount;
            public int IndexCount;
            public VertexBuffer? VertexBuffer;
            public IndexBuffer? IndexBuffer;
            private bool gpuStale = true;

            public ReadOnlySpan<MeshVertex> VertexSpan => new(Vertices, 0, VertexCount);
            public ReadOnlySpan<short> IndexSpan => new(Indices, 0, IndexCount);

            public void Fill(MeshVertex[] vertices, int vertexCount, short[] indices, int indexCount)
            {
                if (Vertices.Length < vertexCount)
                {
                    Vertices = new MeshVertex[vertexCount];
                }

                if (Indices.Length < indexCount)
                {
                    Indices = new short[indexCount];
                }

                Array.Copy(vertices, Vertices, vertexCount);
                Array.Copy(indices, Indices, indexCount);
                VertexCount = vertexCount;
                IndexCount = indexCount;
                gpuStale = true;
            }

            public void Empty()
            {
                VertexCount = 0;
                IndexCount = 0;
                gpuStale = true;
            }

            /// <summary>
            /// Copies the CPU arrays into GPU buffers. A buffer is recreated only when the content
            /// outgrew it; otherwise <c>SetData</c> overwrites it in place. <c>WriteOnly</c> tells
            /// the driver the CPU will never read it back, which lets it place the buffer in
            /// memory the GPU reads fastest.
            /// </summary>
            public void Upload(GraphicsDevice device)
            {
                if (!gpuStale && VertexBuffer is not null)
                {
                    return;
                }

                if (VertexBuffer is null || VertexBuffer.VertexCount < VertexCount)
                {
                    VertexBuffer?.Dispose();
                    VertexBuffer = new VertexBuffer(device, MeshVertex.VertexDeclaration, Vertices.Length, BufferUsage.WriteOnly);
                }

                if (IndexBuffer is null || IndexBuffer.IndexCount < IndexCount)
                {
                    IndexBuffer?.Dispose();
                    IndexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, Indices.Length, BufferUsage.WriteOnly);
                }

                VertexBuffer.SetData(Vertices, 0, VertexCount);
                IndexBuffer.SetData(Indices, 0, IndexCount);
                gpuStale = false;
            }

            public void ReleaseGpu()
            {
                VertexBuffer?.Dispose();
                IndexBuffer?.Dispose();
                VertexBuffer = null;
                IndexBuffer = null;
                gpuStale = true;
            }
        }
    }
}
