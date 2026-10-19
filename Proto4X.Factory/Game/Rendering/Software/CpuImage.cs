using Microsoft.Xna.Framework;
using System;

namespace Proto4X.Factory.Rendering.Software
{
    /// <summary>
    /// A premultiplied-alpha RGBA image in memory: the CPU twin of a texture. Sprites are generated
    /// (or baked) into these, packed into an atlas, and the same pixels go to the GPU and to the
    /// software rasterizer.
    /// </summary>
    public sealed class CpuImage(int width, int height)
    {
        public int Width { get; } = width;
        public int Height { get; } = height;
        public Color[] Pixels { get; } = new Color[width * height];

        public ref Color At(int x, int y) => ref Pixels[y * Width + x];

        public void Fill(Color colour) => Array.Fill(Pixels, colour);

        /// <summary>Premultiplied "over" for one pixel.</summary>
        public void Blend(int x, int y, Color colour)
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
            {
                return;
            }

            ref var dst = ref At(x, y);
            var inverse = 255 - colour.A;
            dst = new Color(
                colour.R + dst.R * inverse / 255,
                colour.G + dst.G * inverse / 255,
                colour.B + dst.B * inverse / 255,
                colour.A + dst.A * inverse / 255);
        }

        public Vector4 Sample(Vector2 uv, bool linear)
        {
            if (!linear)
            {
                var x = Math.Clamp((int)(uv.X * Width), 0, Width - 1);
                var y = Math.Clamp((int)(uv.Y * Height), 0, Height - 1);
                return Pixels[y * Width + x].ToVector4();
            }

            var fx = uv.X * Width - 0.5f;
            var fy = uv.Y * Height - 0.5f;
            var x0 = (int)MathF.Floor(fx);
            var y0 = (int)MathF.Floor(fy);
            var tx = fx - x0;
            var ty = fy - y0;
            var a = Texel(x0, y0);
            var b = Texel(x0 + 1, y0);
            var c = Texel(x0, y0 + 1);
            var d = Texel(x0 + 1, y0 + 1);
            return Vector4.Lerp(Vector4.Lerp(a, b, tx), Vector4.Lerp(c, d, tx), ty);
        }

        private Vector4 Texel(int x, int y) => Pixels[Math.Clamp(y, 0, Height - 1) * Width + Math.Clamp(x, 0, Width - 1)].ToVector4();
    }

    /// <summary>Where an image landed in an atlas, as texture coordinates.</summary>
    public readonly record struct AtlasRegion(Vector2 Min, Vector2 Max, int PixelWidth, int PixelHeight)
    {
        public Vector2 TopLeft => Min;
        public Vector2 TopRight => new(Max.X, Min.Y);
        public Vector2 BottomRight => Max;
        public Vector2 BottomLeft => new(Min.X, Max.Y);
    }

    /// <summary>
    /// Shelf-packs images into one square atlas. Every image gets a one-pixel border copied from its
    /// own edge, so linear filtering never bleeds a neighbour in. The top-left 4x4 is white, for
    /// untextured geometry.
    /// </summary>
    public sealed class AtlasBuilder
    {
        private const int Border = 1;
        private int shelfX;
        private int shelfY;
        private int shelfHeight;

        public AtlasBuilder(int size = 2048)
        {
            Image = new CpuImage(size, size);
            for (var y = 0; y < 4; y++)
            {
                for (var x = 0; x < 4; x++)
                {
                    Image.At(x, y) = Color.White;
                }
            }

            WhiteUv = new Vector2(2f / size, 2f / size);
            shelfX = 4 + Border;
            shelfY = 0;
            shelfHeight = 4;
        }

        public CpuImage Image { get; }
        public Vector2 WhiteUv { get; }

        /// <summary>How far down the atlas the packed shelves reach, 0-1: a rough measure of how much art a style needs.</summary>
        public float FilledFraction => (shelfY + shelfHeight) / (float)Image.Height;

        public AtlasRegion Add(CpuImage source)
        {
            var w = source.Width + 2 * Border;
            var h = source.Height + 2 * Border;
            if (shelfX + w > Image.Width)
            {
                shelfX = 0;
                shelfY += shelfHeight;
                shelfHeight = 0;
            }

            if (shelfY + h > Image.Height)
            {
                throw new InvalidOperationException("Atlas is full.");
            }

            for (var y = -Border; y < source.Height + Border; y++)
            {
                for (var x = -Border; x < source.Width + Border; x++)
                {
                    var sx = Math.Clamp(x, 0, source.Width - 1);
                    var sy = Math.Clamp(y, 0, source.Height - 1);
                    ref var texel = ref source.At(sx, sy);
                    Image.At(shelfX + Border + x, shelfY + Border + y) = texel;
                }
            }

            var left = shelfX + Border;
            var top = shelfY + Border;
            shelfX += w;
            shelfHeight = Math.Max(shelfHeight, h);
            return new AtlasRegion(
                new Vector2(left / (float)Image.Width, top / (float)Image.Height),
                new Vector2((left + source.Width) / (float)Image.Width, (top + source.Height) / (float)Image.Height),
                source.Width, source.Height);
        }
    }
}
