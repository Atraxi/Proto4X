using Microsoft.Xna.Framework;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Diagnostics;
using Proto4X.Factory.Simulation.Grid;
using System;
using System.Collections.Generic;

namespace Proto4X.Factory.Simulation
{
    public enum CommandKind : byte
    {
        Place,
        Remove,
        SetRecipe,
    }

    /// <summary>
    /// A player action. Commands are the only way input reaches the simulation, which is the
    /// lockstep shape: two machines fed the same command stream on the same ticks stay identical.
    /// </summary>
    public readonly record struct Command(CommandKind Kind, int Surface, int X, int Y, BuildingKind Building = BuildingKind.None,
        Direction Direction = Direction.North, RecipeId Recipe = RecipeId.None);

    /// <summary>
    /// Every surface, the fixed-rate tick, the command queue and interplanetary shipping.
    /// </summary>
    /// <remarks>
    /// The engine has no fixed-tick concept: <c>Core.Update</c> runs the scheduler with whatever
    /// <see cref="GameTime"/> MonoGame supplies. The simulation instead counts integer ticks and hands
    /// every system the same synthetic one-sixtieth-of-a-second <see cref="GameTime"/>, so no system
    /// ever reads a variable delta. MonoGame's own fixed timestep (on by default) makes one
    /// <c>Update</c> one tick in the game; the headless runner just calls <see cref="Step"/> in a loop.
    /// </remarks>
    public sealed class Universe
    {
        public const int TicksPerSecond = 60;
        public const int ShipmentTransitTicks = 180;

        private static readonly GameTime tickTime = new(TimeSpan.Zero, TimeSpan.FromTicks(TimeSpan.TicksPerSecond / TicksPerSecond));

        private readonly SystemScheduler scheduler;
        private readonly bool includeStateHashing;
        private readonly Func<IUpdateSystem, IUpdateSystem>? decorate;
        private readonly List<Surface> surfaces = [];
        private readonly Queue<Command> commands = new();
        private readonly List<Shipment> inTransit = [];

        public long Tick { get; private set; }
        public IReadOnlyList<Surface> Surfaces => surfaces;
        public IReadOnlyList<Shipment> InTransit => inTransit;

        /// <summary>Results of the last tick's commands, in order, for UI feedback.</summary>
        public List<(Command Command, PlacementResult Result)> LastResults { get; } = [];

        /// <param name="decorate">Optional wrapper applied to every system as it is registered - the benchmark uses it for per-system timing.</param>
        public Universe(SystemScheduler scheduler, bool includeStateHashing = false, Func<IUpdateSystem, IUpdateSystem>? decorate = null)
        {
            this.decorate = decorate;
            this.scheduler = scheduler;
            this.includeStateHashing = includeStateHashing;
        }

        public Surface AddSurface(string name, SurfaceKind kind, int width, int height)
        {
            var surface = new Surface(this, surfaces.Count, name, kind, width, height);
            surfaces.Add(surface);
            surface.RegisterSystems(scheduler, includeStateHashing, decorate);
            return surface;
        }

        public void Enqueue(in Command command) => commands.Enqueue(command);

        /// <summary>Runs one simulation tick across every surface.</summary>
        public void Step()
        {
            ApplyCommands();

            foreach (var surface in surfaces)
            {
                surface.BeforeTick();
            }

            foreach (var surface in surfaces)
            {
                scheduler.Update(surface.World, tickTime);
            }

            foreach (var surface in surfaces)
            {
                surface.AfterTick();
                foreach (var shipment in surface.Outbound)
                {
                    inTransit.Add(shipment);
                }

                surface.Outbound.Clear();
                surface.Frame.Integrate();
            }

            DeliverShipments();
            Tick++;
        }

        private void ApplyCommands()
        {
            LastResults.Clear();
            while (commands.Count > 0)
            {
                var command = commands.Dequeue();
                if ((uint)command.Surface >= (uint)surfaces.Count)
                {
                    continue;
                }

                var surface = surfaces[command.Surface];
                var result = command.Kind switch
                {
                    CommandKind.Place => surface.TryPlace(command.Building, command.X, command.Y, command.Direction, command.Recipe),
                    CommandKind.Remove => surface.TryRemoveAt(command.X, command.Y) ? PlacementResult.Placed : PlacementResult.Occupied,
                    _ => surface.TrySetRecipeAt(command.X, command.Y, command.Recipe) ? PlacementResult.Placed : PlacementResult.RecipeNotAllowedHere,
                };
                LastResults.Add((command, result));
            }
        }

