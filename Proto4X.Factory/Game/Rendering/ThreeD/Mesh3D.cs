using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using Proto4X.Factory.Rendering.Models;
using System;

namespace Proto4X.Factory.Rendering.ThreeD
{
    /// <summary>Small 3D shapes in grid-local tiles (x east, y south, z up), lit like the models.</summary>
    public static class Mesh3D
    {
        /// <summary>An axis-aligned box, lit.</summary>
        public static void Box(GeometryBatch batch, GeometryLayer layer, Vector3 min, Vector3 max, Color colour, bool emissive = false)
        {
            ModelMesher.EmitBox(batch, layer, new ModelBox(min, max, colour, emissive), Matrix.Identity, Color.White);
        }

        /// <summary>A cube centred on <paramref name="centre"/>'s ground point, sitting on <paramref name="baseZ"/>.</summary>
        public static void Cube(GeometryBatch batch, GeometryLayer layer, Vector2 centre, float size, float baseZ, Color colour, bool emissive = false)
        {
            var half = size * 0.5f;
            Box(batch, layer, new Vector3(centre.X - half, centre.Y - half, baseZ), new Vector3(centre.X + half, centre.Y + half, baseZ + size), colour, emissive);
        }

        /// <summary>A square-section beam from <paramref name="from"/> to <paramref name="to"/>, lit per face.</summary>
        public static void Beam(GeometryBatch batch, GeometryLayer layer, Vector3 from, Vector3 to, float thickness, Color colour)
        {
            var axis = to - from;
            var length = axis.Length();
            if (length < 1e-4f)
            {
                return;
            }

            axis /= length;
            var side = Vector3.Cross(axis, Vector3.UnitZ);
            if (side.LengthSquared() < 1e-6f)
            {
                side = Vector3.UnitX;
            }

            side = Vector3.Normalize(side) * (thickness * 0.5f);
            var up = Vector3.Normalize(Vector3.Cross(side, axis)) * (thickness * 0.5f);
            if (up.Z < 0)
            {
                up = -up;
            }

            // Four long faces; the ends are too small to matter.
            Quad(batch, layer, from + up - side, to + up - side, to + up + side, from + up + side, Vector3.Normalize(up), colour);
            Quad(batch, layer, from - up - side, to - up - side, to - up + side, from - up + side, -Vector3.Normalize(up), colour);
            Quad(batch, layer, from - up + side, to - up + side, to + up + side, from + up + side, Vector3.Normalize(side), colour);
            Quad(batch, layer, from - up - side, to - up - side, to + up - side, from + up - side, -Vector3.Normalize(side), colour);
        }

        /// <summary>A flat quad lit as facing <paramref name="normal"/>.</summary>
        public static void Quad(GeometryBatch batch, GeometryLayer layer, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color colour)
        {
            batch.AddQuad(layer, a, b, c, d, ModelMesher.Lit(colour, ModelMesher.Light(normal)));
        }

        /// <summary>A flat ground-parallel rectangle at height <paramref name="z"/>, lit as a top face.</summary>
        public static void Flat(GeometryBatch batch, GeometryLayer layer, float x, float y, float width, float height, float z, Color colour, bool lit = true)
        {
            var c = lit ? ModelMesher.Lit(colour, ModelMesher.Light(Vector3.UnitZ)) : colour;
            batch.AddQuad(layer, new Vector3(x, y, z), new Vector3(x + width, y, z), new Vector3(x + width, y + height, z), new Vector3(x, y + height, z), c);
        }

        /// <summary>A flat line on a plane of height <paramref name="z"/>.</summary>
        public static void FlatLine(GeometryBatch batch, GeometryLayer layer, Vector2 from, Vector2 to, float thickness, float z, Color colour)
        {
            var delta = to - from;
            var length = delta.Length();
            if (length < 1e-5f)
            {
                return;
            }

            var normal = new Vector2(-delta.Y, delta.X) / length * (thickness * 0.5f);
            batch.AddQuad(layer, Shapes.At(from + normal, z), Shapes.At(to + normal, z), Shapes.At(to - normal, z), Shapes.At(from - normal, z), colour);
        }

        /// <summary>Cheap stable hash for per-tile decoration.</summary>
        public static uint Hash(int x, int y, int salt = 0)
        {
            unchecked
            {
                var h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(salt * 83492791);
                h ^= h >> 13;
                h *= 0x5bd1e995;
                h ^= h >> 15;
                return h;
            }
        }

        public static float Hash01(int x, int y, int salt = 0) => (Hash(x, y, salt) & 0xFFFF) / 65535f;
    }
}
