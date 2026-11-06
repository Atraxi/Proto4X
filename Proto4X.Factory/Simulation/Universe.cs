using Microsoft.Xna.Framework;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Diagnostics;
using Proto4X.Factory.Simulation.Grid;
using Proto4X.Factory.Simulation.Ships;
using Proto4X.Factory.Simulation.Space;
using Proto4X.Factory.Simulation.Systems;
using Proto4X.Factory.Simulation.Warehouses;
using System;
using System.Collections.Generic;

namespace Proto4X.Factory.Simulation
{
    public enum CommandKind : byte
    {
        Place,
        Remove,
        SetRecipe,
        /// <summary>Dock filter, warehouse blueprint, shipyard order: <see cref="Command.Config"/>.</summary>
        SetConfig,
        /// <summary>Make the warehouse at (X, Y) live: its interior ticks and is shown (plans/03).</summary>
        EnterInterior,
        ExitInterior,
        /// <summary>Payload: <c>int[]</c> ship serials; (X, Y) is the target in space tiles.</summary>
        MoveShips,
        /// <summary>Payload: a <see cref="ShipDesign"/>; <see cref="Command.Config"/> is the slot.</summary>
        SaveDesign,
        /// <summary>A new blueprint for the warehouse at (X, Y): a copy of blueprint <see cref="Command.Config"/>, or empty if that's -1.</summary>
        NewBlueprint,
        /// <summary>Peaceful mode on (<see cref="Command.Config"/> 1) or off: holds the wave timer.</summary>
        SetPeaceful,
    }

    /// <summary>
    /// A player action. Commands are the only way input reaches the simulation, which is the
    /// lockstep shape: two machines fed the same command stream on the same ticks stay identical.
    /// </summary>
    /// <param name="Payload">Immutable data too big for the fixed fields (a design, a ship list).</param>
    public readonly record struct Command(CommandKind Kind, int Surface, int X, int Y, BuildingKind Building = BuildingKind.None,
        Direction Direction = Direction.North, RecipeId Recipe = RecipeId.None, int Config = 0, object? Payload = null);

    /// <summary>
    /// Every surface, the fixed-rate tick, the command queue, interplanetary shipping, warehouse
    /// interiors and blueprints, ship designs, and the space layer.
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
        public const int DesignSlots = 8;

        /// <summary>A live interior is re-rated this long after its last edit, so drag-building doesn't re-rate per tile.</summary>
        public const int RerateDelayTicks = 60;

        private static readonly GameTime tickTime = new(TimeSpan.Zero, TimeSpan.FromTicks(TimeSpan.TicksPerSecond / TicksPerSecond));

        private readonly SystemScheduler scheduler;
        private readonly bool includeStateHashing;
        private readonly Func<IUpdateSystem, IUpdateSystem>? decorate;
        private readonly List<Surface> surfaces = [];
        private readonly Queue<Command> commands = new();
        private readonly List<Shipment> inTransit = [];
        private readonly Dictionary<(int Surface, int Slot), Surface> interiors = [];
        private readonly Stack<Surface> spareInteriors = new();
        private (int Surface, int Slot, Surface Interior)? live;
        private long lastLiveEditTick = -1;
        private int launchedShips;

        public long Tick { get; private set; }
        public IReadOnlyList<Surface> Surfaces => surfaces;
        public IReadOnlyList<Shipment> InTransit => inTransit;

        /// <summary>Results of the last tick's commands, in order, for UI feedback.</summary>
        public List<(Command Command, PlacementResult Result)> LastResults { get; } = [];

        public List<Blueprint> Blueprints { get; } = [];

        /// <summary>The player's design slots. Shipyards build by slot.</summary>
        public ShipDesign?[] Designs { get; } = new ShipDesign?[DesignSlots];

        public SpaceLayer Space { get; } = new();

        /// <summary>Bills of the station's current shipyard orders kept in stock ahead: two ships' worth.</summary>
        public const int RequestedBills = 2;

