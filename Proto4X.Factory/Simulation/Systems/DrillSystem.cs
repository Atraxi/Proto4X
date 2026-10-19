using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Simulation.Components;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Power;
using System.Collections.Generic;

namespace Proto4X.Factory.Simulation.Systems
{
    /// <summary>
    /// Mines the ore under each drill (or conjures a collector's product) and pushes it to whatever is
    /// on the output tile.
    /// </summary>
    public sealed class DrillSystem(Surface surface) : IUpdateSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders =
        [
            ComponentRef.Of<Building>(), ComponentRef.Of<Placement>(), ComponentRef.Of<Drill>(),
        ];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            var grid = surface.Grid;
            var power = surface.Power;
            var powerVersion = power.Version;
            var satisfaction = power.Satisfaction;
            var demand = power.Demand;
            const int ticksToMine = Buildings.DrillTicksPerOre * PowerGrid.Full;

            foreach (var chunk in archetypeChunks)
            {
                var buildingSpan = chunk.GetSpan<Building>();
                var placementSpan = chunk.GetSpan<Placement>();
                var drillSpan = chunk.GetSpan<Drill>();

                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var drill = ref drillSpan[i];
                    ref var building = ref buildingSpan[i];

                    var chunkVersion = grid.ChunkVersion(drill.Chunk);
                    if (drill.WiringVersion != chunkVersion)
                    {
                        drill.OutputSlot = Exchange.ResolveAt(surface, drill.OutputX, drill.OutputY);
                        drill.WiringVersion = chunkVersion;
                    }

                    if (drill.PowerVersion != powerVersion)
                    {
                        drill.PowerIndex = power.IndexOf(building.Slot);
                        drill.PowerVersion = powerVersion;
                    }

                    if (drill.Held != ItemId.None)
                    {
                        if (drill.OutputSlot < 0 || !Exchange.TryDeliver(surface, drill.OutputSlot, drill.Held, placementSpan[i].Direction))
                        {
                            continue;
                        }

                        drill.Held = ItemId.None;
                    }

                    if (drill.Depleted)
                    {
                        continue;
                    }

                    var spec = Buildings.Spec(building.Kind);
                    demand[drill.PowerIndex] += spec.ActiveDraw;
                    drill.Progress += satisfaction[drill.PowerIndex];
                    if (drill.Progress < ticksToMine)
                    {
                        continue;
                    }

                    drill.Progress -= ticksToMine;
                    drill.Held = drill.Produces != ItemId.None ? drill.Produces : MineFootprint(ref placementSpan[i]);
                    if (drill.Held == ItemId.None)
                    {
                        drill.Depleted = true;
                        continue;
                    }

                    if (drill.OutputSlot >= 0 && Exchange.TryDeliver(surface, drill.OutputSlot, drill.Held, placementSpan[i].Direction))
                    {
                        drill.Held = ItemId.None;
                    }
                }
            }
        }

        private ItemId MineFootprint(ref Placement placement)
        {
            var grid = surface.Grid;
            for (var y = placement.Y; y < placement.Y + placement.Size; y++)
            {
                for (var x = placement.X; x < placement.X + placement.Size; x++)
                {
                    var mined = grid.MineOne(x, y);
                    if (mined != ItemId.None)
                    {
                        return mined;
                    }
                }
            }

            return ItemId.None;
        }
    }
}
