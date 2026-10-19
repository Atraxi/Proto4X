using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Systems.Infrastructure;
using MonoGameLibrary.UI;
using Proto4X.Factory.Input;
using Proto4X.Factory.Rendering.Models;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Belts;
using Proto4X.Factory.Simulation.Components;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;
using System;
using System.Collections.Generic;

namespace Proto4X.Factory.Rendering.ThreeD
{
    /// <summary>
    /// How far the camera is, for level of detail. Shared by the 3D renderers and refreshed once a
    /// frame by the style.
    /// </summary>
    public sealed class Detail
    {
        /// <summary>Beyond this camera distance, small parts (ore rocks, belt chevrons, item cubes) drop out.</summary>
        public const float FineDistance = 70f;

        /// <summary>Beyond this, ground is drawn in 4x4 blocks and models as their first (base) box only.</summary>
        public const float CoarseDistance = 140f;

        public float Distance { get; set; } = 30f;
        public bool Fine => Distance < FineDistance;
        public bool Coarse => Distance >= CoarseDistance;
    }

    /// <summary>Ground, deck plates with a hull edge where they meet void, and ore as clusters of rocks.</summary>
    public sealed class Terrain3DRenderer(Surface surface, Detail detail) : IGeometryDrawSystem
    {
        private static readonly Color hull = new(44, 48, 58);

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => [];

        public void Draw(GeometryBatch batch, Rectangle localCullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            var grid = surface.Grid;
            var visible = SurfaceView.VisibleTiles(localCullingBounds, grid, padding: 2);

            if (detail.Coarse)
            {
                DrawCoarse(batch, grid, visible);
                return;
            }

            for (var y = visible.MinY; y <= visible.MaxY; y++)
            {
                for (var x = visible.MinX; x <= visible.MaxX; x++)
                {
                    var tile = grid.TileAt(x, y);
                    if (tile == TileKind.Void)
                    {
                        continue;
                    }

                    var shade = 0.94f + 0.06f * Mesh3D.Hash01(x, y);
                    var colour = Palette.Tile(tile) * shade;
                    colour.A = 255;
                    Mesh3D.Flat(batch, GeometryLayer.Opaque, x, y, 1f, 1f, 0f, colour);

                    if (tile == TileKind.Foundation)
                    {
                        DrawHullEdges(batch, grid, x, y);
                    }

                    var ore = grid.OreAt(x, y);
                    if (ore != ItemId.None)
                    {
                        DrawOre(batch, x, y, ore);
                    }
                }
            }
        }

        private void DrawOre(GeometryBatch batch, int x, int y, ItemId ore)
        {
            var colour = Palette.Item(ore);
            var dark = ModelMesher.Lit(colour, 0.55f);
            if (!detail.Fine)
            {
                Mesh3D.Flat(batch, GeometryLayer.Opaque, x + 0.15f, y + 0.15f, 0.7f, 0.7f, 0.01f, dark);
                return;
            }

            for (var rock = 0; rock < 3; rock++)
            {
                var size = 0.16f + 0.14f * Mesh3D.Hash01(x, y, rock * 3);
                var centre = new Vector2(x + 0.2f + 0.6f * Mesh3D.Hash01(x, y, rock * 3 + 1), y + 0.2f + 0.6f * Mesh3D.Hash01(x, y, rock * 3 + 2));
                var height = size * (0.5f + 0.5f * Mesh3D.Hash01(x, y, rock + 17));
                Mesh3D.Box(batch, GeometryLayer.Opaque, new Vector3(centre.X - size * 0.5f, centre.Y - size * 0.5f, 0f),
                    new Vector3(centre.X + size * 0.5f, centre.Y + size * 0.5f, height), rock == 0 ? colour : dark);
            }
        }

