using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using System;

namespace Proto4X.Sandbox.Rendering
{
    /// <summary>
    /// Lines, arcs, bars and icons lying flat on the ground plane, built into the geometry pass.
    /// The 3D counterpart of <see cref="Primitives"/>, which stays for screen-space UI.
    /// </summary>
    /// <remarks>
    /// Everything is drawn on the translucent layer, so colours must be premultiplied: pass them
    /// through <see cref="Premultiply"/>. Overlays sit <see cref="OverlayHeight"/> above the ship
    /// models (z = 0) so coplanar depth ties never flicker.
    /// </remarks>
    public static class GroundShapes
    {
        public const float OverlayHeight = 1f;

        public static Color Premultiply(Color colour) => colour * (colour.A / 255f);

        public static void Line(GeometryBuffer buffer, Vector2 from, Vector2 to, Color colour, float thickness = 1f, float height = OverlayHeight)
        {
            var delta = to - from;
            var length = delta.Length();
            if (length < float.Epsilon)
            {
                return;
            }

            var side = new Vector2(-delta.Y, delta.X) / length * (thickness / 2f);
            Quad(buffer, from + side, to + side, to - side, from - side, Premultiply(colour), height);
        }

        public static void RectangleOutline(GeometryBuffer buffer, ReadOnlySpan<Vector2> corners, Color colour, float thickness = 1f, float height = OverlayHeight)
        {
            for (var i = 0; i < corners.Length; i++)
            {
                Line(buffer, corners[i], corners[(i + 1) % corners.Length], colour, thickness, height);
            }
        }

        public static void CircleOutline(GeometryBuffer buffer, Vector2 centre, float radius, Color colour, float thickness = 1f, int segments = 20, float height = OverlayHeight)
        {
            Arc(buffer, centre, radius, 0f, MathHelper.TwoPi, colour, thickness, segments, height);
        }

        /// <summary>A partial circle swept clockwise (as seen from above) from <paramref name="startAngle"/>.</summary>
        public static void Arc(GeometryBuffer buffer, Vector2 centre, float radius, float startAngle, float sweepAngle, Color colour, float thickness = 1f, int segments = 24, float height = OverlayHeight)
        {
            if (MathF.Abs(sweepAngle) < 0.0001f)
            {
                return;
            }

            var step = sweepAngle / segments;
            var previous = centre + new Vector2(MathF.Cos(startAngle), MathF.Sin(startAngle)) * radius;
            for (var i = 1; i <= segments; i++)
            {
                var angle = startAngle + step * i;
                var next = centre + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
                Line(buffer, previous, next, colour, thickness, height);
                previous = next;
            }
        }

        public static void FillRectangle(GeometryBuffer buffer, Vector2 topLeft, Vector2 size, Color colour, float height = OverlayHeight)
        {
            Quad(buffer, topLeft, topLeft + new Vector2(size.X, 0f), topLeft + size, topLeft + new Vector2(0f, size.Y), Premultiply(colour), height);
        }

        /// <summary>
        /// A model from <see cref="ModelLibrary"/> scaled to <paramref name="length"/> nose to tail,
        /// nose pointing north, on the translucent layer so <paramref name="tint"/>'s alpha fades it.
        /// </summary>
        public static void Model(GeometryBuffer buffer, int modelId, Vector2 centre, float length, Color tint, float height = OverlayHeight)
        {
            var model = ModelLibrary.Get(modelId);
            var transform = Matrix.CreateScale(length / model.Length) *
                Matrix.CreateRotationZ(-MathHelper.PiOver2) *
                Matrix.CreateTranslation(centre.X, centre.Y, height);
            ModelMesher.EmitModel(buffer, GeometryLayer.Translucent, model, transform, Premultiply(tint));
        }

        private static void Quad(GeometryBuffer buffer, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color premultiplied, float height)
        {
            buffer.AddQuad(
                GeometryLayer.Translucent,
                new Vector3(a, height), new Vector3(b, height), new Vector3(c, height), new Vector3(d, height),
                Vector3.UnitZ,
                premultiplied);
        }
    }
}
