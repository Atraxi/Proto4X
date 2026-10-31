using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace MonoGameLibrary.Graphics
{
    /// <summary>
    /// The one vertex format of the geometry pass: position, normal, colour and texture coordinate,
    /// 36 bytes. MonoGame ships no built-in type with all four, and <see cref="BasicEffect"/> needs
    /// the normal for lighting and the colour for per-vertex tint at the same time.
    /// </summary>
    /// <remarks>
    /// The <see cref="VertexDeclaration"/> is what tells the GPU how to read this struct out of a
    /// vertex buffer: each element's byte offset, its type, and which shader input it feeds. The
    /// field order and the offsets below must agree - <c>Sequential</c>/<c>Pack = 1</c> pins the
    /// layout so they do.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MeshVertex(Vector3 position, Vector3 normal, Color color, Vector2 textureCoordinate) : IVertexType
    {
        public Vector3 Position = position;
        public Vector3 Normal = normal;
        public Color Color = color;
        public Vector2 TextureCoordinate = textureCoordinate;

        public static readonly VertexDeclaration VertexDeclaration = new(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(24, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));

        readonly VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }
}