        /// <summary>
        /// What the station still wants, per item (Factorio Space Age platform requests): its shipyards'
        /// ordered bills times <see cref="RequestedBills"/>, less what's already in the yards, in its
        /// receivers, in flight, and waiting in silos. Recomputed between ticks; silos only take wanted
        /// items, and taking one claims it.
        /// </summary>
        public int[] Requests { get; } = new int[Items.Count];

        /// <summary>The surface ships launch beside and payloads land on: the first orbit surface.</summary>
        public Surface? Station { get; private set; }

        /// <summary>Raised when a surface is created, including interiors created on first entry, so the game can attach renderers.</summary>
        public event Action<Surface>? SurfaceAdded;

        /// <summary>The live warehouse and its interior, if the player is inside one.</summary>
        public (int Surface, int Slot, Surface Interior)? Live => live;

        /// <param name="decorate">Optional wrapper applied to every system as it is registered - the benchmark uses it for per-system timing.</param>
        public Universe(SystemScheduler scheduler, bool includeStateHashing = false, Func<IUpdateSystem, IUpdateSystem>? decorate = null)
        {
            this.decorate = decorate;
            this.scheduler = scheduler;
            this.includeStateHashing = includeStateHashing;
            Space.RegisterSystems(scheduler);
            Designs[0] = ShipPresets.Picket();
            Designs[1] = ShipPresets.Lancer();
            Designs[2] = ShipPresets.Bulwark();
        }

        public Surface AddSurface(string name, SurfaceKind kind, int width, int height)
        {
            var surface = new Surface(this, surfaces.Count, name, kind, width, height);
            surfaces.Add(surface);
            surface.RegisterSystems(scheduler, includeStateHashing, decorate);
            if (kind == SurfaceKind.Orbit && Station is null)
            {
                Station = surface;
            }

            SurfaceAdded?.Invoke(surface);
            return surface;
        }

        public Blueprint AddBlueprint(string name, IEnumerable<BlueprintEntry> entries)
        {
            var blueprint = new Blueprint(Blueprints.Count, name);
            blueprint.SetEntries(entries);
            Blueprints.Add(blueprint);
            return blueprint;
        }

        /// <summary>Puts the station's body and the raider bastion into space. Setup only.</summary>
        public void SetUpSpace(Vector2 bastionPosition)
        {
            if (Station is not { } station)
            {
                throw new InvalidOperationException("Space needs an orbit surface for the station.");
            }

            var centre = station.Frame.LocalToWorld(new Vector2(station.Grid.Width / 2f, station.Grid.Height / 2f));
            Space.AddStation(centre, hp: 4_000f, dps: 50f, range: 22f);
            Space.AddBastion(bastionPosition, hp: 8_000f, dps: 100f, range: 26f);
        }

        public void Enqueue(in Command command) => commands.Enqueue(command);

        /// <summary>The interior of the warehouse at (surface, slot) if it is the live one, else null.</summary>
        public Surface? LiveInteriorOf(int surface, int slot) =>
            live is { } l && l.Surface == surface && l.Slot == slot ? l.Interior : null;

        /// <summary>The warehouse whose interior <paramref name="interior"/> is, if it's live.</summary>
        public bool IsLiveInterior(Surface interior) => live is { } l && l.Interior == interior;

        /// <summary>Runs one simulation tick across every surface.</summary>
        public void Step()
        {
            Space.ClearTickEvents();
            ApplyCommands();
            RerateLiveIfSettled();
            SyncLiveInterior();
            UpdateRequests();

            foreach (var surface in surfaces)
            {
                if (surface.IsActive)
                {
                    surface.BeforeTick();
                }
            }

            foreach (var surface in surfaces)
            {
                if (surface.IsActive)
                {
                    scheduler.Update(surface.World, tickTime);
                }
            }

            scheduler.Update(Space.World, tickTime);

            foreach (var surface in surfaces)
            {
                if (!surface.IsActive)
                {
                    continue;
                }

                surface.AfterTick();
                foreach (var shipment in surface.Outbound)
                {
                    inTransit.Add(shipment);
                }

                surface.Outbound.Clear();
                foreach (var launch in surface.ShipsLaunched)
                {
                    LaunchShip(surface, launch);
                }

                surface.ShipsLaunched.Clear();
                surface.Frame.Integrate();
            }

            Space.AfterTick(Tick);
            DeliverShipments();
            Tick++;
        }

