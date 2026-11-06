using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Systems.Infrastructure;
using MonoGameLibrary.World;
using Proto4X.Factory.Simulation.Belts;
using Proto4X.Factory.Simulation.Components;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Diagnostics;
using Proto4X.Factory.Simulation.Grid;
using Proto4X.Factory.Simulation.Power;
using Proto4X.Factory.Simulation.Stores;
using Proto4X.Factory.Simulation.Systems;
using System.Collections.Generic;

namespace Proto4X.Factory.Simulation
{
    public enum PlacementResult
    {
        Placed,
        OutOfBounds,
        WrongTerrain,
        WrongSurface,
        Occupied,
        NoOreUnderneath,
        RecipeNotAllowedHere,
    }

    /// <summary>One cargo launch in flight between surfaces.</summary>
    public sealed record Shipment(int FromSurface, int ToSurface, long ArrivalTick, (ItemId Item, int Count)[] Cargo);

    /// <summary>
    /// One self-contained simulation space - a planet, or a ship. Each owns its own engine
    /// <see cref="GameWorld"/>, its own stores, and its own instances of every system.
    /// </summary>
    /// <remarks>
    /// Surfaces share nothing at tick time. Anything that crosses between them (a launch) goes
    /// through <see cref="Outbound"/> and is delivered by <see cref="Universe"/> between ticks, which
    /// is what would let surfaces tick in parallel - if the engine's global registries were
    /// thread-safe, which today they aren't (ENGINE_REQUESTS.md, "Global registries").
    /// <para>
    /// All structural changes (placement, removal, recipe changes) happen between ticks, through
    /// the methods here. Placement updates the stores immediately and queues the entity; the engine
    /// flushes it at the end of the next tick's update, so a building starts working the tick after
    /// that. Removal is two-phase: marked now, entity removed by <see cref="DeconstructionSystem"/>
    /// during the next tick, stores released after it.
    /// </para>
    /// </remarks>
    public sealed class Surface
    {
        public int Index { get; }
        public string Name { get; }
        public SurfaceKind Kind { get; }

        public GameWorld World { get; }
        public SurfaceGrid Grid { get; }
        public BuildingTable Buildings { get; } = new();
        public InventoryStore Inventories { get; } = new();
        public BeltNetwork Belts { get; }
        public PowerGrid Power { get; }

        public SurfaceFrame Frame;

        /// <summary>Bumped whenever building configuration (recipes) changes from outside a system.</summary>
        public int ConfigVersion { get; private set; }

        public int PendingRemovalCount { get; private set; }

        /// <summary>Slots whose entities <see cref="DeconstructionSystem"/> queued for removal this tick.</summary>
        internal List<int> RemovedThisTick { get; } = [];

        /// <summary>Launches completed this tick, collected by the universe after the tick.</summary>
        internal List<Shipment> Outbound { get; } = [];

        /// <summary>Ships finished by shipyards this tick, launched into space by the universe after the tick.</summary>
        internal List<ShipLaunch> ShipsLaunched { get; } = [];

        /// <summary>
        /// Whether the universe ticks this surface. Planets and orbits always; a warehouse interior only
        /// while it's live (viewed or edited). An inactive surface is frozen, not discarded.
        /// </summary>
        public bool IsActive { get; internal set; } = true;

        /// <summary>Interior only: the blueprint and version this floor was last built from.</summary>
        public int BlueprintId { get; internal set; } = -1;
        public int BlueprintVersion { get; internal set; } = -1;

        /// <summary>Set by the universe for a tick when component state should be folded into <see cref="ComponentHash"/>.</summary>
        internal bool HashRequested;
        internal ulong ComponentHash;

        public Universe Universe { get; }

        public long CurrentTick => Universe.Tick;

        internal Surface(Universe universe, int index, string name, SurfaceKind kind, int width, int height)
        {
            Universe = universe;
            Index = index;
            Name = name;
            Kind = kind;
            World = new GameWorld(0, 0, width, height);
            Grid = new SurfaceGrid(width, height);
            Belts = new BeltNetwork(Grid, Buildings);
            Power = new PowerGrid(Grid, Buildings, ambient: kind == SurfaceKind.Interior);
        }

