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
    /// <b>CPU copy.</b> The vertices and indices stay in managed arrays after upload, which doubles
    /// the memory (36 bytes a vertex, twice). They let the upload wait for the first draw, where a
    /// device is at hand, and let a refill reuse them.
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

        public int VertexCount { get; private set; }
        public bool IsEmpty => VertexCount == 0;

        /// <summary>Replaces the contents with a copy of <paramref name="source"/>. The source can be cleared and reused straight after.</summary>
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

                // Segments past the new page count are emptied, not freed, so a mesh that shrinks
                // and grows back reuses them.
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
        /// Issues the draw calls for one layer, uploading any segment whose GPU copy is stale. The
        /// caller has already applied the effect pass.
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
                device.SetVertexBuffer(segment.VertexBuffer);
                device.Indices = segment.IndexBuffer;
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
