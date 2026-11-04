using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace MonoGameLibrary.Graphics
{
    /// <summary>
    /// The one vertex format of the geometry pass: position, normal, colour, texture coordinate
    /// (36 bytes). No stock MonoGame vertex type has both a normal and a colour, and
    /// <see cref="BasicEffect"/> lighting needs both.
    /// </summary>
    // Pack = 1 and Sequential pin the C# layout, so the byte offsets in VertexDeclaration stay true.
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MeshVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate) : IVertexType
    {
        // Field order here must match the offsets in VertexDeclaration below.
        public Vector3 Position = position;            // bytes 0-11
        public Vector3 Normal = normal;                // bytes 12-23, unit length; used by lit layers only
        public Color Color = color;                    // bytes 24-27, RGBA, one byte each
        public Vector2 TextureCoordinate = textureCoordinate; // bytes 28-35

        /// <summary>Tells the GPU where each field sits in the struct and which shader input it feeds.</summary>
        public static readonly VertexDeclaration VertexDeclaration = new(
            // (byte offset, data format, usage = which shader input, usage index)
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(24, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));

        // IVertexType lets MonoGame find the declaration when handed an array of these.
        readonly VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }
}
