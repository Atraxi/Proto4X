using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Components;
using Proto4X.Factory.Simulation.Data;
using System.Collections.Generic;

namespace Proto4X.Factory.Headless
{
    /// <summary>
    /// What the checks need to know about component state: items held in hands and machines, and
    /// whether every entity still agrees with the building table.
    /// </summary>
    /// <remarks>
    /// Component data is only reachable from inside a system, so a test that wants to look at it has
    /// to be a system too - one per AND-query. That's three systems for one probe, which is the
    /// one-query-per-system limit showing up in test code.
    /// </remarks>
    public sealed class Probe
    {
        public int HeldByInserters;
        public int[] HeldByInsertersOf { get; } = new int[Items.Count];
        public int HeldByDrills;
        public int CraftsInProgress;
        public int EntitiesSeen;
        public int IntegrityErrors;
        public Dictionary<BuildingKind, int> EntitiesByKind { get; } = [];

        private readonly HashSet<int> seenSlots = [];

        public void Attach(SystemScheduler scheduler, Surface surface)
        {
            scheduler.Add(surface.World, new BuildingPass(this, surface));
            scheduler.Add(surface.World, new InserterPass(this));
            scheduler.Add(surface.World, new DrillPass(this));
            scheduler.Add(surface.World, new CrafterPass(this));
        }

        private void Reset()
        {
            HeldByInserters = 0;
            System.Array.Clear(HeldByInsertersOf);
            HeldByDrills = 0;
            CraftsInProgress = 0;
            EntitiesSeen = 0;
            IntegrityErrors = 0;
            EntitiesByKind.Clear();
            seenSlots.Clear();
        }

        private sealed class BuildingPass(Probe probe, Surface surface) : IUpdateSystem
        {
            public IReadOnlyList<ComponentRef> RequiredComponentProviders => [ComponentRef.Of<Building>()];

            public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
            {
                probe.Reset();
                foreach (var chunk in archetypeChunks)
                {
                    var buildingSpan = chunk.GetSpan<Building>();
                    for (var i = 0; i < chunk.EntityCount; i++)
                    {
                        ref var building = ref buildingSpan[i];
                        probe.EntitiesSeen++;
                        probe.EntitiesByKind[building.Kind] = probe.EntitiesByKind.GetValueOrDefault(building.Kind) + 1;

                        // Entities removed this tick are still iterated until the end-of-tick flush.
                        var expectedAlive = surface.Buildings.IsAlive(building.Slot);
                        if (!expectedAlive || surface.Buildings.KindOf(building.Slot) != building.Kind || !probe.seenSlots.Add(building.Slot))
                        {
                            probe.IntegrityErrors++;
                        }
                    }
                }
            }
        }

        private sealed class InserterPass(Probe probe) : IUpdateSystem
        {
            public IReadOnlyList<ComponentRef> RequiredComponentProviders => [ComponentRef.Of<Inserter>()];

            public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
            {
                foreach (var chunk in archetypeChunks)
                {
                    foreach (ref var inserter in chunk.GetSpan<Inserter>())
                    {
                        if (inserter.Held != ItemId.None)
                        {
                            probe.HeldByInserters++;
                            probe.HeldByInsertersOf[(int)inserter.Held]++;
                        }
                    }
                }
            }
        }

        private sealed class DrillPass(Probe probe) : IUpdateSystem
        {
            public IReadOnlyList<ComponentRef> RequiredComponentProviders => [ComponentRef.Of<Drill>()];

            public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
            {
                foreach (var chunk in archetypeChunks)
                {
                    foreach (ref var drill in chunk.GetSpan<Drill>())
                    {
                        if (drill.Held != ItemId.None)
                        {
                            probe.HeldByDrills++;
                        }
                    }
                }
            }
        }

        private sealed class CrafterPass(Probe probe) : IUpdateSystem
        {
            public IReadOnlyList<ComponentRef> RequiredComponentProviders => [ComponentRef.Of<Crafter>()];

            public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
            {
                foreach (var chunk in archetypeChunks)
                {
                    foreach (ref var crafter in chunk.GetSpan<Crafter>())
                    {
                        if (crafter.IsCrafting)
                        {
                            probe.CraftsInProgress++;
                        }
                    }
                }
            }
        }
    }
}
