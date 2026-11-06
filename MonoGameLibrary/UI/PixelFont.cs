using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace MonoGameLibrary.UI
{
    /// <summary>
    /// A built-in 5×7 pixel font, rasterised at load into a texture and wrapped in a real
    /// <see cref="SpriteFont"/>, so <c>SpriteBatch.DrawString</c> and <c>MeasureString</c> work
    /// unchanged. No content pipeline, no font file, no licence.
    /// </summary>
    /// <remarks>
    /// Uppercase only: lowercase letters draw as their capitals (the classic pixel-font look), which
    /// halves the glyph table. Baked at an integer pixel scale rather than scaled at draw time, so
    /// SpriteBatch's default linear sampler can't blur it as long as text sits on whole pixels.
    /// Upgrade path when real typography matters: swap <see cref="Create"/> for a TTF rasteriser
    /// (FontStashSharp, or a content-pipeline SpriteFont); callers only ever see a SpriteFont.
    /// </remarks>
    public static class PixelFont
    {
        public const int GlyphWidth = 5;
        public const int GlyphHeight = 7;

        /// <summary>Space between glyphs and lines, in font pixels.</summary>
        public const int Spacing = 1;

        /// <summary>Rows top to bottom, '#' lit. Lowercase is folded onto these at creation.</summary>
        private static readonly (char Character, string Rows)[] glyphs =
        [
            (' ', "..... ..... ..... ..... ..... ..... ....."),
            ('!', "..#.. ..#.. ..#.. ..#.. ..#.. ..... ..#.."),
            ('"', ".#.#. .#.#. ..... ..... ..... ..... ....."),
            ('#', ".#.#. .#.#. ##### .#.#. ##### .#.#. .#.#."),
            ('$', "..#.. .#### #.#.. .###. ..#.# ####. ..#.."),
            ('%', "##... ##..# ...#. ..#.. .#... #..## ...##"),
            ('&', ".##.. #..#. #.#.. .#... #.#.# #..#. .##.#"),
            ('\'', "..#.. ..#.. ..... ..... ..... ..... ....."),
            ('(', "...#. ..#.. .#... .#... .#... ..#.. ...#."),
            (')', ".#... ..#.. ...#. ...#. ...#. ..#.. .#..."),
            ('*', "..... ..#.. #.#.# .###. #.#.# ..#.. ....."),
            ('+', "..... ..#.. ..#.. ##### ..#.. ..#.. ....."),
            (',', "..... ..... ..... ..... ..##. ..#.. .#..."),
            ('-', "..... ..... ..... ##### ..... ..... ....."),
            ('.', "..... ..... ..... ..... ..... .##.. .##.."),
            ('/', "..... ....# ...#. ..#.. .#... #.... ....."),
            ('0', ".###. #...# #..## #.#.# ##..# #...# .###."),
            ('1', "..#.. .##.. ..#.. ..#.. ..#.. ..#.. .###."),
            ('2', ".###. #...# ....# ...#. ..#.. .#... #####"),
            ('3', "##### ...#. ..#.. ...#. ....# #...# .###."),
            ('4', "...#. ..##. .#.#. #..#. ##### ...#. ...#."),
            ('5', "##### #.... ####. ....# ....# #...# .###."),
            ('6', "..##. .#... #.... ####. #...# #...# .###."),
            ('7', "##### ....# ...#. ..#.. .#... .#... .#..."),
            ('8', ".###. #...# #...# .###. #...# #...# .###."),
            ('9', ".###. #...# #...# .#### ....# ...#. .##.."),
            (':', "..... .##.. .##.. ..... .##.. .##.. ....."),
            (';', "..... .##.. .##.. ..... .##.. ..#.. .#..."),
            ('<', "...#. ..#.. .#... #.... .#... ..#.. ...#."),
            ('=', "..... ..... ##### ..... ##### ..... ....."),
            ('>', ".#... ..#.. ...#. ....# ...#. ..#.. .#..."),
            ('?', ".###. #...# ....# ...#. ..#.. ..... ..#.."),
            ('@', ".###. #...# ....# .##.# #.#.# #.#.# .###."),
            ('A', ".###. #...# #...# ##### #...# #...# #...#"),
            ('B', "####. #...# #...# ####. #...# #...# ####."),
            ('C', ".###. #...# #.... #.... #.... #...# .###."),
            ('D', "###.. #..#. #...# #...# #...# #..#. ###.."),
            ('E', "##### #.... #.... ####. #.... #.... #####"),
            ('F', "##### #.... #.... ####. #.... #.... #...."),
            ('G', ".###. #...# #.... #.### #...# #...# .####"),
            ('H', "#...# #...# #...# ##### #...# #...# #...#"),
            ('I', ".###. ..#.. ..#.. ..#.. ..#.. ..#.. .###."),
            ('J', "..### ...#. ...#. ...#. ...#. #..#. .##.."),
            ('K', "#...# #..#. #.#.. ##... #.#.. #..#. #...#"),
            ('L', "#.... #.... #.... #.... #.... #.... #####"),
            ('M', "#...# ##.## #.#.# #.#.# #...# #...# #...#"),
            ('N', "#...# #...# ##..# #.#.# #..## #...# #...#"),
            ('O', ".###. #...# #...# #...# #...# #...# .###."),
            ('P', "####. #...# #...# ####. #.... #.... #...."),
            ('Q', ".###. #...# #...# #...# #.#.# #..#. .##.#"),
            ('R', "####. #...# #...# ####. #.#.. #..#. #...#"),
            ('S', ".#### #.... #.... .###. ....# ....# ####."),
            ('T', "##### ..#.. ..#.. ..#.. ..#.. ..#.. ..#.."),
            ('U', "#...# #...# #...# #...# #...# #...# .###."),
            ('V', "#...# #...# #...# #...# #...# .#.#. ..#.."),
            ('W', "#...# #...# #...# #.#.# #.#.# #.#.# .#.#."),
            ('X', "#...# #...# .#.#. ..#.. .#.#. #...# #...#"),
            ('Y', "#...# #...# .#.#. ..#.. ..#.. ..#.. ..#.."),
            ('Z', "##### ....# ...#. ..#.. .#... #.... #####"),
            ('[', ".###. .#... .#... .#... .#... .#... .###."),
            ('\\', "..... #.... .#... ..#.. ...#. ....# ....."),
            (']', ".###. ...#. ...#. ...#. ...#. ...#. .###."),
            ('^', "..#.. .#.#. #...# ..... ..... ..... ....."),
            ('_', "..... ..... ..... ..... ..... ..... #####"),
            ('|', "..#.. ..#.. ..#.. ..#.. ..#.. ..#.. ..#.."),
        ];

        /// <summary>Every character the font can draw, lowercase included.</summary>
        public static IEnumerable<char> Characters
        {
            get
            {
                foreach (var (character, _) in glyphs)
                {
                    yield return character;
                    if (character is >= 'A' and <= 'Z')
                    {
                        yield return char.ToLowerInvariant(character);
                    }
                }
            }
        }

        /// <summary>
        /// The atlas as RGBA pixels, one row of cells, each <c>(5+1)·scale</c> wide: device-free, so it
        /// can be checked headless. Returns the pixel data plus the atlas size.
        /// </summary>
        public static (Color[] Pixels, int Width, int Height) RasterizeAtlas(int scale)
        {
            ValidateScale(scale);
            var cellWidth = (GlyphWidth + Spacing) * scale;
            var width = glyphs.Length * cellWidth;
            var height = GlyphHeight * scale;
            var pixels = new Color[width * height];

            for (var g = 0; g < glyphs.Length; g++)
            {
                var rows = glyphs[g].Rows.Split(' ');
                if (rows.Length != GlyphHeight)
                {
                    throw new InvalidOperationException($"Glyph '{glyphs[g].Character}' has {rows.Length} rows, expected {GlyphHeight}.");
                }

                for (var row = 0; row < GlyphHeight; row++)
                {
                    if (rows[row].Length != GlyphWidth)
                    {
                        throw new InvalidOperationException($"Glyph '{glyphs[g].Character}' row {row} is {rows[row].Length} wide, expected {GlyphWidth}.");
                    }

                    for (var column = 0; column < GlyphWidth; column++)
                    {
                        if (rows[row][column] != '#')
                        {
                            continue;
                        }

                        for (var sy = 0; sy < scale; sy++)
                        {
                            for (var sx = 0; sx < scale; sx++)
                            {
                                var x = g * cellWidth + column * scale + sx;
                                var y = row * scale + sy;
                                pixels[y * width + x] = Color.White;
                            }
                        }
                    }
                }
            }

            return (pixels, width, height);
        }

        /// <summary>Builds the font at <paramref name="scale"/> screen pixels per font pixel. Draw it at scale 1.</summary>
        public static SpriteFont Create(GraphicsDevice device, int scale = 2)
        {
            var (pixels, width, height) = RasterizeAtlas(scale);
            var texture = new Texture2D(device, width, height);
            texture.SetData(pixels);

            var cellWidth = (GlyphWidth + Spacing) * scale;
            var glyphBounds = new List<Rectangle>();
            var cropping = new List<Rectangle>();
            var characters = new List<char>();
            var kerning = new List<Vector3>();

            // SpriteFont needs its characters in ascending order: it binary-searches them.
            var ordered = new SortedDictionary<char, int>();
            for (var g = 0; g < glyphs.Length; g++)
            {
                ordered[glyphs[g].Character] = g;
                var character = glyphs[g].Character;
                if (character is >= 'A' and <= 'Z')
                {
                    ordered[char.ToLowerInvariant(character)] = g;
                }
            }

            foreach (var (character, g) in ordered)
            {
                characters.Add(character);
                glyphBounds.Add(new Rectangle(g * cellWidth, 0, GlyphWidth * scale, GlyphHeight * scale));
                cropping.Add(new Rectangle(0, 0, GlyphWidth * scale, GlyphHeight * scale));
                // (left bearing, glyph width, right bearing): the advance is their sum.
                kerning.Add(new Vector3(0, GlyphWidth * scale, Spacing * scale));
            }

            return new SpriteFont(texture, glyphBounds, cropping, characters, (GlyphHeight + Spacing * 2) * scale, 0f, kerning, '?');
        }

        /// <summary>Width of <paramref name="text"/> in screen pixels at <paramref name="scale"/>, without a device.</summary>
        public static int MeasureWidth(string text, int scale)
        {
            ValidateScale(scale);
            return text.Length * (GlyphWidth + Spacing) * scale;
        }

        private static void ValidateScale(int scale)
        {
            if (scale < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(scale), scale, "Scale must be at least 1.");
            }
        }
    }
}
