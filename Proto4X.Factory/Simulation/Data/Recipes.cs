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
        Anywhere = Planet | Orbit,
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
            RecipeId.Gear, RecipeId.Wire, RecipeId.Circuit, RecipeId.CrushChunk, RecipeId.HullSegment,
        ];

        public static Recipe Get(RecipeId id) => table[(int)id];

        /// <summary>Furnaces pick their recipe from whatever they're fed, so the lookup goes item to recipe.</summary>
        public static RecipeId SmeltingRecipeFor(ItemId input) => input switch
        {
            ItemId.IronOre => RecipeId.SmeltIron,
            ItemId.CopperOre => RecipeId.SmeltCopper,
            _ => RecipeId.None,
        };

        private static Recipe[] BuildTable()
        {
            var recipes = new Recipe[(int)RecipeId.HullSegment + 1];
            recipes[(int)RecipeId.None] = new Recipe(RecipeId.None, [], ItemId.None, 0, 1, SurfaceCondition.Anywhere, false);
            recipes[(int)RecipeId.SmeltIron] = new Recipe(RecipeId.SmeltIron, [new(ItemId.IronOre, 1)], ItemId.IronPlate, 1, 96, SurfaceCondition.Anywhere, true);
            recipes[(int)RecipeId.SmeltCopper] = new Recipe(RecipeId.SmeltCopper, [new(ItemId.CopperOre, 1)], ItemId.CopperPlate, 1, 96, SurfaceCondition.Anywhere, true);
            recipes[(int)RecipeId.Gear] = new Recipe(RecipeId.Gear, [new(ItemId.IronPlate, 2)], ItemId.Gear, 1, 40, SurfaceCondition.Anywhere, false);
            recipes[(int)RecipeId.Wire] = new Recipe(RecipeId.Wire, [new(ItemId.CopperPlate, 1)], ItemId.Wire, 2, 30, SurfaceCondition.Anywhere, false);
            recipes[(int)RecipeId.Circuit] = new Recipe(RecipeId.Circuit, [new(ItemId.IronPlate, 1), new(ItemId.Wire, 3)], ItemId.Circuit, 1, 40, SurfaceCondition.Anywhere, false);
            recipes[(int)RecipeId.CrushChunk] = new Recipe(RecipeId.CrushChunk, [new(ItemId.MetallicChunk, 1)], ItemId.IronOre, 2, 60, SurfaceCondition.Anywhere, false);
            recipes[(int)RecipeId.HullSegment] = new Recipe(RecipeId.HullSegment, [new(ItemId.Gear, 2), new(ItemId.Circuit, 1)], ItemId.HullSegment, 1, 120, SurfaceCondition.Orbit, false);
            return recipes;
        }
    }
}
