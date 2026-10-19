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
    /// The inserter state machine: wait for something the drop side wants, swing, wait for room,
    /// swing back.
    /// </summary>
    /// <remarks>
    /// The two waiting phases poll every tick. A starved inserter re-scans its source each tick
    /// looking for something it can move, and a blocked one re-tries its target. The original game
    /// avoids exactly this cost by putting entities to sleep and waking them on inventory changes;
    /// doing that here needs either a per-entity filter (ENGINE_REQUESTS.md, "Filtering") or a
    /// change notification on the stores. The headless benchmark's idle variant measures what
    /// polling costs.
    /// </remarks>
    public sealed class InserterSystem(Surface surface) : IUpdateSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders =
        [
            ComponentRef.Of<Building>(), ComponentRef.Of<Placement>(), ComponentRef.Of<Inserter>(),
        ];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            var grid = surface.Grid;
            var buildings = surface.Buildings;
            var power = surface.Power;
            var powerVersion = power.Version;
            var satisfaction = power.Satisfaction;
            var demand = power.Demand;
            var spec = Buildings.Spec(BuildingKind.Inserter);
            const int swingUnits = Buildings.InserterSwingTicks * PowerGrid.Full;

            foreach (var chunk in archetypeChunks)
            {
                var buildingSpan = chunk.GetSpan<Building>();
                var placementSpan = chunk.GetSpan<Placement>();
                var inserterSpan = chunk.GetSpan<Inserter>();

                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var inserter = ref inserterSpan[i];

                    var chunkVersion = grid.ChunkVersion(inserter.Chunk);
                    if (inserter.WiringVersion != chunkVersion)
                    {
                        inserter.PickupSlot = Exchange.ResolveAt(surface, inserter.PickupX, inserter.PickupY);
                        inserter.DropSlot = Exchange.ResolveAt(surface, inserter.DropX, inserter.DropY);
                        if (inserter.PickupSlot >= 0 && !Exchange.HasOutputs(buildings.KindOf(inserter.PickupSlot)))
                        {
                            inserter.PickupSlot = -1;
                        }

                        if (inserter.DropSlot >= 0 && !Exchange.HasInputs(buildings.KindOf(inserter.DropSlot)))
                        {
                            inserter.DropSlot = -1;
                        }

                        inserter.WiringVersion = chunkVersion;
                    }

                    if (inserter.PowerVersion != powerVersion)
                    {
                        inserter.PowerIndex = power.IndexOf(buildingSpan[i].Slot);
                        inserter.PowerVersion = powerVersion;
                    }

                    var powered = satisfaction[inserter.PowerIndex];

                    switch (inserter.Phase)
                    {
                        case InserterPhase.WaitingForPickup:
                            demand[inserter.PowerIndex] += spec.IdleDraw;
                            if (powered > 0 && inserter.PickupSlot >= 0 && inserter.DropSlot >= 0
                                && Exchange.TryTakeFor(surface, inserter.PickupSlot, inserter.DropSlot, out var taken))
                            {
                                inserter.Held = taken;
                                inserter.Phase = InserterPhase.SwingingToDrop;
                                inserter.Progress = 0;
                            }

                            break;

                        case InserterPhase.SwingingToDrop:
                            demand[inserter.PowerIndex] += spec.ActiveDraw;
                            inserter.Progress += powered;
                            if (inserter.Progress >= swingUnits)
                            {
                                inserter.Phase = InserterPhase.WaitingToDrop;
                            }

                            break;

                        case InserterPhase.WaitingToDrop:
                            demand[inserter.PowerIndex] += spec.IdleDraw;
                            if (inserter.DropSlot >= 0
                                && Exchange.TryDeliver(surface, inserter.DropSlot, inserter.Held, placementSpan[i].Direction))
                            {
                                inserter.Held = ItemId.None;
                                inserter.Phase = InserterPhase.SwingingBack;
                                inserter.Progress = 0;
                            }

                            break;

                        case InserterPhase.SwingingBack:
                            demand[inserter.PowerIndex] += spec.ActiveDraw;
                            inserter.Progress += powered;
                            if (inserter.Progress >= swingUnits)
                            {
                                inserter.Phase = InserterPhase.WaitingForPickup;
                                inserter.Progress = 0;
                            }

                            break;
                    }
                }
            }
        }
    }
}
