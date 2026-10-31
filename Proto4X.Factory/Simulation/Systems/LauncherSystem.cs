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
    /// Launch silos: once a full payload is loaded, count down and send it to the other surface.
    /// </summary>
    /// <remarks>
    /// The shipment is appended to the surface's outbound list, not handed to the destination - this
    /// system must not reach another surface's stores mid-tick. The universe moves it between ticks,
    /// the same describe-don't-do pattern as the sandbox's spawn queue, one level up.
    /// </remarks>
    public sealed class LauncherSystem(Surface surface) : IUpdateSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Building>(), ComponentRef.Of<Launcher>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            var inventories = surface.Inventories;
            var power = surface.Power;
            var powerVersion = power.Version;
            var satisfaction = power.Satisfaction;
            var demand = power.Demand;
            var spec = Buildings.Spec(BuildingKind.LaunchSilo);

            foreach (var chunk in archetypeChunks)
            {
                var buildingSpan = chunk.GetSpan<Building>();
                var launcherSpan = chunk.GetSpan<Launcher>();

                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var launcher = ref launcherSpan[i];
                    if (launcher.PowerVersion != powerVersion)
                    {
                        launcher.PowerIndex = power.IndexOf(buildingSpan[i].Slot);
                        launcher.PowerVersion = powerVersion;
                    }

                    if (!launcher.IsLaunching)
                    {
                        demand[launcher.PowerIndex] += spec.IdleDraw;
                        if (inventories.TotalItems(launcher.Inventory, launcher.Stacks) >= Buildings.LaunchPayload)
                        {
                            launcher.IsLaunching = true;
                            launcher.Progress = 0;
                        }

                        continue;
                    }

                    demand[launcher.PowerIndex] += spec.ActiveDraw;
                    launcher.Progress += satisfaction[launcher.PowerIndex];
                    if (launcher.Progress < Buildings.LaunchTicks * PowerGrid.Full)
                    {
                        continue;
                    }

                    launcher.IsLaunching = false;
                    launcher.Progress = 0;
                    surface.Outbound.Add(PackPayload(ref launcher, inventories));
                }
            }
        }

        private Shipment PackPayload(ref Launcher launcher, Stores.InventoryStore inventories)
        {
            var cargo = new List<(ItemId, int)>();
            var remaining = Buildings.LaunchPayload;
            for (var stack = launcher.Inventory; stack < launcher.Inventory + launcher.Stacks && remaining > 0; stack++)
            {
                var count = inventories.CountAt(stack);
                if (count == 0)
                {
                    continue;
                }

                var taken = count < remaining ? count : remaining;
                cargo.Add((inventories.ItemAt(stack), taken));
                inventories.RemoveFromStack(stack, taken);
                remaining -= taken;
            }

            var universe = surface.Universe;
            return new Shipment(surface.Index, universe.DestinationFor(surface), universe.CurrentTick + Universe.ShipmentTransitTicks, [.. cargo]);
        }
    }
}