        internal void RegisterSystems(SystemScheduler scheduler, bool includeStateHashing, System.Func<IUpdateSystem, IUpdateSystem>? decorate)
        {
            void Add(IUpdateSystem system) => scheduler.Add(World, decorate is null ? system : decorate(system));

            // Order is the tick's causality: tear-downs first, so nothing interacts with a building
            // that's going; generators offer, the solve sets satisfaction, then every consumer acts.
            Add(new DeconstructionSystem(this));
            Add(new GeneratorSystem(this));
            Add(new PowerSolveSystem(this));
            Add(new DrillSystem(this));
            Add(new BeltSystem(this));
            Add(new InserterSystem(this));
            Add(new CrafterSystem(this));
            Add(new LauncherSystem(this));
            Add(new WarehouseSystem(this));
            Add(new ShipyardSystem(this));

            if (includeStateHashing)
            {
                // One system per component type: a system declares a single AND-query, so there is
                // no way to visit "every component of every kind" from one place.
                Add(new ComponentHashSystem<Inserter>(this));
                Add(new ComponentHashSystem<Drill>(this));
                Add(new ComponentHashSystem<Crafter>(this));
                Add(new ComponentHashSystem<Generator>(this));
                Add(new ComponentHashSystem<Launcher>(this));
                Add(new ComponentHashSystem<Warehouse>(this));
                Add(new ComponentHashSystem<Shipyard>(this));
            }
        }

        // ---------------------------------------------------------------- terrain setup

        public void FillTiles(int x, int y, int width, int height, TileKind kind)
        {
            for (var ty = y; ty < y + height; ty++)
            {
                for (var tx = x; tx < x + width; tx++)
                {
                    if (Grid.InBounds(tx, ty))
                    {
                        Grid.SetTile(tx, ty, kind);
                    }
                }
            }
        }

        public void FillOre(int x, int y, int width, int height, ItemId ore, int amountPerTile)
        {
            for (var ty = y; ty < y + height; ty++)
            {
                for (var tx = x; tx < x + width; tx++)
                {
                    if (Grid.InBounds(tx, ty))
                    {
                        Grid.SetOre(tx, ty, ore, amountPerTile);
                    }
                }
            }
        }

        // ---------------------------------------------------------------- structural changes

        public PlacementResult CanPlace(BuildingKind kind, int x, int y, RecipeId recipe = RecipeId.None)
        {
            if (kind == BuildingKind.None || (int)kind >= Data.Buildings.Count)
            {
                return PlacementResult.WrongSurface;
            }

            var spec = Data.Buildings.Spec(kind);
            var size = spec.Size;
            var requiredTile = Kind == SurfaceKind.Planet ? TileKind.Ground : TileKind.Foundation;

            if (!Grid.InBounds(x, y) || !Grid.InBounds(x + size - 1, y + size - 1))
            {
                return PlacementResult.OutOfBounds;
            }

            if ((spec.AllowedOn & ConditionOf(Kind)) == 0)
            {
                return PlacementResult.WrongSurface;
            }

            var hasOre = false;
            for (var ty = y; ty < y + size; ty++)
            {
                for (var tx = x; tx < x + size; tx++)
                {
                    if (Grid.TileAt(tx, ty) != requiredTile)
                    {
                        return PlacementResult.WrongTerrain;
                    }

                    if (Grid.OccupantAt(tx, ty) >= 0)
                    {
                        return PlacementResult.Occupied;
                    }

                    hasOre |= Grid.OreAt(tx, ty) != ItemId.None;
                }
            }

            if (kind == BuildingKind.Drill && !hasOre)
            {
                return PlacementResult.NoOreUnderneath;
            }

            if (kind == BuildingKind.Assembler && (Recipes.Get(recipe).AllowedOn & ConditionOf(Kind)) == 0)
            {
                return PlacementResult.RecipeNotAllowedHere;
            }

            return PlacementResult.Placed;
        }

        public static SurfaceCondition ConditionOf(SurfaceKind kind) => kind switch
        {
            SurfaceKind.Planet => SurfaceCondition.Planet,
            SurfaceKind.Orbit => SurfaceCondition.Orbit,
            _ => SurfaceCondition.Interior,
        };

