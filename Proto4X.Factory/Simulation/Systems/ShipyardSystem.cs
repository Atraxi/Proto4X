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
    /// <summary>Production mode of a shipyard order: off, one ship, or keep building (mass production).</summary>
    public enum ShipyardMode : byte
    {
        Off,
        Once,
        Repeat,
    }

    /// <summary>A finished ship, launched into space by the universe between ticks.</summary>
    public readonly record struct ShipLaunch(int Design, int FromSurface, int X, int Y);

    /// <summary>Packs and unpacks a shipyard order into the building table's config int.</summary>
    public static class ShipyardOrder
    {
        public static int Pack(int design, ShipyardMode mode) => (design & 0xFF) | ((int)mode << 8);
        public static int DesignOf(int config) => config & 0xFF;
        public static ShipyardMode ModeOf(int config) => (ShipyardMode)((config >> 8) & 0xFF);
    }

    /// <summary>
    /// Shipyards: when the ordered design's whole bill is in stock, consume it, build for the design's
    /// build time (scaled by power), then launch. Once reverts to Off; Repeat goes again.
    /// </summary>
    public sealed class ShipyardSystem(Surface surface) : IUpdateSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Building>(), ComponentRef.Of<Shipyard>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            var inventories = surface.Inventories;
            var buildings = surface.Buildings;
            var power = surface.Power;
            var powerVersion = power.Version;
            var satisfaction = power.Satisfaction;
            var demand = power.Demand;
            var designs = surface.Universe.Designs;
            var spec = Buildings.Spec(BuildingKind.Shipyard);

            foreach (var chunk in archetypeChunks)
            {
                var buildingSpan = chunk.GetSpan<Building>();
                var shipyardSpan = chunk.GetSpan<Shipyard>();
                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var yard = ref shipyardSpan[i];
                    var slot = buildingSpan[i].Slot;
                    if (yard.PowerVersion != powerVersion)
                    {
                        yard.PowerIndex = power.IndexOf(slot);
                        yard.PowerVersion = powerVersion;
                    }

                    if (!yard.IsBuilding && !TryStart(ref yard, slot, buildings, inventories, designs))
                    {
                        demand[yard.PowerIndex] += spec.IdleDraw;
                        continue;
                    }

                    demand[yard.PowerIndex] += spec.ActiveDraw;
                    yard.Progress += satisfaction[yard.PowerIndex];
                    if (yard.Progress < yard.BuildTicks * PowerGrid.Full)
                    {
                        continue;
                    }

                    surface.ShipsLaunched.Add(new ShipLaunch(yard.Design, surface.Index, buildings.OriginX(slot), buildings.OriginY(slot)));
                    yard.IsBuilding = false;
                    yard.Progress = 0;
                }
            }
        }

        private static bool TryStart(ref Shipyard yard, int slot, Stores.BuildingTable buildings, Stores.InventoryStore inventories, Ships.ShipDesign?[] designs)
        {
            var order = buildings.ConfigOf(slot);
            var mode = ShipyardOrder.ModeOf(order);
            var designIndex = ShipyardOrder.DesignOf(order);
            if (mode == ShipyardMode.Off || designIndex >= designs.Length || designs[designIndex] is not { Stats.IsValid: true } design)
            {
                return false;
            }

            var stacks = Buildings.ShipyardStacks;
            foreach (var (item, count) in design.Stats.Cost)
            {
                var index = System.Array.IndexOf(stacks, item);
                if (index < 0 || inventories.CountAt(yard.Inventory + index) < count)
                {
                    return false;
                }
            }

            foreach (var (item, count) in design.Stats.Cost)
            {
                inventories.RemoveFromStack(yard.Inventory + System.Array.IndexOf(stacks, item), count);
            }

            if (mode == ShipyardMode.Once)
            {
                // The order is the yard's own store entry; consuming it is this system's job.
                buildings.SetConfig(slot, ShipyardOrder.Pack(designIndex, ShipyardMode.Off));
            }

            yard.IsBuilding = true;
            yard.Design = designIndex;
            yard.BuildTicks = design.Stats.BuildTicks;
            yard.Progress = 0;
            return true;
        }
    }
}
