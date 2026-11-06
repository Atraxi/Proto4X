using Microsoft.Xna.Framework;
using System;

namespace MonoGameLibrary.Graphics
{
    /// <summary>
    /// Turns model boxes into triangles with face normals. The sun is applied on the GPU
    /// (<see cref="SceneLighting"/>). The only baked shading is a darker foot on side faces, a cheap
    /// stand-in for ambient occlusion that doesn't depend on the sun.
    /// </summary>
    public static class ModelMesher
    {
        // Brightness of a side face's bottom edge; the GPU interpolates up to full at the top.
        private const float FootShade = 0.78f;

        public static void EmitModel(GeometryBuffer buffer, GeometryLayer layer, Model3D model, in Matrix transform, Color tint)
        {
            foreach (var box in model.Boxes)
            {
                EmitBox(buffer, layer, box, transform, tint);
            }
        }

        /// <summary>
        /// Five faces (no bottom) of one box through <paramref name="transform"/> (model to buffer
        /// space: rotation, translation, uniform scale). An emissive box aimed at the opaque layer goes
        /// to <see cref="GeometryLayer.Emissive"/> instead, so the sun doesn't darken it.
        /// </summary>
        public static void EmitBox(GeometryBuffer buffer, GeometryLayer layer, in ModelBox box, in Matrix transform, Color tint)
        {
            var colour = box.Tinted ? Multiply(box.Colour, tint) : box.Colour;
            var emissive = box.Emissive;
            if (emissive && layer == GeometryLayer.Opaque)
            {
                layer = GeometryLayer.Emissive;
            }

            // The 8 corners, transformed once and shared by the five faces.
            Span<Vector3> c = stackalloc Vector3[8];
            Corners(box, transform, c);
            var foot = emissive ? colour : Scale(colour, FootShade);

            // Corner order per face matches the winding of the others; the first colour is the bottom
            // edge, the second the top. The top face is uniform. Bottom faces are never seen.
            Face(buffer, layer, c[4], c[5], c[6], c[7], Vector3.UnitZ, transform, colour, colour);
            Face(buffer, layer, c[0], c[1], c[5], c[4], -Vector3.UnitY, transform, foot, colour);
            Face(buffer, layer, c[1], c[2], c[6], c[5], Vector3.UnitX, transform, foot, colour);
            Face(buffer, layer, c[2], c[3], c[7], c[6], Vector3.UnitY, transform, foot, colour);
            Face(buffer, layer, c[3], c[0], c[4], c[7], -Vector3.UnitX, transform, foot, colour);
        }

        /// <summary>
        /// A model's shadow caster: each box's five faces in one flat colour, every normal straight
        /// up. Positions stay where the model is. The flattening along the sun is a matrix applied at
        /// draw time (<see cref="SceneLighting.PlanarShadow"/>), so one mesh stays right as the
        /// surface turns.
        /// </summary>
        public static void EmitShadow(GeometryBuffer buffer, Model3D model, in Matrix transform, Color shade)
        {
            Span<Vector3> c = stackalloc Vector3[8];
            // Normal +z on every face on purpose: once flattened these lie on the ground, so they
            // should light like the ground, whatever direction the box face originally pointed.
            var up = Vector3.UnitZ;
            foreach (var box in model.Boxes)
            {
                Corners(box, transform, c);
                buffer.AddQuad(GeometryLayer.Opaque, c[4], c[5], c[6], c[7], up, shade);
                buffer.AddQuad(GeometryLayer.Opaque, c[0], c[1], c[5], c[4], up, shade);
                buffer.AddQuad(GeometryLayer.Opaque, c[1], c[2], c[6], c[5], up, shade);
                buffer.AddQuad(GeometryLayer.Opaque, c[2], c[3], c[7], c[6], up, shade);
                buffer.AddQuad(GeometryLayer.Opaque, c[3], c[0], c[4], c[7], up, shade);
            }
        }

        /// <summary>Scales a colour's brightness, keeping alpha.</summary>
        public static Color Scale(Color colour, float factor) =>
            new((int)MathF.Min(255, colour.R * factor), (int)MathF.Min(255, colour.G * factor), (int)MathF.Min(255, colour.B * factor), colour.A);

        public static Color Multiply(Color a, Color b) => new(a.R * b.R / 255, a.G * b.G / 255, a.B * b.B / 255, a.A * b.A / 255);

        /// <summary>Corners of a box through <paramref name="transform"/>: bottom ring 0-3, top ring 4-7, both clockwise from (min.x, min.y) seen from above.</summary>
        private static void Corners(in ModelBox box, in Matrix transform, Span<Vector3> c)
        {
            var min = box.Min;
            var max = box.Max;
            c[0] = Vector3.Transform(new Vector3(min.X, min.Y, min.Z), transform);
            c[1] = Vector3.Transform(new Vector3(max.X, min.Y, min.Z), transform);
            c[2] = Vector3.Transform(new Vector3(max.X, max.Y, min.Z), transform);
            c[3] = Vector3.Transform(new Vector3(min.X, max.Y, min.Z), transform);
            c[4] = Vector3.Transform(new Vector3(min.X, min.Y, max.Z), transform);
            c[5] = Vector3.Transform(new Vector3(max.X, min.Y, max.Z), transform);
            c[6] = Vector3.Transform(new Vector3(max.X, max.Y, max.Z), transform);
            c[7] = Vector3.Transform(new Vector3(min.X, max.Y, max.Z), transform);
        }

        /// <summary>One quad. For side faces a/b are the bottom edge and c/d the top, so the bottom pair gets <paramref name="bottom"/>.</summary>
        private static void Face(GeometryBuffer buffer, GeometryLayer layer, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 modelNormal,
            in Matrix transform, Color bottom, Color top)
        {
            // Normals rotate with the model (TransformNormal ignores translation). Fine for rotation
            // and uniform scale; a non-uniform scale would need the inverse-transpose.
            var normal = Vector3.Normalize(Vector3.TransformNormal(modelNormal, transform));
            var uv = buffer.WhiteUv;
            buffer.AddQuad(layer, a, b, c, d, normal, bottom, bottom, top, top, uv, uv, uv, uv);
        }
    }
}