        private void ApplyCommands()
        {
            LastResults.Clear();
            while (commands.Count > 0)
            {
                var command = commands.Dequeue();
                if ((uint)command.Surface >= (uint)surfaces.Count && command.Kind is not (CommandKind.MoveShips or CommandKind.SaveDesign or CommandKind.ExitInterior or CommandKind.SetPeaceful))
                {
                    continue;
                }

                var result = command.Kind switch
                {
                    CommandKind.Place => surfaces[command.Surface].TryPlace(command.Building, command.X, command.Y, command.Direction, command.Recipe, command.Config),
                    CommandKind.Remove => Remove(command) ? PlacementResult.Placed : PlacementResult.Occupied,
                    CommandKind.SetRecipe => surfaces[command.Surface].TrySetRecipeAt(command.X, command.Y, command.Recipe) ? PlacementResult.Placed : PlacementResult.RecipeNotAllowedHere,
                    CommandKind.SetConfig => surfaces[command.Surface].TrySetConfigAt(command.X, command.Y, command.Config) ? PlacementResult.Placed : PlacementResult.WrongSurface,
                    CommandKind.EnterInterior => EnterInterior(command.Surface, command.X, command.Y) ? PlacementResult.Placed : PlacementResult.WrongSurface,
                    CommandKind.ExitInterior => ExitInterior() ? PlacementResult.Placed : PlacementResult.WrongSurface,
                    CommandKind.MoveShips => OrderShips(command),
                    CommandKind.NewBlueprint => NewBlueprint(command),
                    CommandKind.SetPeaceful => SetPeaceful(command.Config != 0),
                    _ => SaveDesign(command),
                };

                if (result == PlacementResult.Placed && command.Kind is CommandKind.Place or CommandKind.Remove or CommandKind.SetConfig or CommandKind.SetRecipe
                    && live is { } l && l.Interior.Index == command.Surface)
                {
                    lastLiveEditTick = Tick;
                }

                LastResults.Add((command, result));
            }
        }

        private bool Remove(in Command command)
        {
            var surface = surfaces[command.Surface];
            var slot = surface.Grid.OccupantAt(command.X, command.Y);
            if (slot >= 0 && surface.Buildings.KindOf(slot) == BuildingKind.Warehouse)
            {
                if (live is { } l && l.Surface == command.Surface && l.Slot == slot)
                {
                    ExitInterior();
                }

                if (interiors.Remove((command.Surface, slot), out var interior))
                {
                    interior.BlueprintId = -1; // contents are stale; rebuilt when reused
                    spareInteriors.Push(interior);
                }
            }

            return surface.TryRemoveAt(command.X, command.Y);
        }

        private PlacementResult OrderShips(in Command command)
        {
            if (command.Payload is not int[] serials || serials.Length == 0)
            {
                return PlacementResult.WrongSurface;
            }

            Space.Order(serials, new Vector2(command.X + 0.5f, command.Y + 0.5f));
            return PlacementResult.Placed;
        }

