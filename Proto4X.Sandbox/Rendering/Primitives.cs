using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Proto4X.Sandbox.Rendering
{
    /// <summary>
    /// Lines, circles and filled bars built out of a stretched 1x1 texture - the engine draws
    /// sprites and nothing else, so every overlay in the game is made of rectangles.
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

            var angle = MathF.Atan2(delta.Y, delta.X);
            spriteBatch.Draw(
                pixel,
                from,
                null,
                colour,
                angle,
                new Vector2(0f, 0.5f),
                new Vector2(length, thickness),
                SpriteEffects.None,
                0f);
        }

        public static void DrawRectangleOutline(SpriteBatch spriteBatch, Texture2D pixel, ReadOnlySpan<Vector2> corners, Color colour, float thickness = 1f)
        {
            for (var i = 0; i < corners.Length; i++)
            {
                DrawLine(spriteBatch, pixel, corners[i], corners[(i + 1) % corners.Length], colour, thickness);
            }
        }

        public static void DrawCircleOutline(SpriteBatch spriteBatch, Texture2D pixel, Vector2 centre, float radius, Color colour, float thickness = 1f, int segments = 20)
        {
            var step = MathHelper.TwoPi / segments;
            var previous = centre + new Vector2(radius, 0f);

            for (var i = 1; i <= segments; i++)
            {
                var angle = step * i;
                var next = centre + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
                DrawLine(spriteBatch, pixel, previous, next, colour, thickness);
                previous = next;
            }
        }

        public static void FillRectangle(SpriteBatch spriteBatch, Texture2D pixel, Vector2 topLeft, Vector2 size, Color colour)
        {
            spriteBatch.Draw(pixel, topLeft, null, colour, 0f, Vector2.Zero, size, SpriteEffects.None, 0f);
        }
    }
}
