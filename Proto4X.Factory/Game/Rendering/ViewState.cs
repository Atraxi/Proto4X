using Microsoft.Xna.Framework;
using MonoGameLibrary.UI;
using System;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// The current frame's mapping from a surface's grid-local tiles to screen pixels, kept by the
    /// style and read by renderers that have to draw something screen-aligned in local space.
    /// </summary>
    public sealed class ViewState
    {
        /// <summary>Grid-local tiles to screen pixels (2D cameras): world transform times camera transform.</summary>
        public Matrix LocalToScreen { get; private set; } = Matrix.Identity;

        public float Zoom { get; private set; } = 1f;

        /// <summary>How far the grid's +x axis is turned on screen, in radians, clockwise.</summary>
        public float GridAngle { get; private set; }

        public void Update(Camera camera, Matrix worldTransform)
        {
            LocalToScreen = worldTransform * camera.GetCameraTransform();
            Zoom = camera.Zoom;
            GridAngle = MathF.Atan2(LocalToScreen.M12, LocalToScreen.M11);
        }

        /// <summary>
        /// Local-space vectors that land one sprite pixel right and one down on screen, given how
        /// many screen pixels one sprite pixel covers at zoom 1.
        /// </summary>
        public (Vector2 Right, Vector2 Down) PixelBasis(float screenPixelsPerSpritePixelAtZoom1)
        {
            var m = LocalToScreen;
            var determinant = m.M11 * m.M22 - m.M12 * m.M21;
            var scale = screenPixelsPerSpritePixelAtZoom1 * Zoom / determinant;
            return (new Vector2(m.M22, -m.M12) * scale, new Vector2(-m.M21, m.M11) * scale);
        }

        /// <summary>Quarter turns (0-3) the grid is turned on screen, rounded to the nearest, for picking a sprite facing.</summary>
        public int QuarterTurns => ((int)MathF.Round(GridAngle / MathHelper.PiOver2) % 4 + 4) % 4;
    }
}
