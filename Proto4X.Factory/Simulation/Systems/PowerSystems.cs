using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Simulation.Components;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Power;
using System;
using System.Collections.Generic;

namespace Proto4X.Factory.Simulation.Systems
{
    /// <summary>
    /// Burns fuel for what the network drew last tick, refuels from the coal stack, and offers this
    /// tick's capacity to the network.
    /// </summary>
    public sealed class GeneratorSystem(Surface surface) : IUpdateSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Building>(), ComponentRef.Of<Generator>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            var power = surface.Power;
            var powerVersion = power.Version;
            var load = power.Load;
            var capacity = power.Capacity;
            var inventories = surface.Inventories;

            foreach (var chunk in archetypeChunks)
            {
                var buildingSpan = chunk.GetSpan<Building>();
                var generatorSpan = chunk.GetSpan<Generator>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var generator = ref generatorSpan[i];
                    if (generator.PowerVersion != powerVersion)
                    {
                        generator.PowerIndex = power.IndexOf(buildingSpan[i].Slot);
                        generator.PowerVersion = powerVersion;
                    }

                    generator.FuelEnergy -= (long)generator.LastOffered * load[generator.PowerIndex] / PowerGrid.Full;

                    if (generator.FuelEnergy < Buildings.GeneratorMaxOutput && inventories.CountAt(generator.Inventory) > 0)
                    {
                        inventories.RemoveFromStack(generator.Inventory, 1);
                        generator.FuelEnergy += Items.CoalFuelValue;
                    }

                    var offered = (int)Math.Min(Buildings.GeneratorMaxOutput, generator.FuelEnergy);
                    capacity[generator.PowerIndex] += offered;
                    generator.LastOffered = offered;
                }
            }
        }
    }

    /// <summary>Turns last tick's demand and this tick's capacity into satisfaction. Iterates no entities.</summary>
    public sealed class PowerSolveSystem(Surface surface) : IUpdateSystem
    {
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => [];

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks) => surface.Power.Solve();
    }

    /// <summary>Advances every belt segment one tick. Iterates no entities - see BeltNetwork for why.</summary>
    public sealed class BeltSystem(Surface surface) : IUpdateSystem
    {
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => [];

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks) => surface.Belts.Tick();
    }
}
