using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Belts;
using Proto4X.Factory.Simulation.Grid;
using System.Collections.Generic;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// Items riding belts, read straight out of the lane segments - they are not entities, so there is
    /// nothing for the scheduler to hand this system. Curves are drawn as if straight along the tile's
    /// own direction, which is wrong by a few pixels on the corner tile and nowhere else.
    /// </summary>
    public sealed class BeltItemRenderer(Surface surface) : IGeometryDrawSystem
    {
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => [];

        public void Draw(GeometryBatch batch, Rectangle localCullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            var visible = SurfaceView.VisibleTiles(localCullingBounds, surface.Grid);
            foreach (var item in BeltItems.Visible(surface, visible))
            {
                Shapes.FillCentred(batch, GeometryLayer.Opaque, item.Position, new Vector2(0.22f), 0f, FlatDepth.BeltItem, Palette.Item(item.Item));
            }
        }
    }

    /// <summary>One item on a belt, in grid-local tiles.</summary>
    public readonly record struct BeltItem(Vector2 Position, Simulation.Data.ItemId Item, Direction Direction);

    /// <summary>
    /// Walks every lane segment and yields the items on visible tiles with their grid-local
    /// position. Shared by every style so they all agree on where an item is.
    /// </summary>
    public static class BeltItems
    {
        public static Enumerator Visible(Surface surface, (int MinX, int MinY, int MaxX, int MaxY) visible) => new(surface, visible);

        /// <summary>A struct enumerator, so a frame's walk allocates nothing.</summary>
        public struct Enumerator(Surface surface, (int MinX, int MinY, int MaxX, int MaxY) visible)
        {
            private int segmentIndex = -1;
            private int itemIndex;
            private int position;

            public BeltItem Current { get; private set; }

            public readonly Enumerator GetEnumerator() => this;

            public bool MoveNext()
            {
                var segments = surface.Belts.Segments;
                var buildings = surface.Buildings;
                while (true)
                {
                    if (segmentIndex >= 0 && segmentIndex < segments.Length && itemIndex < segments[segmentIndex].Count)
                    {
                        var segment = segments[segmentIndex];
                        position -= segment.Gaps[itemIndex];
                        var i = itemIndex++;
                        var tileIndex = System.Math.Min(position / BeltNetwork.TileLength, segment.TileCount - 1);
                        var slot = segment.TileSlots[tileIndex];
                        var x = buildings.OriginX(slot);
                        var y = buildings.OriginY(slot);
                        if (!SurfaceView.IsVisible(visible, x, y, 1))
                        {
                            continue;
                        }

                        var along = (position - tileIndex * BeltNetwork.TileLength) / (float)BeltNetwork.TileLength - 0.5f;
                        var direction = buildings.DirectionOf(slot);
                        var forward = SurfaceView.DirectionVector(direction);
                        var left = SurfaceView.DirectionVector(direction.Left());
                        var lateral = segment.Lane == 0 ? 0.24f : -0.24f;
                        Current = new BeltItem(new Vector2(x + 0.5f, y + 0.5f) + forward * along + left * lateral, segment.Items[i], direction);
                        return true;
                    }

                    segmentIndex++;
                    if (segmentIndex >= segments.Length)
                    {
                        return false;
                    }

                    itemIndex = 0;
                    position = segments[segmentIndex].Length;
                }
            }
        }
    }
}
