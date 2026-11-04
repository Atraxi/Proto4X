using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Graphics
{
    /// <summary>
    /// Geometry built once and drawn many frames: a compact copy of a <see cref="GeometryBuffer"/>,
    /// uploaded to GPU vertex and index buffers on first draw.
    /// </summary>
    /// <remarks>
    /// Immediate geometry is rebuilt and re-sent to the GPU every frame. Unchanging geometry
    /// (terrain, buildings, shadows) lives in GPU memory instead and costs one draw call per frame.
    /// <para>
    /// The CPU arrays are kept after upload (double the memory). That lets the upload wait for a
    /// device, and lets <see cref="CopyFrom"/> refill without reallocating.
    /// </para>
    /// </remarks>
    public sealed class StaticMesh : IDisposable
    {
        // One segment list per GeometryLayer; a segment is one page's worth, so one draw call.
        private readonly List<Segment>[] layers = [[], [], [], []];
        private bool disposed;

        public int VertexCount { get; private set; }
        public bool IsEmpty => VertexCount == 0;

        /// <summary>Replaces the contents with a copy of <paramref name="source"/>, which can be cleared and reused straight after.</summary>
        public void CopyFrom(GeometryBuffer source)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            var vertexCount = 0;

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
                    vertexCount += vCount;
                }

                // Segments beyond the new page count are emptied, not freed, so a mesh that shrinks
                // then grows back reuses them.
                for (var unused = pages; unused < segments.Count; unused++)
                {
                    segments[unused].Empty();
                }
            }

            VertexCount = vertexCount;
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

        /// <summary>
        /// Draws one layer, uploading any segment whose GPU copy is stale first. The caller has
        /// already set the effect's parameters and applied its pass.
        /// </summary>
        internal void DrawLayer(GraphicsDevice device, GeometryLayer layer)
        {
            foreach (var segment in layers[(int)layer])
            {
                if (segment.IndexCount == 0)
                {
                    continue;
                }

                segment.Upload(device);
                // Bind the buffers, then draw by index. Nothing crosses the bus here.
                device.SetVertexBuffer(segment.VertexBuffer);
                device.Indices = segment.IndexBuffer;
                // (primitive type, base vertex, start index, triangle count)
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, segment.IndexCount / 3);
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            // GPU buffers are unmanaged; the garbage collector can't see their size, so free them explicitly.
            foreach (var layer in layers)
            {
                foreach (var segment in layer)
                {
                    segment.ReleaseGpu();
                }
                layer.Clear();
            }
        }

        /// <summary>One page's worth of one layer: at most 32,764 vertices, so 16-bit indices.</summary>
        private sealed class Segment
        {
            public MeshVertex[] Vertices = [];
            public short[] Indices = [];
            public int VertexCount;
            public int IndexCount;
            public VertexBuffer? VertexBuffer;
            public IndexBuffer? IndexBuffer;
            // True when the CPU arrays hold newer data than the GPU buffers.
            private bool gpuStale = true;

            public void Fill(MeshVertex[] vertices, int vertexCount, short[] indices, int indexCount)
            {
                // Grow-only: arrays are reused whenever the new content fits.
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
            /// Copies the CPU arrays to the GPU. Buffers are recreated only when the content has
            /// outgrown them; otherwise <c>SetData</c> overwrites in place.
            /// </summary>
            public void Upload(GraphicsDevice device)
            {
                if (!gpuStale && VertexBuffer is not null)
                {
                    return;
                }

                // WriteOnly promises the CPU never reads the buffer back, so the driver can put it
                // in the memory the GPU reads fastest.
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

                // Only the used part: the arrays may be longer than the content.
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
