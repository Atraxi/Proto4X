using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;

namespace Proto4X.Factory.Rendering.Software
{
    /// <summary>
    /// Draws a <see cref="GeometryBatch"/> on the CPU, with the same rules the GPU pass uses: opaque
    /// layer alpha-tested at 50% with depth test and write, then translucent (premultiplied over)
    /// and additive layers depth-tested only, in submission order.
    /// </summary>
    /// <remarks>
    /// Exists so rendering can be looked at without launching the game: headless snapshots run the
    /// real draw systems into a batch and rasterize it here. Also used to bake sprites from 3D
    /// models. Clip-space conventions follow the GPU (D3D-style depth, z in [0, w]); triangles are
    /// clipped against the near and far planes, and x/y by the pixel loop. Coverage uses a strict
    /// edge rule with an antisymmetric tie-break, so shared edges are drawn exactly once - which
    /// matters for translucent quads.
    /// </remarks>
    public sealed class SoftwareRasterizer
    {
        private readonly Vector4[] colour;
        private readonly float[] depth;

        public SoftwareRasterizer(int width, int height)
        {
            Width = width;
            Height = height;
            colour = new Vector4[width * height];
            depth = new float[width * height];
        }

        public int Width { get; }
        public int Height { get; }

        public void Clear(Color background)
        {
            Array.Fill(colour, background.ToVector4());
            ClearDepth();
        }

        public void ClearDepth() => Array.Fill(depth, 1f);

        /// <summary>The same three layer passes as <see cref="GeometryBatch.Flush"/>, depth cleared first.</summary>
        public void Draw(GeometryBatch batch, Matrix world, Matrix view, Matrix projection, CpuImage? texture, bool linear)
        {
            ClearDepth();
            var transform = world * view * projection;
            for (var layer = 0; layer < GeometryBatch.LayerCount; layer++)
            {
                var kind = (GeometryLayer)layer;
                for (var page = 0; page < batch.PageCount(kind); page++)
                {
                    var vertices = batch.PageVertices(kind, page);
                    var indices = batch.PageIndices(kind, page);
                    for (var i = 0; i + 2 < indices.Length; i += 3)
                    {
                        DrawTriangle(vertices[indices[i]], vertices[indices[i + 1]], vertices[indices[i + 2]], transform, kind, texture, linear);
                    }
                }
            }
        }

        /// <summary>Box-filters down by <paramref name="factor"/> into a byte image (premultiplied, opaque if the clear was).</summary>
        public CpuImage Resolve(int factor = 1)
        {
            var w = Width / factor;
            var h = Height / factor;
            var image = new CpuImage(w, h);
            var scale = 1f / (factor * factor);
            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var sum = Vector4.Zero;
                    for (var sy = 0; sy < factor; sy++)
                    {
                        var row = (y * factor + sy) * Width + x * factor;
                        for (var sx = 0; sx < factor; sx++)
                        {
                            sum += colour[row + sx];
                        }
                    }

                    image.At(x, y) = new Color(Vector4.Clamp(sum * scale, Vector4.Zero, Vector4.One));
                }
            }

