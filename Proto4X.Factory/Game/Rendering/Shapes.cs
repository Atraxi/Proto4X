using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using System;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// Flat shapes for the geometry pass, in a surface's grid-local tile units at a fixed depth. The
    /// geometry-batch replacement for <see cref="Primitives"/>: no per-quad frame transform, because
    /// the scheduler applies the surface's transform on the GPU.
    /// </summary>
    public static class Shapes
    {
        /// <summary>Axis-aligned rectangle from its top-left corner.</summary>
        public static void FillRect(GeometryBatch batch, GeometryLayer layer, float x, float y, float width, float height, float z, Color colour)
        {
            batch.AddQuad(layer, new Vector3(x, y, z), new Vector3(x + width, y, z), new Vector3(x + width, y + height, z), new Vector3(x, y + height, z), colour);
        }

        /// <summary>Rectangle centred on <paramref name="centre"/>, rotated about it.</summary>
        public static void FillCentred(GeometryBatch batch, GeometryLayer layer, Vector2 centre, Vector2 size, float rotation, float z, Color colour)
        {
            var half = size * 0.5f;
            if (rotation == 0f)
            {
                FillRect(batch, layer, centre.X - half.X, centre.Y - half.Y, size.X, size.Y, z, colour);
                return;
            }

            var cos = MathF.Cos(rotation);
            var sin = MathF.Sin(rotation);
            var ax = new Vector2(cos, sin) * half.X;
            var ay = new Vector2(-sin, cos) * half.Y;
            batch.AddQuad(layer, At(centre - ax - ay, z), At(centre + ax - ay, z), At(centre + ax + ay, z), At(centre - ax + ay, z), colour);
        }

        /// <summary>A line as a quad, <paramref name="thickness"/> in tiles.</summary>
        public static void Line(GeometryBatch batch, GeometryLayer layer, Vector2 from, Vector2 to, float thickness, float z, Color colour)
        {
            var delta = to - from;
            var length = delta.Length();
            if (length < 1e-5f)
            {
                return;
            }

            var normal = new Vector2(-delta.Y, delta.X) / length * (thickness * 0.5f);
            batch.AddQuad(layer, At(from + normal, z), At(to + normal, z), At(to - normal, z), At(from - normal, z), colour);
        }

        public static void Outline(GeometryBatch batch, GeometryLayer layer, float x, float y, float width, float height, float thickness, float z, Color colour)
        {
            FillRect(batch, layer, x, y, width, thickness, z, colour);
            FillRect(batch, layer, x, y + height - thickness, width, thickness, z, colour);
            FillRect(batch, layer, x, y + thickness, thickness, height - 2 * thickness, z, colour);
            FillRect(batch, layer, x + width - thickness, y + thickness, thickness, height - 2 * thickness, z, colour);
        }

        /// <summary>A regular polygon approximating a disc.</summary>
        public static void Disc(GeometryBatch batch, GeometryLayer layer, Vector2 centre, float radius, int segments, float z, Color colour)
        {
            var previous = centre + new Vector2(radius, 0);
            for (var i = 1; i <= segments; i++)
            {
                var angle = MathHelper.TwoPi * i / segments;
                var next = centre + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                batch.AddTriangle(layer, At(centre, z), At(previous, z), At(next, z), colour);
                previous = next;
            }
        }

        public static Vector3 At(Vector2 point, float z) => new(point.X, point.Y, z);
    }

    /// <summary>
    /// Draw order for flat (top-down) styles, as geometry depth: larger is nearer the viewer. The
    /// orthographic projection takes view-space z in [-1, 1].
    /// </summary>
    public static class FlatDepth
    {
        public const float Terrain = -0.90f;
        public const float Ore = -0.88f;
        public const float Belt = -0.80f;
        public const float BeltDetail = -0.78f;
        public const float BeltItem = -0.74f;
        public const float BuildingShadow = -0.70f;
        public const float Building = -0.60f;
        public const float BuildingInset = -0.58f;
        public const float BuildingDetail = -0.56f;
        public const float BuildingFill = -0.54f;
        public const float Inserter = -0.40f;
        public const float InserterArm = -0.38f;
        public const float Held = -0.36f;
        public const float Bar = -0.30f;
        public const float BarFill = -0.29f;
        public const float Overlay = 0.00f;
    }
}