        /// <summary>Places a building. Between ticks only - see <see cref="Universe.Enqueue"/> for the in-game path.</summary>
        /// <param name="config">The kind's setting (see <see cref="BuildingTable.ConfigOf"/>): dock filter, warehouse blueprint, shipyard order.</param>
        public PlacementResult TryPlace(BuildingKind kind, int x, int y, Direction direction, RecipeId recipe = RecipeId.None, int config = 0)
        {
            if (kind == BuildingKind.Assembler && recipe == RecipeId.None)
            {
                recipe = RecipeId.Gear;
            }

            var result = CanPlace(kind, x, y, recipe);
            if (result != PlacementResult.Placed)
            {
                return result;
            }

            var size = Data.Buildings.Size(kind);
            var slot = Buildings.Allocate(kind, x, y, direction);
            Buildings.SetConfig(slot, config);
            Grid.SetOccupant(x, y, size, slot);
            Grid.BumpChunksAround(x, y, size);
            Power.MarkDirty();

            var entity = EntityBuilder.CreateEntity()
                .With(new Building(slot, kind))
                .With(new Placement(x, y, size, direction));

            switch (kind)
            {
                case BuildingKind.Belt:
                    Belts.MarkDirty();
                    entity.With(new BeltTile());
                    break;

                case BuildingKind.Inserter:
                    entity.With(new Inserter(
                        x - direction.Dx(), y - direction.Dy(),
                        x + direction.Dx(), y + direction.Dy(),
                        Grid.ChunkIndexOf(x, y)));
                    break;

                case BuildingKind.Drill:
                case BuildingKind.Collector:
                {
                    var (outputX, outputY) = DrillOutputTile(x, y, direction);
                    var produces = kind == BuildingKind.Collector ? ItemId.MetallicChunk : ItemId.None;
                    entity.With(new Drill(outputX, outputY, produces, Grid.ChunkIndexOf(x, y)));
                    break;
                }

                case BuildingKind.Furnace:
                {
                    var inventory = Inventories.Allocate(2);
                    // Room for two of the hungriest smelt (steel: 5 plates), as with assemblers.
                    Inventories.ConfigureFree(inventory, 10);
                    Inventories.ConfigureFree(inventory + 1, 0);
                    Buildings.SetInventory(slot, inventory, 2, 1);
                    entity.With(new Crafter(inventory, RecipeId.None, picksRecipeFromInput: true) { ConfigVersion = ConfigVersion });
                    break;
                }

                case BuildingKind.Assembler:
                {
                    Buildings.SetRecipe(slot, recipe);
                    var inventory = AllocateRecipeInventory(slot, recipe);
                    entity.With(new Crafter(inventory, recipe, picksRecipeFromInput: false) { ConfigVersion = ConfigVersion });
                    break;
                }

                case BuildingKind.Chest:
                case BuildingKind.Receiver:
                {
                    var inventory = Inventories.Allocate(Data.Buildings.ChestStacks);
                    Buildings.SetInventory(slot, inventory, Data.Buildings.ChestStacks, Data.Buildings.ChestStacks);
                    break;
                }

                case BuildingKind.LaunchSilo:
                {
                    var inventory = Inventories.Allocate(Data.Buildings.LaunchSiloStacks);
                    Buildings.SetInventory(slot, inventory, Data.Buildings.LaunchSiloStacks, Data.Buildings.LaunchSiloStacks);
                    entity.With(new Launcher(inventory, Data.Buildings.LaunchSiloStacks));
                    break;
                }

                case BuildingKind.Generator:
                {
                    var inventory = Inventories.Allocate(1);
                    Inventories.ConfigureFixed(inventory, ItemId.Coal, 5);
                    Buildings.SetInventory(slot, inventory, 1, 1);
                    // A starter charge: the inserter that feeds a generator runs on that generator's
                    // power, so without it a fresh build can never start. (The original solves this
                    // with burner-fuelled inserters; one free coal is the cheap version.)
                    entity.With(new Generator(inventory) { FuelEnergy = Items.CoalFuelValue });
                    break;
                }

                case BuildingKind.SolarPanel:
                    entity.With(new Generator(-1) { Solar = true });
                    break;

                case BuildingKind.InputDock:
                {
                    var inventory = Inventories.Allocate(1);
                    Inventories.ConfigureFixed(inventory, (ItemId)config, Data.Buildings.DockStackLimit);
                    Buildings.SetInventory(slot, inventory, 1, 0);
                    break;
                }

                case BuildingKind.OutputDock:
                {
                    var inventory = Inventories.Allocate(Data.Buildings.OutputDockStacks);
                    Buildings.SetInventory(slot, inventory, Data.Buildings.OutputDockStacks, Data.Buildings.OutputDockStacks);
                    break;
                }

                case BuildingKind.Warehouse:
                    // The buffer is laid out from the blueprint's rating by WarehouseSystem on its first tick.
                    entity.With(new Warehouse { Blueprint = config, RatingVersion = -1, ConfigVersion = -1, PowerVersion = -1 });
                    break;

                case BuildingKind.Shipyard:
                {
                    var stacks = Data.Buildings.ShipyardStacks;
                    var inventory = Inventories.Allocate(stacks.Length);
                    for (var i = 0; i < stacks.Length; i++)
                    {
                        Inventories.ConfigureFixed(inventory + i, stacks[i], Data.Buildings.ShipyardStackLimit);
                    }

                    Buildings.SetInventory(slot, inventory, stacks.Length, stacks.Length);
                    entity.With(new Shipyard(inventory));
                    break;
                }

                case BuildingKind.Pole:
                    break;
            }

            World.AddEntity(entity);
            return PlacementResult.Placed;
        }