        /// <summary>A ship deck is a slab: drop a hull wall wherever a deck tile borders void.</summary>
        private static void DrawHullEdges(GeometryBatch batch, SurfaceGrid grid, int x, int y)
        {
            const float depth = -0.45f;
            if (IsVoid(grid, x, y - 1))
            {
                Mesh3D.Quad(batch, GeometryLayer.Opaque, new Vector3(x, y, 0), new Vector3(x + 1, y, 0), new Vector3(x + 1, y, depth), new Vector3(x, y, depth), -Vector3.UnitY, hull);
            }

            if (IsVoid(grid, x, y + 1))
            {
                Mesh3D.Quad(batch, GeometryLayer.Opaque, new Vector3(x, y + 1, 0), new Vector3(x + 1, y + 1, 0), new Vector3(x + 1, y + 1, depth), new Vector3(x, y + 1, depth), Vector3.UnitY, hull);
            }

            if (IsVoid(grid, x - 1, y))
            {
                Mesh3D.Quad(batch, GeometryLayer.Opaque, new Vector3(x, y, 0), new Vector3(x, y + 1, 0), new Vector3(x, y + 1, depth), new Vector3(x, y, depth), -Vector3.UnitX, hull);
            }

            if (IsVoid(grid, x + 1, y))
            {
                Mesh3D.Quad(batch, GeometryLayer.Opaque, new Vector3(x + 1, y, 0), new Vector3(x + 1, y + 1, 0), new Vector3(x + 1, y + 1, depth), new Vector3(x + 1, y, depth), Vector3.UnitX, hull);
            }
        }

        private static bool IsVoid(SurfaceGrid grid, int x, int y) =>
            x < 0 || y < 0 || x >= grid.Width || y >= grid.Height || grid.TileAt(x, y) == TileKind.Void;

        /// <summary>Far out, one quad per 4x4 block, coloured by its first tile and any ore in it.</summary>
        private static void DrawCoarse(GeometryBatch batch, SurfaceGrid grid, (int MinX, int MinY, int MaxX, int MaxY) visible)
        {
            const int block = 4;
            for (var by = visible.MinY / block * block; by <= visible.MaxY; by += block)
            {
                for (var bx = visible.MinX / block * block; bx <= visible.MaxX; bx += block)
                {
                    var tile = grid.TileAt(bx, by);
                    if (tile == TileKind.Void)
                    {
                        continue;
                    }

                    var colour = Palette.Tile(tile);
                    var ore = grid.OreAt(bx + 1, by + 1);
                    if (ore != ItemId.None)
                    {
                        colour = Color.Lerp(colour, Palette.Item(ore), 0.35f);
                    }

                    var w = Math.Min(block, grid.Width - bx);
                    var h = Math.Min(block, grid.Height - by);
                    Mesh3D.Flat(batch, GeometryLayer.Opaque, bx, by, w, h, 0f, colour);
                }
            }
        }
    }

    /// <summary>Belts as low slabs with rails, chevrons crawling along the top.</summary>
    public sealed class Belt3DRenderer(Surface surface, Detail detail) : IGeometryDrawSystem
    {
        private const float Top = 0.1f;