        private PlacementResult NewBlueprint(in Command command)
        {
            var surface = surfaces[command.Surface];
            var slot = surface.Grid.OccupantAt(command.X, command.Y);
            if (slot < 0 || surface.Buildings.KindOf(slot) != BuildingKind.Warehouse)
            {
                return PlacementResult.WrongSurface;
            }

            // Leave first: exiting saves the live floor into the current blueprint, so the copy includes it.
            if (live is { } l && l.Surface == command.Surface && l.Slot == slot)
            {
                ExitInterior();
            }

            var source = (uint)command.Config < (uint)Blueprints.Count ? Blueprints[command.Config] : null;
            var blueprint = AddBlueprint(source is null ? $"Blueprint {Blueprints.Count + 1}" : $"{source.Name} ({Blueprints.Count + 1})", source?.Entries ?? []);
            return surface.TrySetConfigAt(command.X, command.Y, blueprint.Id) ? PlacementResult.Placed : PlacementResult.WrongSurface;
        }

        private PlacementResult SetPeaceful(bool peaceful)
        {
            Space.Peaceful = peaceful;
            return PlacementResult.Placed;
        }

        private PlacementResult SaveDesign(in Command command)
        {
            if (command.Payload is not ShipDesign design || (uint)command.Config >= DesignSlots)
            {
                return PlacementResult.WrongSurface;
            }

            Designs[command.Config] = design;
            return PlacementResult.Placed;
        }

        // ---------------------------------------------------------------- warehouse interiors

        private bool EnterInterior(int surfaceIndex, int x, int y)
        {
            var surface = surfaces[surfaceIndex];
            var slot = surface.Grid.OccupantAt(x, y);
            if (slot < 0 || !surface.Buildings.IsUsable(slot) || surface.Buildings.KindOf(slot) != BuildingKind.Warehouse)
            {
                return false;
            }

            var blueprintId = surface.Buildings.ConfigOf(slot);
            if ((uint)blueprintId >= (uint)Blueprints.Count)
            {
                return false;
            }

            if (live is { } current)
            {
                if (current.Surface == surfaceIndex && current.Slot == slot)
                {
                    return true;
                }

                ExitInterior();
            }

            if (!interiors.TryGetValue((surfaceIndex, slot), out var interior))
            {
                interior = spareInteriors.Count > 0 ? spareInteriors.Pop() : CreateInterior();
                interiors[(surfaceIndex, slot)] = interior;
            }

            var blueprint = Blueprints[blueprintId];
            if (interior.BlueprintId != blueprint.Id || interior.BlueprintVersion != blueprint.Version)
            {
                Rebuild(interior, blueprint);
            }

            interior.IsActive = true;
            live = (surfaceIndex, slot, interior);
            lastLiveEditTick = -1;
            return true;
        }

        /// <summary>Leaves the live interior: snapshots it into its blueprint (re-rating if it changed) and freezes it.</summary>
        private bool ExitInterior()
        {
            if (live is not { } current)
            {
                return false;
            }

            CommitLiveBlueprint(current);
            current.Interior.IsActive = false;
            live = null;
            lastLiveEditTick = -1;
            return true;
        }

        private void CommitLiveBlueprint((int Surface, int Slot, Surface Interior) current)
        {
            var blueprintId = surfaces[current.Surface].Buildings.ConfigOf(current.Slot);
            if ((uint)blueprintId >= (uint)Blueprints.Count)
            {
                return;
            }

            var blueprint = Blueprints[blueprintId];
            blueprint.SetEntries(Blueprint.Snapshot(current.Interior));
            // The live floor is the blueprint's source, so it's current by definition.
            current.Interior.BlueprintId = blueprint.Id;
            current.Interior.BlueprintVersion = blueprint.Version;
        }

        /// <summary>A second after the last edit inside a live interior, re-rate it, so the warehouse's I/O follows the layout.</summary>
        private void RerateLiveIfSettled()
        {
            if (live is { } current && lastLiveEditTick >= 0 && Tick - lastLiveEditTick >= RerateDelayTicks)
            {
                CommitLiveBlueprint(current);
                lastLiveEditTick = -1;
            }
        }

        private Surface CreateInterior()
        {
            var size = BlueprintCalibrator.InteriorSize;
            var interior = AddSurface("interior", SurfaceKind.Interior, size, size);
            interior.FillTiles(0, 0, size, size, TileKind.Foundation);
            interior.IsActive = false;
            return interior;
        }

