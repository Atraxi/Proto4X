using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// Lines and rectangles from a stretched 1x1 texture. A copy of the sandbox's Primitives (the
    /// two projects deliberately share nothing), plus rotated rectangles for ship surfaces - this
    /// is the second game to need it, which is the promotion signal ENGINE_LAYERING.md asks for.
    /// </summary>
    public static class Primitives
    {
        public static void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 from, Vector2 to, Color colour, float thickness = 1f)
        {
            var delta = to - from;
            var length = delta.Length();
            if (length < float.Epsilon)
            {
                return;
            }

            spriteBatch.Draw(pixel, from, null, colour, MathF.Atan2(delta.Y, delta.X), new Vector2(0f, 0.5f),
                new Vector2(length, thickness), SpriteEffects.None, 0f);
        }

        /// <summary>A rectangle whose top-left corner is <paramref name="corner"/>, rotated about that corner.</summary>
        public static void FillRectangle(SpriteBatch spriteBatch, Texture2D pixel, Vector2 corner, Vector2 size, float rotation, Color colour)
        {
            spriteBatch.Draw(pixel, corner, null, colour, rotation, Vector2.Zero, size, SpriteEffects.None, 0f);
        }

        /// <summary>A rectangle centred on <paramref name="centre"/>, rotated about its centre.</summary>
        public static void FillCentred(SpriteBatch spriteBatch, Texture2D pixel, Vector2 centre, Vector2 size, float rotation, Color colour)
        {
            spriteBatch.Draw(pixel, centre, null, colour, rotation, new Vector2(0.5f, 0.5f), size, SpriteEffects.None, 0f);
        }

        public static void DrawOutline(SpriteBatch spriteBatch, Texture2D pixel, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color colour, float thickness = 1f)
        {
            DrawLine(spriteBatch, pixel, a, b, colour, thickness);
            DrawLine(spriteBatch, pixel, b, c, colour, thickness);
            DrawLine(spriteBatch, pixel, c, d, colour, thickness);
            DrawLine(spriteBatch, pixel, d, a, colour, thickness);
        }
    }
}
