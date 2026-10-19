using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using System;

namespace Proto4X.Factory.Rendering.Models
{
    /// <summary>
    /// Turns model boxes into lit triangles. Lighting is baked into vertex colour on the CPU (one sun,
    /// flat per face, plus a darker foot on side faces as cheap ambient occlusion), so the GPU side
    /// needs no lighting setup and the software rasterizer sees exactly the same colours.
    /// </summary>
    public static class ModelMesher
    {
        /// <summary>Towards the sun: high, from the north-west (screen top-left), so shadows fall south-east.</summary>
        public static readonly Vector3 Sun = Vector3.Normalize(new Vector3(-0.45f, -0.6f, 1f));

        private const float Ambient = 0.52f;
        private const float Diffuse = 0.48f;
        private const float FootShade = 0.78f;

        public static void EmitModel(GeometryBatch batch, GeometryLayer layer, BlockModel model, in Matrix transform, Color tint)
        {
            foreach (var box in model.Boxes)
            {
                EmitBox(batch, layer, box, transform, tint);
            }
        }

        /// <summary>Five faces (no bottom) of one box through <paramref name="transform"/> (model to batch space; rotation, translation, uniform scale).</summary>
        public static void EmitBox(GeometryBatch batch, GeometryLayer layer, in ModelBox box, in Matrix transform, Color tint)
        {
            var colour = Multiply(box.Colour, tint);
            var min = box.Min;
            var max = box.Max;

            // Corners: bottom ring 0-3, top ring 4-7, both clockwise from (min.x, min.y) seen from above.
            Span<Vector3> c =
            [
                Vector3.Transform(new Vector3(min.X, min.Y, min.Z), transform),
                Vector3.Transform(new Vector3(max.X, min.Y, min.Z), transform),
                Vector3.Transform(new Vector3(max.X, max.Y, min.Z), transform),
                Vector3.Transform(new Vector3(min.X, max.Y, min.Z), transform),
                Vector3.Transform(new Vector3(min.X, min.Y, max.Z), transform),
                Vector3.Transform(new Vector3(max.X, min.Y, max.Z), transform),
                Vector3.Transform(new Vector3(max.X, max.Y, max.Z), transform),
                Vector3.Transform(new Vector3(min.X, max.Y, max.Z), transform),
            ];

            Face(batch, layer, c[4], c[5], c[6], c[7], Vector3.UnitZ, colour, box.Emissive, transform, isSide: false);
            Face(batch, layer, c[0], c[1], c[5], c[4], -Vector3.UnitY, colour, box.Emissive, transform, isSide: true);
            Face(batch, layer, c[1], c[2], c[6], c[5], Vector3.UnitX, colour, box.Emissive, transform, isSide: true);
            Face(batch, layer, c[2], c[3], c[7], c[6], Vector3.UnitY, colour, box.Emissive, transform, isSide: true);
            Face(batch, layer, c[3], c[0], c[4], c[7], -Vector3.UnitX, colour, box.Emissive, transform, isSide: true);
        }

        /// <summary>Sun lighting for a world-space normal.</summary>
        public static float Light(Vector3 worldNormal) => Ambient + Diffuse * MathF.Max(0f, Vector3.Dot(worldNormal, Sun));

        public static Color Lit(Color colour, float light) =>
            new((int)MathF.Min(255, colour.R * light), (int)MathF.Min(255, colour.G * light), (int)MathF.Min(255, colour.B * light), colour.A);

        public static Color Multiply(Color a, Color b) => new(a.R * b.R / 255, a.G * b.G / 255, a.B * b.B / 255, a.A * b.A / 255);

        /// <summary>
        /// One quad. For side faces a/b are the bottom edge and c/d the top, so the bottom pair gets
        /// the foot shade.
        /// </summary>
        private static void Face(GeometryBatch batch, GeometryLayer layer, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 modelNormal,
            Color colour, bool emissive, in Matrix transform, bool isSide)
        {
            var normal = Vector3.Normalize(Vector3.TransformNormal(modelNormal, transform));
            var lit = emissive ? colour : Lit(colour, Light(normal));
            var foot = isSide && !emissive ? Lit(lit, FootShade) : lit;
            var uv = batch.WhiteUv;
            batch.AddQuad(layer, a, b, c, d, foot, foot, lit, lit, uv, uv, uv, uv);
        }
    }
}