        /// <summary>Where a drill drops what it mines: one tile out from its front edge, offset by facing.</summary>
        public static (int X, int Y) DrillOutputTile(int x, int y, Direction direction) => direction switch
        {
            Direction.North => (x, y - 1),
            Direction.East => (x + 2, y),
            Direction.South => (x + 1, y + 2),
            _ => (x - 1, y + 1),
        };

        internal int AllocateRecipeInventory(int slot, RecipeId recipeId)
        {
            var recipe = Recipes.Get(recipeId);
            var inputs = recipe.Ingredients.Length;
            var inventory = Inventories.Allocate(inputs + 1);
            for (var i = 0; i < inputs; i++)
            {
                var ingredient = recipe.Ingredients[i];
                Inventories.ConfigureFixed(inventory + i, ingredient.Item, ingredient.Amount * 2);
            }

            Inventories.ConfigureFixed(inventory + inputs, recipe.Result, 0);
            Buildings.SetInventory(slot, inventory, inputs + 1, inputs);
            return inventory;
        }

        public bool TryRemoveAt(int x, int y)
        {
            var slot = Grid.OccupantAt(x, y);
            if (slot < 0 || !Buildings.IsUsable(slot))
            {
                return false;
            }

            var kind = Buildings.KindOf(slot);
            var size = Data.Buildings.Size(kind);
            var originX = Buildings.OriginX(slot);
            var originY = Buildings.OriginY(slot);

            Buildings.MarkPendingRemoval(slot);
            PendingRemovalCount++;
            Grid.SetOccupant(originX, originY, size, -1);
            Grid.BumpChunksAround(originX, originY, size);

            if (kind == BuildingKind.Belt)
            {
                Belts.MarkDirty();
            }

            Power.MarkDirty();
            return true;
        }

        public bool TrySetRecipeAt(int x, int y, RecipeId recipe)
        {
            var slot = Grid.OccupantAt(x, y);
            if (slot < 0 || !Buildings.IsUsable(slot) || Buildings.KindOf(slot) != BuildingKind.Assembler)
            {
                return false;
            }

            if ((Recipes.Get(recipe).AllowedOn & ConditionOf(Kind)) == 0 || Buildings.RecipeOf(slot) == recipe)
            {
                return false;
            }

            Buildings.SetRecipe(slot, recipe);
            ConfigVersion++;
            return true;
        }