        private static readonly ComponentRef[] requiredComponentProviders =
        [
            ComponentRef.Of<Building>(), ComponentRef.Of<Placement>(), ComponentRef.Of<BeltTile>(),
        ];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(GeometryBatch batch, Rectangle localCullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            var visible = SurfaceView.VisibleTiles(localCullingBounds, surface.Grid, padding: 2);
            var crawl = surface.CurrentTick * BeltNetwork.Speed % BeltNetwork.TileLength / (float)BeltNetwork.TileLength;
            var bed = new Color(58, 60, 62);
            var rail = new Color(120, 112, 90);

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

                    var x = placement.X;
                    var y = placement.Y;
                    var horizontal = placement.Direction is Direction.East or Direction.West;
                    Mesh3D.Box(batch, GeometryLayer.Opaque,
                        new Vector3(x + (horizontal ? 0f : 0.1f), y + (horizontal ? 0.1f : 0f), 0f),
                        new Vector3(x + (horizontal ? 1f : 0.9f), y + (horizontal ? 0.9f : 1f), Top), bed);

                    if (!detail.Fine)
                    {
                        continue;
                    }

                    // Rails along both sides.
                    if (horizontal)
                    {
                        Mesh3D.Box(batch, GeometryLayer.Opaque, new Vector3(x, y + 0.06f, 0f), new Vector3(x + 1f, y + 0.14f, Top + 0.05f), rail);
                        Mesh3D.Box(batch, GeometryLayer.Opaque, new Vector3(x, y + 0.86f, 0f), new Vector3(x + 1f, y + 0.94f, Top + 0.05f), rail);
                    }
                    else
                    {
                        Mesh3D.Box(batch, GeometryLayer.Opaque, new Vector3(x + 0.06f, y, 0f), new Vector3(x + 0.14f, y + 1f, Top + 0.05f), rail);
                        Mesh3D.Box(batch, GeometryLayer.Opaque, new Vector3(x + 0.86f, y, 0f), new Vector3(x + 0.94f, y + 1f, Top + 0.05f), rail);
                    }

                    var centre = new Vector2(x + 0.5f, y + 0.5f);
                    var forward = SurfaceView.DirectionVector(placement.Direction);
                    var side = SurfaceView.DirectionVector(placement.Direction.Left());
                    for (var c = 0; c < 2; c++)
                    {
                        var along = (c * 0.5f + crawl * 0.5f) % 1f - 0.5f;
                        var tip = centre + forward * (along + 0.12f);
                        Mesh3D.FlatLine(batch, GeometryLayer.Opaque, centre + forward * along + side * 0.28f, tip, 0.06f, Top + 0.005f, Palette.BeltChevron);
                        Mesh3D.FlatLine(batch, GeometryLayer.Opaque, centre + forward * along - side * 0.28f, tip, 0.06f, Top + 0.005f, Palette.BeltChevron);
                    }

                    if (surface.Belts.ShapeOf(buildingSpan[i].Slot) is var shape && shape != BeltShape.Straight)
                    {
                        var feedSide = shape == BeltShape.CurveFromLeft ? side : -side;
                        var marker = centre + feedSide * 0.38f;
                        Mesh3D.Flat(batch, GeometryLayer.Opaque, marker.X - 0.06f, marker.Y - 0.06f, 0.12f, 0.12f, Top + 0.006f, Palette.BeltChevron);
                    }
                }
            }
        }
    }

    /// <summary>Belt items as little cubes riding the slab.</summary>
    public sealed class BeltItem3DRenderer(Surface surface, Detail detail) : IGeometryDrawSystem
    {
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => [];

        public void Draw(GeometryBatch batch, Rectangle localCullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            var visible = SurfaceView.VisibleTiles(localCullingBounds, surface.Grid, padding: 2);
            var coarse = !detail.Fine;
            foreach (var item in BeltItems.Visible(surface, visible))
            {
                if (coarse)
                {
                    Mesh3D.Flat(batch, GeometryLayer.Opaque, item.Position.X - 0.11f, item.Position.Y - 0.11f, 0.22f, 0.22f, 0.12f, Palette.Item(item.Item));
                }
                else
                {
                    Mesh3D.Cube(batch, GeometryLayer.Opaque, item.Position, 0.2f, 0.1f, Palette.Item(item.Item));
                }
            }
        }
    }

    /// <summary>
    /// Every building's block model, its ground shadow, and its status: the recipe as a floating
    /// cube on assemblers, the top item on chests, and a progress bar over crafters and silos.
    /// </summary>
    public sealed class Building3DRenderer(Surface surface, Detail detail) : IGeometryDrawSystem
    {
        private const float ShadowDarkness = 0.6f;
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Building>(), ComponentRef.Of<Placement>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(GeometryBatch batch, Rectangle localCullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            // Tall models and their shadows reach past their footprint, so cull generously.
            var visible = SurfaceView.VisibleTiles(localCullingBounds, surface.Grid, padding: 4);
            var buildings = surface.Buildings;
            var inventories = surface.Inventories;
            var shadow = Projections.SunShadow();
            var spin = surface.CurrentTick * 0.03f;

            foreach (var chunk in archetypeChunks)
            {
                var buildingSpan = chunk.GetSpan<Building>();
                var placementSpan = chunk.GetSpan<Placement>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var building = ref buildingSpan[i];
                    if (building.Kind == BuildingKind.Belt)
                    {
                        continue;
                    }

                    ref var placement = ref placementSpan[i];
                    if (!SurfaceView.IsVisible(visible, placement.X, placement.Y, placement.Size))
                    {
                        continue;
                    }

                    var model = BlockModels.For(building.Kind);
                    var place = BlockModels.Facing(placement.Direction, placement.Size) * Matrix.CreateTranslation(placement.X, placement.Y, 0f);
                    var pending = buildings.IsPendingRemoval(building.Slot);

                    if (detail.Coarse)
                    {
                        // Far out: the base box alone carries the silhouette and colour.
                        ModelMesher.EmitBox(batch, GeometryLayer.Opaque, model.Boxes[0], place, Color.White);
                        continue;
                    }

                    if (pending)
                    {
                        ModelMesher.EmitModel(batch, GeometryLayer.Translucent, model, place, Color.White * 0.4f);
                        continue;
                    }

                    ModelMesher.EmitModel(batch, GeometryLayer.Opaque, model, place, Color.White);

                    // Shadow: the model flattened onto the ground along the sun, a hair above it,
                    // opaque so overlapping faces don't double-darken.
                    var groundColour = Palette.Tile(surface.Grid.TileAt(placement.X, placement.Y));
                    var shade = ModelMesher.Lit(ModelMesher.Lit(groundColour, ModelMesher.Light(Vector3.UnitZ)), ShadowDarkness);
                    var lift = Matrix.CreateTranslation(0f, 0f, 0.004f);
                    foreach (var box in model.Boxes)
                    {
                        ModelMesher.EmitBox(batch, GeometryLayer.Opaque, box with { Colour = shade, Emissive = true }, place * shadow * lift, Color.White);
                    }

                    var centre = new Vector2(placement.X + placement.Size * 0.5f, placement.Y + placement.Size * 0.5f);
                    if (building.Kind == BuildingKind.Assembler && detail.Fine)
                    {
                        // The recipe's product, floating and turning above the machine.
                        var result = Recipes.Get(buildings.RecipeOf(building.Slot)).Result;
                        var turn = Matrix.CreateTranslation(-0.5f, -0.5f, 0f) * Matrix.CreateRotationZ(spin) * Matrix.CreateScale(0.4f) *
                            Matrix.CreateTranslation(centre.X, centre.Y, 1.55f);
                        ModelMesher.EmitBox(batch, GeometryLayer.Opaque, new ModelBox(Vector3.Zero, Vector3.One, Palette.Item(result), Emissive: true), turn, Color.White);
                    }

                    var start = buildings.InventoryStart(building.Slot);
                    if (detail.Fine && start >= 0 && building.Kind is BuildingKind.Chest or BuildingKind.Receiver)
                    {
                        var item = TopItem(inventories, start, buildings.InventoryLength(building.Slot));
                        if (item != ItemId.None)
                        {
                            Mesh3D.Cube(batch, GeometryLayer.Opaque, centre, 0.26f, model.Height, Palette.Item(item));
                        }
                    }
                }
            }
        }

        private static ItemId TopItem(Simulation.Stores.InventoryStore inventories, int start, int length)
        {
            var best = ItemId.None;
            var bestCount = 0;
            for (var k = 0; k < length; k++)
            {
                var count = inventories.CountAt(start + k);
                if (count > bestCount)
                {
                    bestCount = count;
                    best = inventories.ItemAt(start + k);
                }
            }

            return best;
        }
    }

    /// <summary>Progress bars floating flat above crafters and silos, readable from any angle above.</summary>
    public sealed class Progress3DRenderer(Surface surface, Detail detail) : IGeometryDrawSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Placement>(), ComponentRef.Of<Crafter>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(GeometryBatch batch, Rectangle localCullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            if (detail.Coarse)
            {
                return;
            }

            var visible = SurfaceView.VisibleTiles(localCullingBounds, surface.Grid, padding: 4);
            foreach (var chunk in archetypeChunks)
            {
                var placementSpan = chunk.GetSpan<Placement>();
                var crafterSpan = chunk.GetSpan<Crafter>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var placement = ref placementSpan[i];
                    ref var crafter = ref crafterSpan[i];
                    if (!crafter.IsCrafting || !SurfaceView.IsVisible(visible, placement.X, placement.Y, placement.Size))
                    {
                        continue;
                    }

                    DrawBar(batch, placement, CrafterProgressRenderer.Fraction(crafter), Palette.Progress);
                }
            }
        }

        internal static void DrawBar(GeometryBatch batch, in Placement placement, float fraction, Color colour)
        {
            // Just south of the footprint, at knee height, so it never hides inside a tall model.
            var x = placement.X + 0.1f;
            var y = placement.Y + placement.Size + 0.02f;
            var width = placement.Size - 0.2f;
            Mesh3D.Box(batch, GeometryLayer.Opaque, new Vector3(x, y, 0f), new Vector3(x + width, y + 0.14f, 0.06f), new Color(20, 22, 26));
            Mesh3D.Box(batch, GeometryLayer.Opaque, new Vector3(x, y + 0.02f, 0.06f), new Vector3(x + width * fraction, y + 0.12f, 0.08f), colour, emissive: true);
        }
    }

    public sealed class Launcher3DRenderer(Detail detail) : IGeometryDrawSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Placement>(), ComponentRef.Of<Launcher>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(GeometryBatch batch, Rectangle localCullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            if (detail.Coarse)
            {
                return;
            }

            foreach (var chunk in archetypeChunks)
            {
                var placementSpan = chunk.GetSpan<Placement>();
                var launcherSpan = chunk.GetSpan<Launcher>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var launcher = ref launcherSpan[i];
                    if (launcher.IsLaunching)
                    {
                        Progress3DRenderer.DrawBar(batch, placementSpan[i], LauncherProgressRenderer.Fraction(launcher), Color.White);
                    }
                }
            }
        }
    }

    /// <summary>Inserter arms: two beams from the pivot to the hand, with the held item as a cube.</summary>
    public sealed class Inserter3DRenderer(Surface surface, Detail detail) : IGeometryDrawSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Placement>(), ComponentRef.Of<Inserter>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(GeometryBatch batch, Rectangle localCullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            if (detail.Coarse)
            {
                return;
            }

            var visible = SurfaceView.VisibleTiles(localCullingBounds, surface.Grid, padding: 2);
            var arm = Palette.Building(BuildingKind.Inserter);
            foreach (var chunk in archetypeChunks)
            {
                var placementSpan = chunk.GetSpan<Placement>();
                var inserterSpan = chunk.GetSpan<Inserter>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var placement = ref placementSpan[i];
                    if (!SurfaceView.IsVisible(visible, placement.X, placement.Y, 1))
                    {
                        continue;
                    }

                    ref var inserter = ref inserterSpan[i];
                    var pivot = new Vector3(placement.X + 0.5f, placement.Y + 0.5f, 0.42f);
                    var hand2 = InserterRenderer.Hand(placement, inserter);
                    var hand = new Vector3(hand2, 0.3f);
                    var elbow = (pivot + hand) * 0.5f + new Vector3(0f, 0f, 0.32f);
                    Mesh3D.Beam(batch, GeometryLayer.Opaque, pivot, elbow, 0.08f, arm);
                    Mesh3D.Beam(batch, GeometryLayer.Opaque, elbow, hand, 0.07f, arm);
                    if (inserter.Held != ItemId.None)
                    {
                        Mesh3D.Cube(batch, GeometryLayer.Opaque, hand2, 0.18f, 0.12f, Palette.Item(inserter.Held));
                    }
                }
            }
        }
    }

    /// <summary>The build preview: the model itself, tinted green or red and see-through.</summary>
    public sealed class Ghost3DRenderer(Surface surface, BuildController controller) : IGeometryDrawSystem
    {
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => [];

        public void Draw(GeometryBatch batch, Rectangle localCullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            if (controller.Surface != surface)
            {
                return;
            }

            var (x, y) = controller.Hovered;
            if (controller.Tool == BuildingKind.None)
            {
                Mesh3D.Flat(batch, GeometryLayer.Translucent, x, y, 1f, 1f, 0.02f, Color.White * 0.25f, lit: false);
                return;
            }

            var valid = surface.CanPlace(controller.Tool, x, y, controller.Recipe) == PlacementResult.Placed;
            var tint = valid ? Palette.Valid : Palette.Invalid;
            var size = Buildings.Size(controller.Tool);
            Mesh3D.Flat(batch, GeometryLayer.Translucent, x, y, size, size, 0.02f, tint, lit: false);
            var model = BlockModels.For(controller.Tool);
            if (model.Boxes.Length > 0)
            {
                var place = BlockModels.Facing(controller.Direction, size) * Matrix.CreateTranslation(x, y, 0f);
                ModelMesher.EmitModel(batch, GeometryLayer.Translucent, model, place, tint);
            }
            else
            {
                // Belts have no model: an arrow on the ground.
                var centre = new Vector2(x + 0.5f, y + 0.5f);
                Mesh3D.FlatLine(batch, GeometryLayer.Translucent, centre, centre + SurfaceView.DirectionVector(controller.Direction) * 0.5f, 0.1f, 0.03f, Color.White * 0.8f);
            }
        }
    }
}