        /// <summary>The surface a launch from <paramref name="from"/> goes to: the first surface of the other kind.</summary>
        internal int DestinationFor(Surface from)
        {
            foreach (var surface in surfaces)
            {
                if (surface.Kind != from.Kind)
                {
                    return surface.Index;
                }
            }

            return -1;
        }

        internal long CurrentTick => Tick;

        /// <summary>
        /// Lands arrived cargo in the lowest-slot receiver with room for all of it. Cargo that can't
        /// land waits in orbit, which is the backpressure that keeps a silo from launching forever.
        /// </summary>
        private void DeliverShipments()
        {
            for (var i = 0; i < inTransit.Count; i++)
            {
                var shipment = inTransit[i];
                if (shipment.ArrivalTick > Tick || shipment.ToSurface < 0)
                {
                    continue;
                }

                if (TryLand(surfaces[shipment.ToSurface], shipment))
                {
                    inTransit.RemoveAt(i);
                    i--;
                }
            }
        }

        private static bool TryLand(Surface surface, Shipment shipment)
        {
            var buildings = surface.Buildings;
            var inventories = surface.Inventories;
            for (var slot = 0; slot < buildings.HighWater; slot++)
            {
                if (buildings.KindOf(slot) != BuildingKind.Receiver || !buildings.IsUsable(slot))
                {
                    continue;
                }

                var start = buildings.InventoryStart(slot);
                var length = buildings.InventoryLength(slot);
                if (!HasRoomFor(surface, start, length, shipment))
                {
                    continue;
                }

                foreach (var (item, count) in shipment.Cargo)
                {
                    for (var n = 0; n < count; n++)
                    {
                        inventories.TryInsert(start, length, item, 1);
                    }
                }

                return true;
            }

            return false;
        }

        private static bool HasRoomFor(Surface surface, int start, int length, Shipment shipment)
        {
            // Conservative: count free stack space per item type plus wholly empty stacks.
            var inventories = surface.Inventories;
            var emptyStacks = 0;
            for (var stack = start; stack < start + length; stack++)
            {
                if (inventories.ItemAt(stack) == ItemId.None)
                {
                    emptyStacks++;
                }
            }

            foreach (var (item, count) in shipment.Cargo)
            {
                var room = 0;
                for (var stack = start; stack < start + length; stack++)
                {
                    if (inventories.ItemAt(stack) == item)
                    {
                        room += inventories.LimitAt(stack) - inventories.CountAt(stack);
                    }
                }

                var shortfall = count - room;
                while (shortfall > 0 && emptyStacks > 0)
                {
                    shortfall -= Items.StackSize(item);
                    emptyStacks--;
                }

                if (shortfall > 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Hash of every surface plus shipments in flight. Requires state hashing enabled at construction.</summary>
        public ulong ComputeStateHash()
        {
            if (!includeStateHashing)
            {
                throw new InvalidOperationException("Construct the universe with includeStateHashing to hash component state.");
            }

            // Component state is only reachable from inside a system, so hashing it takes a tick.
            foreach (var surface in surfaces)
            {
                surface.HashRequested = true;
                surface.ComponentHash = StateHash.Seed;
            }

            Step();

            var hash = StateHash.Seed;
            foreach (var surface in surfaces)
            {
                surface.HashRequested = false;
                hash = StateHash.Mix(hash, surface.StateHashOf());
            }

            foreach (var shipment in inTransit)
            {
                hash = StateHash.Mix(hash, (ulong)shipment.ArrivalTick);
                foreach (var (item, count) in shipment.Cargo)
                {
                    hash = StateHash.Mix(hash, ((ulong)item << 32) | (uint)count);
                }
            }

            return StateHash.Mix(hash, (ulong)Tick);
        }
    }
}
