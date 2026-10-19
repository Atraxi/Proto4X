using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Belts;
using Proto4X.Factory.Simulation.Components;
using Proto4X.Factory.Simulation.Grid;
using System.Collections.Generic;

namespace Proto4X.Factory.Rendering
{
    /// <summary>Belt tiles, with chevrons that crawl at belt speed so a running belt reads as running.</summary>
    public sealed class BeltRenderer(Surface surface) : IGeometryDrawSystem
    {
        private const float ChevronThickness = 2f / SurfaceView.TileSize;

        private static readonly ComponentRef[] requiredComponentProviders =
        [
            ComponentRef.Of<Building>(), ComponentRef.Of<Placement>(), ComponentRef.Of<BeltTile>(),
        ];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(GeometryBatch batch, Rectangle localCullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            var visible = SurfaceView.VisibleTiles(localCullingBounds, surface.Grid);
            var crawl = surface.CurrentTick * BeltNetwork.Speed % BeltNetwork.TileLength / (float)BeltNetwork.TileLength;

            foreach (var chunk in archetypeChunks)
            {
                var buildingSpan = chunk.GetSpan<Building>();
                var placementSpan = chunk.GetSpan<Placement>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var placement = ref placementSpan[i];
                    if (!SurfaceView.IsVisible(visible, placement.X, placement.Y, 1))
                    {
                        continue;
                    }

                    var centre = new Vector2(placement.X + 0.5f, placement.Y + 0.5f);
                    Shapes.FillCentred(batch, GeometryLayer.Opaque, centre, new Vector2(0.86f), 0f, FlatDepth.Belt, Palette.Belt);

                    var forward = SurfaceView.DirectionVector(placement.Direction);
                    var side = SurfaceView.DirectionVector(placement.Direction.Left());
                    var shape = surface.Belts.ShapeOf(buildingSpan[i].Slot);

                    for (var c = 0; c < 2; c++)
                    {
                        var along = (c * 0.5f + crawl * 0.5f) % 1f - 0.5f;
                        var tip = centre + forward * (along + 0.12f);
                        var wingLeft = centre + forward * along + side * 0.3f;
                        var wingRight = centre + forward * along - side * 0.3f;
                        Shapes.Line(batch, GeometryLayer.Opaque, wingLeft, tip, ChevronThickness, FlatDepth.BeltDetail, Palette.BeltChevron);
                        Shapes.Line(batch, GeometryLayer.Opaque, wingRight, tip, ChevronThickness, FlatDepth.BeltDetail, Palette.BeltChevron);
                    }

                    if (shape != BeltShape.Straight)
                    {
                        // Mark the side a curve is fed from.
                        var feedSide = shape == BeltShape.CurveFromLeft ? side : -side;
                        Shapes.FillCentred(batch, GeometryLayer.Opaque, centre + feedSide * 0.4f, new Vector2(0.12f), 0f, FlatDepth.BeltDetail, Palette.BeltChevron);
                    }
                }
            }
        }
    }
}
