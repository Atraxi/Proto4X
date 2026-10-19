using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
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
    public sealed class InserterRenderer(Surface surface, Texture2D pixel) : IDrawSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Placement>(), ComponentRef.Of<Inserter>()];

        public DrawSpace DrawSpace => DrawSpace.World;
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(SpriteBatch spriteBatch, Rectangle cullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            var frame = surface.Frame;
            var visible = SurfaceView.VisibleTiles(frame, cullingBounds, surface.Grid);
            const float swingUnits = Buildings.InserterSwingTicks * PowerGrid.Full;

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
                    Primitives.FillCentred(spriteBatch, pixel, SurfaceView.TileToWorld(frame, centre), new Vector2(SurfaceView.TileSize * 0.45f), frame.Rotation, Palette.Building(BuildingKind.Inserter) * 0.7f);

                    // 0 = over the pickup side, 1 = over the drop side.
                    var t = inserter.Phase switch
                    {
                        InserterPhase.WaitingForPickup => 0f,
                        InserterPhase.SwingingToDrop => inserter.Progress / swingUnits,
                        InserterPhase.WaitingToDrop => 1f,
                        _ => 1f - inserter.Progress / swingUnits,
                    };

                    var forward = SurfaceView.DirectionVector(placement.Direction);
                    var side = SurfaceView.DirectionVector(placement.Direction.Left());
                    var angle = MathF.PI * Math.Clamp(t, 0f, 1f);
                    var hand = centre + (-forward * MathF.Cos(angle) + side * MathF.Sin(angle) * 0.6f) * 0.75f;

                    Primitives.DrawLine(spriteBatch, pixel, SurfaceView.TileToWorld(frame, centre), SurfaceView.TileToWorld(frame, hand), Palette.Building(BuildingKind.Inserter), 3f);
                    if (inserter.Held != ItemId.None)
                    {
                        Primitives.FillCentred(spriteBatch, pixel, SurfaceView.TileToWorld(frame, hand), new Vector2(SurfaceView.TileSize * 0.24f), frame.Rotation, Palette.Item(inserter.Held));
                    }
                }
            }
        }
    }

    /// <summary>A progress bar along the bottom of each furnace and assembler while it crafts.</summary>
    public sealed class CrafterProgressRenderer(Surface surface, Texture2D pixel) : IDrawSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Placement>(), ComponentRef.Of<Crafter>()];

        public DrawSpace DrawSpace => DrawSpace.World;
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(SpriteBatch spriteBatch, Rectangle cullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            var frame = surface.Frame;
            var visible = SurfaceView.VisibleTiles(frame, cullingBounds, surface.Grid);

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

                    var fraction = crafter.Progress / (float)(Recipes.Get(crafter.Recipe).Ticks * PowerGrid.Full);
                    DrawBar(spriteBatch, pixel, frame, placement, Math.Clamp(fraction, 0f, 1f), Palette.Progress);
                }
            }
        }

        internal static void DrawBar(SpriteBatch spriteBatch, Texture2D pixel, in SurfaceFrame frame, in Placement placement, float fraction, Color colour)
        {
            var corner = new Vector2(placement.X + 0.1f, placement.Y + placement.Size - 0.22f);
            var size = new Vector2((placement.Size - 0.2f) * SurfaceView.TileSize, 0.12f * SurfaceView.TileSize);
            Primitives.FillRectangle(spriteBatch, pixel, SurfaceView.TileToWorld(frame, corner), size, frame.Rotation, Color.Black * 0.6f);
            Primitives.FillRectangle(spriteBatch, pixel, SurfaceView.TileToWorld(frame, corner), new Vector2(size.X * fraction, size.Y), frame.Rotation, colour);
        }
    }

    /// <summary>Launch countdown on silos.</summary>
    public sealed class LauncherProgressRenderer(Surface surface, Texture2D pixel) : IDrawSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Placement>(), ComponentRef.Of<Launcher>()];

        public DrawSpace DrawSpace => DrawSpace.World;
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Draw(SpriteBatch spriteBatch, Rectangle cullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            var frame = surface.Frame;
            foreach (var chunk in archetypeChunks)
            {
                var placementSpan = chunk.GetSpan<Placement>();
                var launcherSpan = chunk.GetSpan<Launcher>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var launcher = ref launcherSpan[i];
                    if (launcher.IsLaunching)
                    {
                        var fraction = launcher.Progress / (float)(Buildings.LaunchTicks * PowerGrid.Full);
                        CrafterProgressRenderer.DrawBar(spriteBatch, pixel, frame, placementSpan[i], Math.Clamp(fraction, 0f, 1f), Color.White);
                    }
                }
            }
        }
    }
}
