using System;

namespace Proto4X.Factory.Simulation.Data
{
    public enum RecipeId : byte
    {
        None = 0,
        SmeltIron,
        SmeltCopper,
        Gear,
        Wire,
        Circuit,
        CrushChunk,
        HullSegment,
        SmeltSteel,
        Frame,
        ArmorPlate,
        Engine,
        PowerCell,
        Processor,
        Emitter,
    }

    /// <summary>
    /// Where a recipe is allowed to run. The expansion's planets gate recipes on gravity, pressure
    /// and so on; one flag per surface kind is enough to prove the rule is data, not code.
    /// </summary>
    [Flags]
    public enum SurfaceCondition : byte
    {
        Planet = 1,
        Orbit = 2,
        /// <summary>Inside a warehouse (plans/03).</summary>
        Interior = 4,
        Anywhere = Planet | Orbit | Interior,
    }

    public readonly record struct Ingredient(ItemId Item, int Amount);

    public sealed class Recipe(RecipeId id, Ingredient[] ingredients, ItemId result, int resultAmount, int ticks, SurfaceCondition allowedOn, bool isSmelting)
    {
        public RecipeId Id { get; } = id;
        public Ingredient[] Ingredients { get; } = ingredients;
        public ItemId Result { get; } = result;
        public int ResultAmount { get; } = resultAmount;

        /// <summary>Duration at full power, in ticks.</summary>
        public int Ticks { get; } = ticks;
        public SurfaceCondition AllowedOn { get; } = allowedOn;
        public bool IsSmelting { get; } = isSmelting;
    }

    public static class Recipes
    {
        private static readonly Recipe[] table = BuildTable();

        /// <summary>The recipes an assembler can be set to, in the order the build tool cycles through them.</summary>
        public static readonly RecipeId[] AssemblerRecipes =
        [
            RecipeId.Gear, RecipeId.Wire, RecipeId.Circuit, RecipeId.Frame, RecipeId.ArmorPlate, RecipeId.Engine,
            RecipeId.PowerCell, RecipeId.Processor, RecipeId.Emitter, RecipeId.CrushChunk, RecipeId.HullSegment,
        ];

        public static Recipe Get(RecipeId id) => table[(int)id];

        /// <summary>Furnaces pick their recipe from whatever they're fed, so the lookup goes item to recipe.</summary>
        public static RecipeId SmeltingRecipeFor(ItemId input) => input switch
        {
            ItemId.IronOre => RecipeId.SmeltIron,
            ItemId.CopperOre => RecipeId.SmeltCopper,
            // Steel is smelted from plates, as in the genre: same furnace, picked by what it's fed.
            ItemId.IronPlate => RecipeId.SmeltSteel,
            _ => RecipeId.None,
        };

        private static Recipe[] BuildTable()
        {
            var recipes = new Recipe[(int)RecipeId.Emitter + 1];
            recipes[(int)RecipeId.None] = new Recipe(RecipeId.None, [], ItemId.None, 0, 1, SurfaceCondition.Anywhere, false);
            recipes[(int)RecipeId.SmeltIron] = new Recipe(RecipeId.SmeltIron, [new(ItemId.IronOre, 1)], ItemId.IronPlate, 1, 96, SurfaceCondition.Anywhere, true);
            recipes[(int)RecipeId.SmeltCopper] = new Recipe(RecipeId.SmeltCopper, [new(ItemId.CopperOre, 1)], ItemId.CopperPlate, 1, 96, SurfaceCondition.Anywhere, true);
            recipes[(int)RecipeId.Gear] = new Recipe(RecipeId.Gear, [new(ItemId.IronPlate, 2)], ItemId.Gear, 1, 40, SurfaceCondition.Anywhere, false);
            recipes[(int)RecipeId.Wire] = new Recipe(RecipeId.Wire, [new(ItemId.CopperPlate, 1)], ItemId.Wire, 2, 30, SurfaceCondition.Anywhere, false);
            recipes[(int)RecipeId.Circuit] = new Recipe(RecipeId.Circuit, [new(ItemId.IronPlate, 1), new(ItemId.Wire, 3)], ItemId.Circuit, 1, 40, SurfaceCondition.Anywhere, false);
            recipes[(int)RecipeId.CrushChunk] = new Recipe(RecipeId.CrushChunk, [new(ItemId.MetallicChunk, 1)], ItemId.IronOre, 2, 60, SurfaceCondition.Anywhere, false);
            recipes[(int)RecipeId.HullSegment] = new Recipe(RecipeId.HullSegment, [new(ItemId.Gear, 2), new(ItemId.Circuit, 1)], ItemId.HullSegment, 1, 120, SurfaceCondition.Orbit, false);
            recipes[(int)RecipeId.SmeltSteel] = new Recipe(RecipeId.SmeltSteel, [new(ItemId.IronPlate, 5)], ItemId.Steel, 1, 480, SurfaceCondition.Anywhere, true);
            recipes[(int)RecipeId.Frame] = new Recipe(RecipeId.Frame, [new(ItemId.Steel, 2), new(ItemId.Gear, 2)], ItemId.Frame, 1, 240, SurfaceCondition.Anywhere, false);
            recipes[(int)RecipeId.ArmorPlate] = new Recipe(RecipeId.ArmorPlate, [new(ItemId.Steel, 3), new(ItemId.CopperPlate, 2)], ItemId.ArmorPlate, 1, 360, SurfaceCondition.Anywhere, false);
            recipes[(int)RecipeId.Engine] = new Recipe(RecipeId.Engine, [new(ItemId.Steel, 1), new(ItemId.Gear, 2), new(ItemId.Circuit, 2)], ItemId.Engine, 1, 480, SurfaceCondition.Anywhere, false);
            recipes[(int)RecipeId.PowerCell] = new Recipe(RecipeId.PowerCell, [new(ItemId.Circuit, 3), new(ItemId.CopperPlate, 4)], ItemId.PowerCell, 1, 360, SurfaceCondition.Anywhere, false);
            recipes[(int)RecipeId.Processor] = new Recipe(RecipeId.Processor, [new(ItemId.Circuit, 3), new(ItemId.Wire, 4)], ItemId.Processor, 1, 360, SurfaceCondition.Anywhere, false);
            recipes[(int)RecipeId.Emitter] = new Recipe(RecipeId.Emitter, [new(ItemId.PowerCell, 1), new(ItemId.Circuit, 2), new(ItemId.Steel, 1)], ItemId.Emitter, 1, 480, SurfaceCondition.Anywhere, false);
            return recipes;
        }
    }
}
