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

        /// <summary>Inputs consumed by warehouse slices that haven't finished: held, not lost.</summary>
        public int[] HeldByWarehousesOf { get; } = new int[Items.Count];
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
            scheduler.Add(surface.World, new WarehousePass(this, surface));
        }

        private void Reset()
        {
            HeldByInserters = 0;
            System.Array.Clear(HeldByInsertersOf);
            HeldByDrills = 0;
            CraftsInProgress = 0;
            System.Array.Clear(HeldByWarehousesOf);
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

        private sealed class WarehousePass(Probe probe, Surface surface) : IUpdateSystem
        {
            public IReadOnlyList<ComponentRef> RequiredComponentProviders => [ComponentRef.Of<Warehouse>()];

            public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
            {
                var blueprints = surface.Universe.Blueprints;
                foreach (var chunk in archetypeChunks)
                {
                    foreach (ref var warehouse in chunk.GetSpan<Warehouse>())
                    {
                        if ((uint)warehouse.Blueprint >= (uint)blueprints.Count)
                        {
                            continue;
                        }

                        var rating = blueprints[warehouse.Blueprint].Rating;
                        for (var k = 0; k < rating.Islands.Length; k++)
                        {
                            var island = rating.Islands[k];
                            var state = warehouse.Islands[k];
                            for (var n = 0; state.IsCrafting && n < island.InputStacks.Length; n++)
                            {
                                probe.HeldByWarehousesOf[(int)rating.Inputs[island.InputStacks[n]].Item] += Simulation.Warehouses.BlueprintRating.Share(island.InputAmounts[n], state.Slice, island.Slices);
                            }
                        }
                    }
                }
            }
        }
    }
}
