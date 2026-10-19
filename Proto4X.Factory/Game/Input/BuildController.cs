using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Input;
using MonoGameLibrary.UI;
using Proto4X.Factory.Rendering;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;
using System;

namespace Proto4X.Factory.Input
{
    /// <summary>
    /// Turns mouse and keyboard into simulation <see cref="Command"/>s. Never touches the simulation
    /// directly - it can only enqueue, which is what keeps input replayable.
    /// </summary>
    public sealed class BuildController(Universe universe, Camera camera)
    {
        public static readonly BuildingKind[] Toolbar =
        [
            BuildingKind.Belt, BuildingKind.Inserter, BuildingKind.Drill, BuildingKind.Furnace, BuildingKind.Assembler,
            BuildingKind.Chest, BuildingKind.Pole, BuildingKind.Generator, BuildingKind.LaunchSilo, BuildingKind.Receiver,
            BuildingKind.Collector,
        ];

        private static readonly Keys[] toolKeys =
        [
            Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0, Keys.OemMinus,
        ];

        private (int X, int Y) lastPlaced = (int.MinValue, int.MinValue);
        private (int X, int Y) lastRemoved = (int.MinValue, int.MinValue);

        public int ActiveSurface { get; private set; }
        public BuildingKind Tool { get; private set; } = BuildingKind.Belt;
        public Direction Direction { get; private set; } = Direction.East;
        public int RecipeIndex { get; private set; }
        public RecipeId Recipe => Recipes.AssemblerRecipes[RecipeIndex];
        public (int X, int Y) Hovered { get; private set; }

        public Surface Surface => universe.Surfaces[ActiveSurface];

        public void Update(Point screenMouse)
        {
            var keyboard = InputManager.Instance.KeyboardInfo;
            var mouse = InputManager.Instance.MouseInfo;

            for (var i = 0; i < toolKeys.Length; i++)
            {
                if (keyboard.WasKeyJustPressed(toolKeys[i]))
                {
                    Tool = Toolbar[i];
                }
            }

            if (keyboard.WasKeyJustPressed(Keys.Q))
            {
                Tool = BuildingKind.None;
            }

            if (keyboard.WasKeyJustPressed(Keys.R))
            {
                Direction = Direction.RotateClockwise();
            }

            if (keyboard.WasKeyJustPressed(Keys.Tab))
            {
                ActiveSurface = (ActiveSurface + 1) % universe.Surfaces.Count;
            }

            var world = camera.ScreenToWorld(screenMouse.ToVector2());
            var local = SurfaceView.WorldToTile(Surface.Frame, world);
            var size = Tool == BuildingKind.None ? 1 : Buildings.Size(Tool);
            // Multi-tile footprints centre on the cursor rather than hanging off its corner.
            Hovered = ((int)MathF.Floor(local.X - (size - 1) * 0.5f), (int)MathF.Floor(local.Y - (size - 1) * 0.5f));
            var pointed = ((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y));

            if (keyboard.WasKeyJustPressed(Keys.T))
            {
                // With a tool, T picks the recipe new assemblers get; with no tool, it re-sets the
                // assembler under the cursor.
                RecipeIndex = (RecipeIndex + 1) % Recipes.AssemblerRecipes.Length;
                if (Tool == BuildingKind.None)
                {
                    universe.Enqueue(new Command(CommandKind.SetRecipe, ActiveSurface, pointed.Item1, pointed.Item2, Recipe: Recipe));
                }
            }

            if (mouse.IsButtonDown(MouseButton.Left) && Tool != BuildingKind.None)
            {
                if (Hovered != lastPlaced)
                {
                    universe.Enqueue(new Command(CommandKind.Place, ActiveSurface, Hovered.X, Hovered.Y, Tool, Direction, Recipe));
                    lastPlaced = Hovered;
                }
            }
            else
            {
                lastPlaced = (int.MinValue, int.MinValue);
            }

            if (mouse.IsButtonDown(MouseButton.Right))
            {
                if (pointed != lastRemoved)
                {
                    universe.Enqueue(new Command(CommandKind.Remove, ActiveSurface, pointed.Item1, pointed.Item2));
                    lastRemoved = pointed;
                }
            }
            else
            {
                lastRemoved = (int.MinValue, int.MinValue);
            }
        }
    }
}