        /// <summary>
        /// Changes a building's player setting. A dock's filter re-types its stack (contents are
        /// dropped - shortcut: they should go back to the warehouse buffer); a warehouse's blueprint and
        /// a shipyard's order are pulled in by their systems.
        /// </summary>
        public bool TrySetConfigAt(int x, int y, int config)
        {
            var slot = Grid.OccupantAt(x, y);
            if (slot < 0 || !Buildings.IsUsable(slot))
            {
                return false;
            }

            switch (Buildings.KindOf(slot))
            {
                case BuildingKind.InputDock:
                    if ((uint)config >= Items.Count)
                    {
                        return false;
                    }

                    Inventories.SetStack(Buildings.InventoryStart(slot), ItemId.None, 0);
                    Inventories.ConfigureFixed(Buildings.InventoryStart(slot), (ItemId)config, Data.Buildings.DockStackLimit);
                    break;

                case BuildingKind.Warehouse:
                    if ((uint)config >= (uint)Universe.Blueprints.Count)
                    {
                        return false;
                    }

                    break;

                case BuildingKind.Shipyard:
                    break;

                default:
                    return false;
            }

            Buildings.SetConfig(slot, config);
            ConfigVersion++;
            return true;
        }

        /// <summary>
        /// (Re)lays out a warehouse's buffer for a rating: one fixed stack per rated input, then one per
        /// rated output, each big enough for two cycles. Items whose stack survives the relayout are
        /// carried over (clamped to the new limit); the rest are dropped - shortcut: refund them.
        /// </summary>
        internal int LayOutWarehouse(int slot, Warehouses.BlueprintRating rating)
        {
            var oldStart = Buildings.InventoryStart(slot);
            var oldLength = Buildings.InventoryLength(slot);
            var oldInputs = Buildings.InputStacks(slot);
            var inputs = rating.Inputs.Length;
            var outputs = rating.Outputs.Length;
            var length = System.Math.Max(1, inputs + outputs);
            var start = Inventories.Allocate(length);
            for (var i = 0; i < inputs; i++)
            {
                var (item, amount) = rating.Inputs[i];
                Inventories.ConfigureFixed(start + i, item, System.Math.Max(amount * 2, Items.StackSize(item)));
            }

            for (var i = 0; i < outputs; i++)
            {
                var (item, amount) = rating.Outputs[i];
                Inventories.ConfigureFixed(start + inputs + i, item, System.Math.Max(amount * 2, Items.StackSize(item)));
            }

            if (inputs + outputs == 0)
            {
                Inventories.ConfigureFixed(start, ItemId.None, 0);
            }

            if (oldStart >= 0)
            {
                for (var stack = oldStart; stack < oldStart + oldLength; stack++)
                {
                    var count = Inventories.CountAt(stack);
                    if (count == 0)
                    {
                        continue;
                    }

                    var item = Inventories.ItemAt(stack);
                    var wasInput = stack - oldStart < oldInputs;
                    var first = wasInput ? start : start + inputs;
                    var run = wasInput ? inputs : outputs;
                    for (var target = first; target < first + run; target++)
                    {
                        if (Inventories.ItemAt(target) == item)
                        {
                            Inventories.SetStack(target, item, System.Math.Min(count, Inventories.LimitAt(target)));
                            break;
                        }
                    }
                }

                Inventories.Free(oldStart, oldLength);
            }

            Buildings.SetInventory(slot, start, length, inputs);
            return start;
        }

        // ---------------------------------------------------------------- tick boundary

        internal void BeforeTick()
        {
            if (Belts.IsDirty)
            {
                Belts.Rebuild();
            }

            if (Power.IsDirty)
            {
                Power.Rebuild();
            }
        }

        internal void AfterTick()
        {
            foreach (var slot in RemovedThisTick)
            {
                var start = Buildings.InventoryStart(slot);
                if (start >= 0)
                {
                    Inventories.Free(start, Buildings.InventoryLength(slot));
                }

                Buildings.Release(slot);
                PendingRemovalCount--;
            }

            RemovedThisTick.Clear();
        }

        public ulong StateHashOf()
        {
            var hash = StateHash.Seed;
            Grid.HashInto(ref hash);
            Buildings.HashInto(ref hash);
            Inventories.HashInto(ref hash);
            Belts.HashInto(ref hash);
            Power.HashInto(ref hash);
            hash = StateHash.Mix(hash, ComponentHash);
            return hash;
        }
    }
}