        /// <summary>Clears a floor and lays a blueprint on it. Removal is two-phase, so the old entities leave on the floor's next tick.</summary>
        private static void Rebuild(Surface interior, Blueprint blueprint)
        {
            var buildings = interior.Buildings;
            for (var slot = 0; slot < buildings.HighWater; slot++)
            {
                if (buildings.IsUsable(slot))
                {
                    interior.TryRemoveAt(buildings.OriginX(slot), buildings.OriginY(slot));
                }
            }

            foreach (var entry in blueprint.Entries)
            {
                interior.TryPlace(entry.Kind, entry.X, entry.Y, entry.Direction, entry.Recipe, entry.Config);
            }

            interior.BlueprintId = blueprint.Id;
            interior.BlueprintVersion = blueprint.Version;
        }

        /// <summary>
        /// Between ticks: power the live floor at its warehouse's satisfaction, top input docks up from
        /// the warehouse's input buffer, and move output-dock contents into its output buffer. One item
        /// type at a time, counts moved exactly, so the item total is conserved.
        /// </summary>
        private void SyncLiveInterior()
        {
            if (live is not { } current)
            {
                return;
            }

            var outside = surfaces[current.Surface];
            var slot = current.Slot;
            if (!outside.Buildings.IsUsable(slot))
            {
                ExitInterior();
                return;
            }

            var interior = current.Interior;
            var power = outside.Power;
            var index = power.IndexOf(slot);
            interior.Power.AmbientSatisfaction = index < power.Satisfaction.Length ? power.Satisfaction[index] : 0;

            var start = outside.Buildings.InventoryStart(slot);
            if (start < 0)
            {
                return;
            }

            var inputs = outside.Buildings.InputStacks(slot);
            var length = outside.Buildings.InventoryLength(slot);
            var store = outside.Inventories;
            var floor = interior.Buildings;
            var floorStore = interior.Inventories;

            for (var dock = 0; dock < floor.HighWater; dock++)
            {
                if (!floor.IsUsable(dock))
                {
                    continue;
                }

                var kind = floor.KindOf(dock);
                if (kind == BuildingKind.InputDock)
                {
                    var stack = floor.InventoryStart(dock);
                    var item = floorStore.ItemAt(stack);
                    var room = floorStore.LimitAt(stack) - floorStore.CountAt(stack);
                    for (var source = start; source < start + inputs && room > 0 && item != ItemId.None; source++)
                    {
                        if (store.ItemAt(source) != item || store.CountAt(source) == 0)
                        {
                            continue;
                        }

                        var moved = Math.Min(room, store.CountAt(source));
                        store.RemoveFromStack(source, moved);
                        floorStore.SetStack(stack, item, floorStore.CountAt(stack) + moved);
                        room -= moved;
                    }
                }
                else if (kind == BuildingKind.OutputDock)
                {
                    var dockStart = floor.InventoryStart(dock);
                    for (var stack = dockStart; stack < dockStart + floor.InventoryLength(dock); stack++)
                    {
                        var count = floorStore.CountAt(stack);
                        if (count == 0)
                        {
                            continue;
                        }

                        var item = floorStore.ItemAt(stack);
                        for (var target = start + inputs; target < start + length && count > 0; target++)
                        {
                            if (store.ItemAt(target) != item)
                            {
                                continue;
                            }

                            var moved = Math.Min(count, store.LimitAt(target) - store.CountAt(target));
                            if (moved <= 0)
                            {
                                continue;
                            }

                            store.AddToStack(target, moved);
                            floorStore.RemoveFromStack(stack, moved);
                            count -= moved;
                        }
                    }
                }
            }
        }

        // ---------------------------------------------------------------- ships and shipments

        private void LaunchShip(Surface from, ShipLaunch launch)
        {
            if ((uint)launch.Design >= DesignSlots || Designs[launch.Design] is not { } design)
            {
                return;
            }

            SpawnPlayerShip(design, from.Frame.LocalToWorld(new Vector2(launch.X + 2f, launch.Y + 2f)));
        }

