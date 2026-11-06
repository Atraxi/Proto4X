using MonoGameLibrary.UI;
using System.Linq;

namespace Proto4X.Factory.Headless
{
    /// <summary>Checks for the prototype loop (plans/03) and the engine pieces it needs (plans/01).</summary>
    public static partial class Scenarios
    {
        private static void RunLoopScenarios()
        {
            Run("pixel font covers the UI's character set", PixelFontCoverage);
        }

        private static void PixelFontCoverage()
        {
            const int scale = 2;
            var (pixels, width, height) = PixelFont.RasterizeAtlas(scale);
            var characters = PixelFont.Characters.ToHashSet();
            var cellWidth = (PixelFont.GlyphWidth + PixelFont.Spacing) * scale;
            var glyphCount = width / cellWidth;

            var emptyGlyphs = 0;
            for (var g = 0; g < glyphCount; g++)
            {
                var lit = false;
                for (var y = 0; y < height && !lit; y++)
                {
                    for (var x = g * cellWidth; x < (g + 1) * cellWidth && !lit; x++)
                    {
                        lit = pixels[y * width + x].A > 0;
                    }
                }

                emptyGlyphs += lit ? 0 : 1;
            }

            Check(emptyGlyphs == 1, $"exactly one blank glyph, the space (got {emptyGlyphs} of {glyphCount})");
            var required = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 .,:;!?%/+-=()[]<>#'\"_|";
            var missing = new string([.. required.Where(c => !characters.Contains(c))]);
            Check(missing.Length == 0, $"every UI character has a glyph{(missing.Length > 0 ? $" (missing '{missing}')" : "")}");
            Check(PixelFont.MeasureWidth("ABC", scale) == 3 * cellWidth, "width is the sum of advances");
        }
    }
}
