using Microsoft.Xna.Framework;
using MonoGameLibrary.Graphics;
using Proto4X.Factory.Rendering.Models;

namespace Proto4X.Factory.Rendering.ThreeD
{
    /// <summary>Small 3D shapes in grid-local tiles (x east, y south, z up), with normals so the GPU can light them.</summary>
    public static class Mesh3D
    {
        /// <summary>An axis-aligned box.</summary>
        public static void Box(GeometryBuffer buffer, GeometryLayer layer, Vector3 min, Vector3 max, Color colour, bool emissive = false)
        {
            ModelMesher.EmitBox(buffer, layer, new ModelBox(min, max, colour, emissive), Matrix.Identity, Color.White);
        }

        /// <summary>A cube centred on <paramref name="centre"/>'s ground point, sitting on <paramref name="baseZ"/>.</summary>
        public static void Cube(GeometryBuffer buffer, GeometryLayer layer, Vector2 centre, float size, float baseZ, Color colour, bool emissive = false)
        {
            var half = size * 0.5f;
            Box(buffer, layer, new Vector3(centre.X - half, centre.Y - half, baseZ), new Vector3(centre.X + half, centre.Y + half, baseZ + size), colour, emissive);
        }

        /// <summary>A square-section beam from <paramref name="from"/> to <paramref name="to"/>.</summary>
        public static void Beam(GeometryBuffer buffer, GeometryLayer layer, Vector3 from, Vector3 to, float thickness, Color colour)
        {
            var axis = to - from;
            var length = axis.Length();
            if (length < 1e-4f)
            {
                return;
            }

            axis /= length;

            // Build two perpendicular half-width offsets, `side` and `up`, around the beam's axis.
            // Cross with world up gives a horizontal side; a vertical beam has no such direction, so
            // fall back to x.
            var side = Vector3.Cross(axis, Vector3.UnitZ);
            if (side.LengthSquared() < 1e-6f)
            {
                side = Vector3.UnitX;
            }

            side = Vector3.Normalize(side) * (thickness * 0.5f);
            var up = Vector3.Normalize(Vector3.Cross(side, axis)) * (thickness * 0.5f);
            // Cross products can come out either way; make `up` point up so the "top" face is the top.
            if (up.Z < 0)
            {
                up = -up;
            }

            // Four long faces; the ends are too small to matter.
            var upNormal = Vector3.Normalize(up);
            var sideNormal = Vector3.Normalize(side);
            buffer.AddQuad(layer, from + up - side, to + up - side, to + up + side, from + up + side, upNormal, colour);
            buffer.AddQuad(layer, from - up - side, to - up - side, to - up + side, from - up + side, -upNormal, colour);
            buffer.AddQuad(layer, from - up + side, to - up + side, to + up + side, from + up + side, sideNormal, colour);
            buffer.AddQuad(layer, from - up - side, to - up - side, to + up - side, from + up - side, -sideNormal, colour);
        }

        /// <summary>A flat ground-parallel rectangle at height <paramref name="z"/>, facing up.</summary>
        public static void Flat(GeometryBuffer buffer, GeometryLayer layer, float x, float y, float width, float height, float z, Color colour)
        {
            buffer.AddQuad(layer, new Vector3(x, y, z), new Vector3(x + width, y, z), new Vector3(x + width, y + height, z), new Vector3(x, y + height, z), Vector3.UnitZ, colour);
        }

        /// <summary>A flat line on a plane of height <paramref name="z"/>, facing up.</summary>
        public static void FlatLine(GeometryBuffer buffer, GeometryLayer layer, Vector2 from, Vector2 to, float thickness, float z, Color colour)
        {
            var delta = to - from;
            var length = delta.Length();
            if (length < 1e-5f)
            {
                return;
            }

            // Half-thickness step perpendicular to the line (rotate delta 90 degrees), for both long edges.
            var normal = new Vector2(-delta.Y, delta.X) / length * (thickness * 0.5f);
            buffer.AddQuad(layer, At(from + normal, z), At(to + normal, z), At(to - normal, z), At(from - normal, z), Vector3.UnitZ, colour);
        }

        private static Vector3 At(Vector2 point, float z) => new(point.X, point.Y, z);

        /// <summary>Cheap, stable hash of a tile (and a salt) for decoration that must not flicker between rebuilds.</summary>
        public static uint Hash(int x, int y, int salt = 0)
        {
            unchecked
            {
                // Multiply by large primes, XOR the inputs together, then mix the bits.
                var h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(salt * 83492791);
                h ^= h >> 13;
                h *= 0x5bd1e995;
                h ^= h >> 15;
                return h;
            }
        }

        /// <summary>The low 16 bits of <see cref="Hash"/> as a float in [0, 1].</summary>
        public static float Hash01(int x, int y, int salt = 0) => (Hash(x, y, salt) & 0xFFFF) / 65535f;
    }
}