        /// <summary>Puts a player ship into space at <paramref name="position"/>, posted on the next slot of the station's picket line.</summary>
        public void SpawnPlayerShip(ShipDesign design, Vector2 position)
        {
            // Posts fan out on the station's bastion-facing side, so new ships form a picket line.
            var towardBastion = Space.BastionPosition - Space.StationPosition;
            var heading = towardBastion == Vector2.Zero ? 0f : FlightSystem.Heading(towardBastion);
            var forward = towardBastion == Vector2.Zero ? new Vector2(1, 0) : Vector2.Normalize(towardBastion);
            var side = new Vector2(-forward.Y, forward.X);
            var post = Space.StationPosition + forward * 30f + side * (((launchedShips % 9) - 4) * 7f);
            launchedShips++;
            Space.Spawn(design, Faction.Player, position, heading, post);
        }

        /// <summary>Where a launch from <paramref name="from"/> goes: the station, from anywhere but the station itself.</summary>
        internal int DestinationFor(Surface from) => Station is { } station && station != from ? station.Index : -1;

        internal long CurrentTick => Tick;

        private void UpdateRequests()
        {
            Array.Clear(Requests);
            if (Station is not { } station)
            {
                return;
            }

            var buildings = station.Buildings;
            var store = station.Inventories;
            for (var slot = 0; slot < buildings.HighWater; slot++)
            {
                if (!buildings.IsUsable(slot) || buildings.KindOf(slot) != BuildingKind.Shipyard)
                {
                    continue;
                }

                var order = buildings.ConfigOf(slot);
                var design = ShipyardOrder.DesignOf(order);
                if (ShipyardOrder.ModeOf(order) == ShipyardMode.Off || design >= DesignSlots || Designs[design] is not { Stats.IsValid: true } wanted)
                {
                    continue;
                }

                var start = buildings.InventoryStart(slot);
                foreach (var (item, count) in wanted.Stats.Cost)
                {
                    var stack = start + Array.IndexOf(Buildings.ShipyardStacks, item);
                    Requests[(int)item] += Math.Min(count * RequestedBills, store.LimitAt(stack)) - store.CountAt(stack);
                }
            }

            foreach (var shipment in inTransit)
            {
                foreach (var (item, count) in shipment.Cargo)
                {
                    Requests[(int)item] -= count;
                }
            }

            foreach (var surface in surfaces)
            {
                var kind = surface == station ? BuildingKind.Receiver : BuildingKind.LaunchSilo;
                var table = surface.Buildings;
                for (var slot = 0; slot < table.HighWater; slot++)
                {
                    if (!table.IsUsable(slot) || table.KindOf(slot) != kind)
                    {
                        continue;
                    }

                    var start = table.InventoryStart(slot);
                    for (var stack = start; stack < start + table.InventoryLength(slot); stack++)
                    {
                        Requests[(int)surface.Inventories.ItemAt(stack)] -= surface.Inventories.CountAt(stack);
                    }
                }
            }
        }

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

        /// <summary>Hash of every surface, shipments in flight, blueprints and space. Requires state hashing enabled at construction.</summary>
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
                hash = StateHash.Mix(hash, surface.IsActive ? 1UL : 0UL);
            }

            foreach (var shipment in inTransit)
            {
                hash = StateHash.Mix(hash, (ulong)shipment.ArrivalTick);
                foreach (var (item, count) in shipment.Cargo)
                {
                    hash = StateHash.Mix(hash, ((ulong)item << 32) | (uint)count);
                }
            }

            foreach (var blueprint in Blueprints)
            {
                hash = StateHash.Mix(hash, ((ulong)blueprint.Version << 32) | (uint)blueprint.Rating.Islands.Length);
            }

            Space.HashInto(ref hash);
            return StateHash.Mix(hash, (ulong)Tick);
        }
    }
}
