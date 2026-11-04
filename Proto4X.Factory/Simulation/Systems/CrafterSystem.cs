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
    /// <summary>Furnaces and assemblers: start a craft when the inputs are there and the output has room, then run it.</summary>
    public sealed class CrafterSystem(Surface surface) : IUpdateSystem
    {
        private static readonly ComponentRef[] requiredComponentProviders = [ComponentRef.Of<Building>(), ComponentRef.Of<Crafter>()];

        public IReadOnlyList<ComponentRef> RequiredComponentProviders => requiredComponentProviders;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            var inventories = surface.Inventories;
            var power = surface.Power;
            var powerVersion = power.Version;
            var configVersion = surface.ConfigVersion;
            var satisfaction = power.Satisfaction;
            var demand = power.Demand;

            foreach (var chunk in archetypeChunks)
            {
                var buildingSpan = chunk.GetSpan<Building>();
                var crafterSpan = chunk.GetSpan<Crafter>();

                for (var i = 0; i < chunk.EntityCount; i++)
                {
                    ref var crafter = ref crafterSpan[i];
                    ref var building = ref buildingSpan[i];

                    if (crafter.PowerVersion != powerVersion)
                    {
                        crafter.PowerIndex = power.IndexOf(building.Slot);
                        crafter.PowerVersion = powerVersion;
                    }

                    if (crafter.ConfigVersion != configVersion)
                    {
                        PullConfiguration(ref crafter, building.Slot);
                        crafter.ConfigVersion = configVersion;
                    }

                    var spec = Buildings.Spec(building.Kind);

                    if (!crafter.IsCrafting && !TryStart(ref crafter, inventories))
                    {
                        demand[crafter.PowerIndex] += spec.IdleDraw;
                        continue;
                    }

                    demand[crafter.PowerIndex] += spec.ActiveDraw;
                    crafter.Progress += satisfaction[crafter.PowerIndex];

                    var recipe = Recipes.Get(crafter.Recipe);
                    if (crafter.Progress >= recipe.Ticks * PowerGrid.Full)
                    {
                        // Room was reserved when the craft started: only inserters take from the
                        // output stack, and nothing else adds to it.
                        var output = crafter.Inventory + recipe.Ingredients.Length;
                        inventories.SetStack(output, recipe.Result, inventories.CountAt(output) + recipe.ResultAmount);
                        crafter.IsCrafting = false;
                        crafter.Progress = 0;
                    }
                }
            }
        }

        private static bool TryStart(ref Crafter crafter, Stores.InventoryStore inventories)
        {
            if (crafter.PicksRecipeFromInput)
            {
                crafter.Recipe = inventories.CountAt(crafter.Inventory) > 0
                    ? Recipes.SmeltingRecipeFor(inventories.ItemAt(crafter.Inventory))
                    : RecipeId.None;
            }

            if (crafter.Recipe == RecipeId.None)
            {
                return false;
            }

            var recipe = Recipes.Get(crafter.Recipe);
            var ingredients = recipe.Ingredients;
            for (var k = 0; k < ingredients.Length; k++)
            {
                var stack = crafter.Inventory + k;
                if (inventories.ItemAt(stack) != ingredients[k].Item || inventories.CountAt(stack) < ingredients[k].Amount)
                {
                    return false;
                }
            }

            if (!inventories.StackAccepts(crafter.Inventory + ingredients.Length, recipe.Result, recipe.ResultAmount))
            {
                return false;
            }

            for (var k = 0; k < ingredients.Length; k++)
            {
                inventories.RemoveFromStack(crafter.Inventory + k, ingredients[k].Amount);
            }

            crafter.IsCrafting = true;
            crafter.Progress = 0;
            return true;
        }

        /// <summary>
        /// Applies a recipe change made from outside the tick. Changing ingredients means changing the
        /// stack layout, so the old inventory is released and a fresh one allocated; what was in it is
        /// discarded (creative-mode simplification - the original refunds it to the player).
        /// </summary>
        private void PullConfiguration(ref Crafter crafter, int slot)
        {
            if (crafter.PicksRecipeFromInput)
            {
                return;
            }

            var wanted = surface.Buildings.RecipeOf(slot);
            if (wanted == crafter.Recipe)
            {
                return;
            }

            surface.Inventories.Free(crafter.Inventory, surface.Buildings.InventoryLength(slot));
            crafter.Inventory = surface.AllocateRecipeInventory(slot, wanted);
            crafter.Recipe = wanted;
            crafter.IsCrafting = false;
            crafter.Progress = 0;
        }
    }
}
