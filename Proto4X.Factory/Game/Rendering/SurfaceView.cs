using Microsoft.Xna.Framework;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Grid;
using System;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// Grid-local tiles to world pixels for one surface, through its frame.
    /// </summary>
    /// <remarks>
    /// Renderers draw in grid-local tile units. <see cref="WorldTransform"/> is handed to
    /// <c>SystemScheduler.Draw</c>, which applies it on the GPU along with the camera - the
    /// per-world draw transform ENGINE_REQUESTS.md asked for. Before it existed, every renderer
    /// pushed every quad through <see cref="TileToWorld"/> by hand; that's now only used for
    /// picking.
    /// </remarks>
    public static class SurfaceView
    {
        public const float TileSize = 32f;

        /// <summary>Grid-local tiles to world pixels: scale to pixels, then the frame's rotation and position.</summary>
        public static Matrix WorldTransform(in SurfaceFrame frame)
        {
            return Matrix.CreateScale(TileSize, TileSize, 1f) *
                Matrix.CreateRotationZ(frame.Rotation) *
                Matrix.CreateTranslation(frame.Position.X * TileSize, frame.Position.Y * TileSize, 0f);
        }

        public static Vector2 TileToWorld(in SurfaceFrame frame, Vector2 localTile) => frame.LocalToWorld(localTile) * TileSize;

        public static Vector2 WorldToTile(in SurfaceFrame frame, Vector2 worldPixels) => frame.WorldToLocal(worldPixels / TileSize);

        /// <summary>
        /// The block of grid tiles that can overlap the scheduler's local culling bounds, padded by
        /// <paramref name="padding"/> tiles and clamped to the grid. Styles that draw height above
        /// the ground pass a bigger padding.
        /// </summary>
        public static (int MinX, int MinY, int MaxX, int MaxY) VisibleTiles(Rectangle localBounds, SurfaceGrid grid, int padding = 1)
        {
            return (
                Math.Max(0, localBounds.Left - padding),
                Math.Max(0, localBounds.Top - padding),
                Math.Min(grid.Width - 1, localBounds.Right + padding),
                Math.Min(grid.Height - 1, localBounds.Bottom + padding));
        }

        public static bool IsVisible((int MinX, int MinY, int MaxX, int MaxY) visible, int x, int y, int size) =>
            x + size - 1 >= visible.MinX && x <= visible.MaxX && y + size - 1 >= visible.MinY && y <= visible.MaxY;

        public static Vector2 DirectionVector(Direction direction) => new(direction.Dx(), direction.Dy());
    }
}
