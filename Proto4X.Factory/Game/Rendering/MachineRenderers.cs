using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Components;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;
using Proto4X.Factory.Simulation.Power;
using System;
using System.Collections.Generic;

namespace Proto4X.Factory.Rendering
{
    /// <summary>Inserter base plus an arm that sweeps from the pickup side to the drop side, carrying its item.</summary>
    public sealed class InserterRenderer(Surface surface) : IGeometryDrawSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Placement>(), ComponentRef.Of<Inserter>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(GeometryBatch batch, Rectangle localCullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            var visible = SurfaceView.VisibleTiles(localCullingBounds, surface.Grid);
            var baseColour = Palette.Building(BuildingKind.Inserter) * 0.7f;
            baseColour.A = 255;

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
                    var centre = new Vector2(placement.X + 0.5f, placement.Y + 0.5f);
                    Shapes.FillCentred(batch, GeometryLayer.Opaque, centre, new Vector2(0.45f), 0f, FlatDepth.Inserter, baseColour);

                    var hand = Hand(placement, inserter);
                    Shapes.Line(batch, GeometryLayer.Opaque, centre, hand, 3f / SurfaceView.TileSize, FlatDepth.InserterArm, Palette.Building(BuildingKind.Inserter));
                    if (inserter.Held != ItemId.None)
                    {
                        Shapes.FillCentred(batch, GeometryLayer.Opaque, hand, new Vector2(0.24f), 0f, FlatDepth.Held, Palette.Item(inserter.Held));
                    }
                }
            }
        }

        /// <summary>How far through its swing an inserter is: 0 over the pickup side, 1 over the drop side.</summary>
        public static float SwingFraction(in Inserter inserter)
        {
            const float swingUnits = Buildings.InserterSwingTicks * PowerGrid.Full;
            var t = inserter.Phase switch
            {
                InserterPhase.WaitingForPickup => 0f,
                InserterPhase.SwingingToDrop => inserter.Progress / swingUnits,
                InserterPhase.WaitingToDrop => 1f,
                _ => 1f - inserter.Progress / swingUnits,
            };
            return Math.Clamp(t, 0f, 1f);
        }

        /// <summary>Where the hand is, in grid-local tiles: a half-circle sweep from behind to in front.</summary>
        public static Vector2 Hand(in Placement placement, in Inserter inserter)
        {
            var centre = new Vector2(placement.X + 0.5f, placement.Y + 0.5f);
            var forward = SurfaceView.DirectionVector(placement.Direction);
            var side = SurfaceView.DirectionVector(placement.Direction.Left());
            var angle = MathF.PI * SwingFraction(inserter);
            return centre + (-forward * MathF.Cos(angle) + side * MathF.Sin(angle) * 0.6f) * 0.75f;
        }
    }

    /// <summary>A progress bar along the bottom of each furnace and assembler while it crafts.</summary>
    public sealed class CrafterProgressRenderer(Surface surface) : IGeometryDrawSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Placement>(), ComponentRef.Of<Crafter>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(GeometryBatch batch, Rectangle localCullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            var visible = SurfaceView.VisibleTiles(localCullingBounds, surface.Grid, padding: 3);

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

                    DrawBar(batch, placement, Fraction(crafter), Palette.Progress);
                }
            }
        }

        public static float Fraction(in Crafter crafter) =>
            Math.Clamp(crafter.Progress / (float)(Recipes.Get(crafter.Recipe).Ticks * PowerGrid.Full), 0f, 1f);

        internal static void DrawBar(GeometryBatch batch, in Placement placement, float fraction, Color colour)
        {
            var x = placement.X + 0.1f;
            var y = placement.Y + placement.Size - 0.22f;
            var width = placement.Size - 0.2f;
            Shapes.FillRect(batch, GeometryLayer.Translucent, x, y, width, 0.12f, FlatDepth.Bar, Color.Black * 0.6f);
            Shapes.FillRect(batch, GeometryLayer.Opaque, x, y, width * fraction, 0.12f, FlatDepth.BarFill, colour);
        }
    }

    /// <summary>Launch countdown on silos.</summary>
    public sealed class LauncherProgressRenderer : IGeometryDrawSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Placement>(), ComponentRef.Of<Launcher>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(GeometryBatch batch, Rectangle localCullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            foreach (var chunk in archetypeChunks)
            {
                var placementSpan = chunk.GetSpan<Placement>();
                var launcherSpan = chunk.GetSpan<Launcher>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var launcher = ref launcherSpan[i];
                    if (launcher.IsLaunching)
                    {
                        CrafterProgressRenderer.DrawBar(batch, placementSpan[i], Fraction(launcher), Color.White);
                    }
                }
            }
        }

        public static float Fraction(in Launcher launcher) =>
            Math.Clamp(launcher.Progress / (float)(Buildings.LaunchTicks * PowerGrid.Full), 0f, 1f);
    }
}
