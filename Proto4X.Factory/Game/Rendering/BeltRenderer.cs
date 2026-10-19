using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Belts;
using Proto4X.Factory.Simulation.Components;
using Proto4X.Factory.Simulation.Grid;
using System.Collections.Generic;

namespace Proto4X.Factory.Rendering
{
    /// <summary>Belt tiles, with chevrons that crawl at belt speed so a running belt reads as running.</summary>
    public sealed class BeltRenderer(Surface surface, Texture2D pixel) : IDrawSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders =
        [
            ComponentRef.Of<Building>(), ComponentRef.Of<Placement>(), ComponentRef.Of<BeltTile>(),
        ];

        public DrawSpace DrawSpace => DrawSpace.World;
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(SpriteBatch spriteBatch, Rectangle cullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            var frame = surface.Frame;
            var visible = SurfaceView.VisibleTiles(frame, cullingBounds, surface.Grid);
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
                    Primitives.FillCentred(spriteBatch, pixel, SurfaceView.TileToWorld(frame, centre), new Vector2(SurfaceView.TileSize * 0.86f), frame.Rotation, Palette.Belt);

                    var forward = SurfaceView.DirectionVector(placement.Direction);
                    var side = SurfaceView.DirectionVector(placement.Direction.Left());
                    var shape = surface.Belts.ShapeOf(buildingSpan[i].Slot);

                    for (var c = 0; c < 2; c++)
                    {
                        var along = (c * 0.5f + crawl * 0.5f) % 1f - 0.5f;
                        var tip = centre + forward * (along + 0.12f);
                        var wingLeft = centre + forward * along + side * 0.3f;
                        var wingRight = centre + forward * along - side * 0.3f;
                        Primitives.DrawLine(spriteBatch, pixel, SurfaceView.TileToWorld(frame, wingLeft), SurfaceView.TileToWorld(frame, tip), Palette.BeltChevron, 2f);
                        Primitives.DrawLine(spriteBatch, pixel, SurfaceView.TileToWorld(frame, wingRight), SurfaceView.TileToWorld(frame, tip), Palette.BeltChevron, 2f);
                    }

                    if (shape != BeltShape.Straight)
                    {
                        // Mark the side a curve is fed from.
                        var feedSide = shape == BeltShape.CurveFromLeft ? side : -side;
                        Primitives.FillCentred(spriteBatch, pixel, SurfaceView.TileToWorld(frame, centre + feedSide * 0.4f),
                            new Vector2(SurfaceView.TileSize * 0.12f), frame.Rotation, Palette.BeltChevron);
                    }
                }
            }
        }
    }
}