            return image;
        }

        private struct ClipVertex
        {
            public Vector4 Position;
            public Vector4 Colour;
            public Vector2 Uv;

            public static ClipVertex Lerp(in ClipVertex a, in ClipVertex b, float t) => new()
            {
                Position = Vector4.Lerp(a.Position, b.Position, t),
                Colour = Vector4.Lerp(a.Colour, b.Colour, t),
                Uv = Vector2.Lerp(a.Uv, b.Uv, t),
            };
        }

        private void DrawTriangle(in VertexPositionColorTexture a, in VertexPositionColorTexture b, in VertexPositionColorTexture c, in Matrix transform, GeometryLayer layer, CpuImage? texture, bool linear)
        {
            Span<ClipVertex> polygon = stackalloc ClipVertex[8];
            Span<ClipVertex> scratch = stackalloc ClipVertex[8];
            polygon[0] = ToClip(a, transform);
            polygon[1] = ToClip(b, transform);
            polygon[2] = ToClip(c, transform);
            var count = 3;

            // Near (z >= 0) then far (z <= w).
            count = ClipAgainst(polygon, count, scratch, near: true);
            count = ClipAgainst(scratch, count, polygon, near: false);
            for (var i = 1; i + 1 < count; i++)
            {
                RasterizeClipped(polygon[0], polygon[i], polygon[i + 1], layer, texture, linear);
            }
        }

        private static ClipVertex ToClip(in VertexPositionColorTexture v, in Matrix transform) => new()
        {
            Position = Vector4.Transform(new Vector4(v.Position, 1f), transform),
            Colour = v.Color.ToVector4(),
            Uv = v.TextureCoordinate,
        };

        private static int ClipAgainst(Span<ClipVertex> input, int count, Span<ClipVertex> output, bool near)
        {
            var written = 0;
            for (var i = 0; i < count; i++)
            {
                ref var current = ref input[i];
                ref var next = ref input[(i + 1) % count];
                var dc = near ? current.Position.Z : current.Position.W - current.Position.Z;
                var dn = near ? next.Position.Z : next.Position.W - next.Position.Z;
                if (dc >= 0)
                {
                    output[written++] = current;
                }

                if ((dc >= 0) != (dn >= 0))
                {
                    output[written++] = ClipVertex.Lerp(current, next, dc / (dc - dn));
                }
            }

            return written;
        }

        private void RasterizeClipped(in ClipVertex v0, in ClipVertex v1, in ClipVertex v2, GeometryLayer layer, CpuImage? texture, bool linear)
        {
            if (v0.Position.W <= 1e-6f || v1.Position.W <= 1e-6f || v2.Position.W <= 1e-6f)
            {
                return;
            }

            var p0 = ToScreen(v0.Position);
            var p1 = ToScreen(v1.Position);
            var p2 = ToScreen(v2.Position);
            var area = Edge(p0, p1, p2);
            if (MathF.Abs(area) < 1e-9f)
            {
                return;
            }

            var minX = Math.Max(0, (int)MathF.Floor(MathF.Min(p0.X, MathF.Min(p1.X, p2.X))));
            var maxX = Math.Min(Width - 1, (int)MathF.Ceiling(MathF.Max(p0.X, MathF.Max(p1.X, p2.X))));
            var minY = Math.Max(0, (int)MathF.Floor(MathF.Min(p0.Y, MathF.Min(p1.Y, p2.Y))));
            var maxY = Math.Min(Height - 1, (int)MathF.Ceiling(MathF.Max(p0.Y, MathF.Max(p1.Y, p2.Y))));
            if (minX > maxX || minY > maxY)
            {
                return;
            }

            var sign = area > 0 ? 1f : -1f;
            var invArea = 1f / area;
            var w0 = 1f / v0.Position.W;
            var w1 = 1f / v1.Position.W;
            var w2 = 1f / v2.Position.W;
            var include12 = TieBreak(p1, p2);
            var include20 = TieBreak(p2, p0);
            var include01 = TieBreak(p0, p1);

            for (var y = minY; y <= maxY; y++)
            {
                var row = y * Width;
                for (var x = minX; x <= maxX; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    var e0 = Edge(p1, p2, p) * sign;
                    var e1 = Edge(p2, p0, p) * sign;
                    var e2 = Edge(p0, p1, p) * sign;
                    if (!Covers(e0, include12) || !Covers(e1, include20) || !Covers(e2, include01))
                    {
                        continue;
                    }

                    var l0 = e0 * sign * invArea;
                    var l1 = e1 * sign * invArea;
                    var l2 = 1f - l0 - l1;
                    var z = l0 * p0.Z + l1 * p1.Z + l2 * p2.Z;
                    var index = row + x;
                    if (z > depth[index])
                    {
                        continue;
                    }

                    // Perspective-correct attributes.
                    var q0 = l0 * w0;
                    var q1 = l1 * w1;
                    var q2 = l2 * w2;
                    var qs = 1f / (q0 + q1 + q2);
                    var tint = (v0.Colour * q0 + v1.Colour * q1 + v2.Colour * q2) * qs;
                    var source = tint;
                    if (texture is not null)
                    {
                        var uv = (v0.Uv * q0 + v1.Uv * q1 + v2.Uv * q2) * qs;
                        source *= texture.Sample(uv, linear);
                    }

                    switch (layer)
                    {
                        case GeometryLayer.Opaque:
                            if (source.W <= 127f / 255f)
                            {
                                continue;
                            }

                            colour[index] = source;
                            depth[index] = z;
                            break;
                        case GeometryLayer.Translucent:
                            colour[index] = source + colour[index] * (1f - source.W);
                            break;
                        default:
                            colour[index] += source;
                            break;
                    }
                }
            }
        }

        private Vector3 ToScreen(Vector4 clip)
        {
            // Snapped to 1/256 pixel so shared vertices land identically from either triangle.
            var inv = 1f / clip.W;
            var x = MathF.Round((clip.X * inv + 1f) * 0.5f * Width * 256f) / 256f;
            var y = MathF.Round((1f - clip.Y * inv) * 0.5f * Height * 256f) / 256f;
            return new Vector3(x, y, clip.Z * inv);
        }

        private static float Edge(Vector3 a, Vector3 b, Vector3 p) => (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);

        /// <summary>
        /// The edge function, always evaluated from the lexicographically smaller endpoint, so the two
        /// triangles sharing an edge get exact negations of each other - otherwise rounding can put
        /// a pixel outside both and leave a hole along the shared diagonal.
        /// </summary>
        private static float Edge(Vector3 a, Vector3 b, Vector2 p)
        {
            if (a.X < b.X || (a.X == b.X && a.Y < b.Y))
            {
                return (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
            }

            return -((a.X - b.X) * (p.Y - b.Y) - (a.Y - b.Y) * (p.X - b.X));
        }

        /// <summary>For a pixel exactly on edge a-b: true for exactly one of a-b and b-a, so neighbours never both draw it.</summary>
        private static bool TieBreak(Vector3 a, Vector3 b)
        {
            var dy = b.Y - a.Y;
            return dy > 0 || (dy == 0 && b.X < a.X);
        }

        private static bool Covers(float edge, bool includeTie) => edge > 0 || (edge == 0 && includeTie);
    }
}
