using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Simulation.Components;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Power;
using Proto4X.Factory.Simulation.Warehouses;
using System.Collections.Generic;

namespace Proto4X.Factory.Simulation.Systems
{
    /// <summary>
    /// Warehouse exteriors. Abstract (nobody inside): each island of the blueprint runs its rated
    /// cycle independently, a slice at a time - start when the slice's share of every input is in
    /// stock and every output has room for its share, progress by power, deliver. Live (the player
    /// is inside): don't craft at all, the interior is doing the real work; just draw what it drew
    /// last tick.
    /// </summary>
    /// <remarks>
    /// Abstract mode never touches the interior and live mode never runs the cycle, so the two can't
    /// double-count. A slice in progress when the warehouse goes live is frozen, not lost, and resumes
    /// when it goes abstract again.
    /// </remarks>
    public sealed class WarehouseSystem(Surface surface) : IUpdateSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Building>(), ComponentRef.Of<Warehouse>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            var inventories = surface.Inventories;
            var buildings = surface.Buildings;
            var power = surface.Power;
            var powerVersion = power.Version;
            var configVersion = surface.ConfigVersion;
            var satisfaction = power.Satisfaction;
            var demand = power.Demand;
            var universe = surface.Universe;
            var blueprints = universe.Blueprints;

            foreach (var chunk in archetypeChunks)
            {
                var buildingSpan = chunk.GetSpan<Building>();
                var warehouseSpan = chunk.GetSpan<Warehouse>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var warehouse = ref warehouseSpan[i];
                    var slot = buildingSpan[i].Slot;

                    if (warehouse.PowerVersion != powerVersion)
                    {
                        warehouse.PowerIndex = power.IndexOf(slot);
                        warehouse.PowerVersion = powerVersion;
                    }

                    if (warehouse.ConfigVersion != configVersion)
                    {
                        var wanted = buildings.ConfigOf(slot);
                        if (wanted != warehouse.Blueprint)
                        {
                            warehouse.Blueprint = wanted;
                            warehouse.RatingVersion = -1;
                            warehouse.Islands = default;
                        }

                        warehouse.ConfigVersion = configVersion;
                    }

                    if ((uint)warehouse.Blueprint >= (uint)blueprints.Count)
                    {
                        demand[warehouse.PowerIndex] += Buildings.WarehouseIdleDraw;
                        continue;
                    }

                    var rating = blueprints[warehouse.Blueprint].Rating;
                    if (warehouse.RatingVersion != rating.Version || buildings.InventoryStart(slot) < 0)
                    {
                        surface.LayOutWarehouse(slot, rating);
                        warehouse.RatingVersion = rating.Version;
                        warehouse.Islands = default;
                    }

                    var interior = universe.LiveInteriorOf(surface.Index, slot);
                    if (interior is not null)
                    {
                        demand[warehouse.PowerIndex] += Buildings.WarehouseIdleDraw + interior.Power.LastDemand;
                        continue;
                    }

                    var start = buildings.InventoryStart(slot);
                    var outputStart = start + rating.Inputs.Length;
                    var draw = (long)Buildings.WarehouseIdleDraw;
                    var islands = rating.Islands;
                    for (var k = 0; k < islands.Length; k++)
                    {
                        ref var state = ref warehouse.Islands[k];
                        var island = islands[k];
                        if (!state.IsCrafting && !TryStart(ref state, island, inventories, start, outputStart))
                        {
                            continue;
                        }

                        draw += island.ActiveDraw;
                        state.Progress += satisfaction[warehouse.PowerIndex];
                        var slices = island.Slices;
                        if (state.Progress < BlueprintRating.Share(island.CycleTicks, state.Slice, slices) * PowerGrid.Full)
                        {
                            continue;
                        }

                        // Room was checked at start, and only inserters take from outputs.
                        for (var o = 0; o < island.OutputStacks.Length; o++)
                        {
                            var share = BlueprintRating.Share(island.OutputAmounts[o], state.Slice, slices);
                            if (share > 0)
                            {
                                inventories.AddToStack(outputStart + island.OutputStacks[o], share);
                            }
                        }

                        state.IsCrafting = false;
                        state.Progress = 0;
                        state.Slice = (state.Slice + 1) % slices;
                    }

                    demand[warehouse.PowerIndex] += draw;
                }
            }
        }

        /// <summary>Starts an island's next slice: its share of every input in stock, room for its share of every output.</summary>
        private static bool TryStart(ref IslandProgress state, IslandRating island, Stores.InventoryStore inventories, int start, int outputStart)
        {
            var slices = island.Slices;
            var slice = state.Slice;
            for (var k = 0; k < island.InputStacks.Length; k++)
            {
                if (inventories.CountAt(start + island.InputStacks[k]) < BlueprintRating.Share(island.InputAmounts[k], slice, slices))
                {
                    return false;
                }
            }

            for (var o = 0; o < island.OutputStacks.Length; o++)
            {
                var stack = outputStart + island.OutputStacks[o];
                var share = BlueprintRating.Share(island.OutputAmounts[o], slice, slices);
                if (share > 0 && !inventories.StackAccepts(stack, inventories.ItemAt(stack), share))
                {
                    return false;
                }
            }

            for (var k = 0; k < island.InputStacks.Length; k++)
            {
                var share = BlueprintRating.Share(island.InputAmounts[k], slice, slices);
                if (share > 0)
                {
                    inventories.RemoveFromStack(start + island.InputStacks[k], share);
                }
            }

            state.IsCrafting = true;
            state.Progress = 0;
            return true;
        }
    }
}
