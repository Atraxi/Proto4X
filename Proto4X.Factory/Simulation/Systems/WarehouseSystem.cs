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
    /// Warehouse exteriors. Abstract (nobody inside): run the blueprint's rated cycle - start when
    /// every input holds a cycle's worth and every output has room, progress by power, deliver - the
    /// crafter's rules with a composite recipe. Live (the player is inside): don't craft at all, the
    /// interior is doing the real work; just draw what it drew last tick.
    /// </summary>
    /// <remarks>
    /// Abstract mode never touches the interior and live mode never runs the cycle, so the two can't
    /// double-count. A cycle in progress when the warehouse goes live is frozen, not lost, and resumes
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
                            warehouse.IsCrafting = false;
                            warehouse.Progress = 0;
                            warehouse.Slice = 0;
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
                        warehouse.IsCrafting = false;
                        warehouse.Progress = 0;
                        warehouse.Slice = 0;
                    }

                    var interior = universe.LiveInteriorOf(surface.Index, slot);
                    if (interior is not null)
                    {
                        demand[warehouse.PowerIndex] += Buildings.WarehouseIdleDraw + interior.Power.LastDemand;
                        continue;
                    }

                    var start = buildings.InventoryStart(slot);
                    if (!warehouse.IsCrafting && !TryStart(ref warehouse, rating, inventories, start))
                    {
                        demand[warehouse.PowerIndex] += Buildings.WarehouseIdleDraw;
                        continue;
                    }

                    demand[warehouse.PowerIndex] += Buildings.WarehouseIdleDraw + rating.ActiveDraw;
                    warehouse.Progress += satisfaction[warehouse.PowerIndex];
                    var slices = rating.Slices;
                    if (warehouse.Progress >= BlueprintRating.Share(rating.CycleTicks, warehouse.Slice, slices) * PowerGrid.Full)
                    {
                        // Room was checked at start, and only inserters take from outputs.
                        var outputStart = start + rating.Inputs.Length;
                        for (var k = 0; k < rating.Outputs.Length; k++)
                        {
                            inventories.AddToStack(outputStart + k, BlueprintRating.Share(rating.Outputs[k].Amount, warehouse.Slice, slices));
                        }

                        warehouse.IsCrafting = false;
                        warehouse.Progress = 0;
                        warehouse.Slice = (warehouse.Slice + 1) % slices;
                    }
                }
            }
        }

        /// <summary>Starts the next slice of the cycle: its share of every input in stock, room for its share of every output.</summary>
        private static bool TryStart(ref Warehouse warehouse, BlueprintRating rating, Stores.InventoryStore inventories, int start)
        {
            if (!rating.CanRun)
            {
                return false;
            }

            var slices = rating.Slices;
            var slice = warehouse.Slice;
            var inputs = rating.Inputs;
            for (var k = 0; k < inputs.Length; k++)
            {
                if (inventories.CountAt(start + k) < BlueprintRating.Share(inputs[k].Amount, slice, slices))
                {
                    return false;
                }
            }

            var outputStart = start + inputs.Length;
            for (var k = 0; k < rating.Outputs.Length; k++)
            {
                var share = BlueprintRating.Share(rating.Outputs[k].Amount, slice, slices);
                if (share > 0 && !inventories.StackAccepts(outputStart + k, rating.Outputs[k].Item, share))
                {
                    return false;
                }
            }

            for (var k = 0; k < inputs.Length; k++)
            {
                var share = BlueprintRating.Share(inputs[k].Amount, slice, slices);
                if (share > 0)
                {
                    inventories.RemoveFromStack(start + k, share);
                }
            }

            warehouse.IsCrafting = true;
            warehouse.Progress = 0;
            return true;
        }
    }
}
