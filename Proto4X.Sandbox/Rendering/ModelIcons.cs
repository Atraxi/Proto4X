using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;
using System;

namespace Proto4X.Sandbox.Rendering
{
    /// <summary>A model seen from straight above as flat rectangles, for screen-space UI where the 3D pass can't draw.</summary>
    public static class ModelIcons
    {
        /// <summary>
        /// Draws <paramref name="modelId"/> nose-up, <paramref name="length"/> pixels nose to tail,
        /// taller boxes over lower ones. Emissive and untinted boxes keep their own colour.
        /// </summary>
        public static void Draw(SpriteBatch spriteBatch, Texture2D pixel, int modelId, Vector2 centre, float length, Color tint)
        {
            var model = ModelLibrary.Get(modelId);
            var scale = length / model.Length;
            var boxes = (ModelBox[])model.Boxes.Clone();
            Array.Sort(boxes, (a, b) => a.Max.Z.CompareTo(b.Max.Z));

            foreach (var box in boxes)
            {
                // Model +x is the nose, which points up the screen here, and +y (right) stays right.
                var topLeft = centre + new Vector2(box.Min.Y, -box.Max.X) * scale;
                var size = new Vector2(box.Max.Y - box.Min.Y, box.Max.X - box.Min.X) * scale;
                var colour = box.Tinted ? ModelMesher.Multiply(box.Colour, tint) : box.Colour;
                Primitives.FillRectangle(spriteBatch, pixel, topLeft, size, colour);
            }
        }
    }
}
