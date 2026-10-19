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
    /// <b>This is game code only because the engine can't do it.</b> <c>SystemScheduler.Draw</c> opens
    /// its world pass with the camera transform and nothing else, so there is no way to give one
    /// GameWorld its own transform - which is exactly what a ship, a surface whose grid moves, needs.
    /// Every renderer therefore pushes every quad through <see cref="TileToWorld"/> and passes the
    /// frame's rotation by hand. With a per-world transform in the engine, all of this collapses into
    /// one matrix in <c>Begin</c> and renderers draw in plain grid coordinates
    /// (ENGINE_REQUESTS.md, "Per-world draw transform").
    /// </remarks>
    public static class SurfaceView
    {
        public const float TileSize = 32f;

        public static Vector2 TileToWorld(in SurfaceFrame frame, Vector2 localTile) => frame.LocalToWorld(localTile) * TileSize;

        public static Vector2 WorldToTile(in SurfaceFrame frame, Vector2 worldPixels) => frame.WorldToLocal(worldPixels / TileSize);

        /// <summary>The block of grid tiles that can overlap the culling rectangle, clamped to the grid.</summary>
        public static (int MinX, int MinY, int MaxX, int MaxY) VisibleTiles(in SurfaceFrame frame, Rectangle cullingBounds, SurfaceGrid grid)
        {
            Span<Vector2> corners =
            [
                WorldToTile(frame, new Vector2(cullingBounds.Left, cullingBounds.Top)),
                WorldToTile(frame, new Vector2(cullingBounds.Right, cullingBounds.Top)),
                WorldToTile(frame, new Vector2(cullingBounds.Right, cullingBounds.Bottom)),
                WorldToTile(frame, new Vector2(cullingBounds.Left, cullingBounds.Bottom)),
            ];

            var min = corners[0];
            var max = corners[0];
            foreach (var corner in corners)
            {
                min = Vector2.Min(min, corner);
                max = Vector2.Max(max, corner);
            }

            return (
                Math.Max(0, (int)MathF.Floor(min.X) - 1),
                Math.Max(0, (int)MathF.Floor(min.Y) - 1),
                Math.Min(grid.Width - 1, (int)MathF.Ceiling(max.X) + 1),
                Math.Min(grid.Height - 1, (int)MathF.Ceiling(max.Y) + 1));
        }

        public static bool IsVisible((int MinX, int MinY, int MaxX, int MaxY) visible, int x, int y, int size) =>
            x + size - 1 >= visible.MinX && x <= visible.MaxX && y + size - 1 >= visible.MinY && y <= visible.MaxY;

        public static Vector2 DirectionVector(Direction direction) => new(direction.Dx(), direction.Dy());
    }
}
