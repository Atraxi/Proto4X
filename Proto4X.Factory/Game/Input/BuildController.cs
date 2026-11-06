using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary.Input;
using MonoGameLibrary.UI;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;
using System;
using System.Linq;

namespace Proto4X.Factory.Input
{
    /// <summary>
    /// Turns mouse and keyboard into simulation <see cref="Command"/>s. Never touches the simulation
    /// directly - it can only enqueue, which is what keeps input replayable.
    /// </summary>
    public sealed class BuildController(Universe universe, CameraBase camera)
    {
        private static readonly Keys[] toolKeys =
        [
            Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0, Keys.OemMinus, Keys.OemPlus,
        ];

        private (int X, int Y) lastPlaced = (int.MinValue, int.MinValue);
        private (int X, int Y) lastRemoved = (int.MinValue, int.MinValue);
        private bool followLive;

        public int ActiveSurface { get; private set; }
        public BuildingKind Tool { get; private set; } = BuildingKind.Belt;
        public Direction Direction { get; private set; } = Direction.East;
        public RecipeId Recipe { get; private set; } = RecipeId.Gear;

        /// <summary>The item new input docks pass in.</summary>
        public ItemId DockItem { get; private set; } = ItemId.IronPlate;
        public (int X, int Y) Hovered { get; private set; }

        /// <summary>The building the inspector shows: a surface and a tile on it, or none.</summary>
        public (int Surface, int X, int Y)? Selected { get; set; }

        public Surface Surface => universe.Surfaces[ActiveSurface];

        /// <summary>The buildings that can go on the active surface, in toolbar order.</summary>
        public BuildingKind[] Toolbar =>
            [.. Enum.GetValues<BuildingKind>().Where(k => k != BuildingKind.None
                && (Buildings.Spec(k).AllowedOn & Surface.ConditionOf(Surface.Kind)) != 0)];

        public void SelectTool(BuildingKind kind) => Tool = kind;

        public void SelectRecipe(RecipeId recipe) => Recipe = recipe;

        public void SelectDockItem(ItemId item) => DockItem = item;

        public void ShowSurface(int index)
        {
            if ((uint)index < (uint)universe.Surfaces.Count && universe.Surfaces[index].Kind != SurfaceKind.Interior)
            {
                ActiveSurface = index;
            }
        }

        /// <summary>Asks to enter the warehouse at (x, y) on the active surface; the view follows once the sim has made it live.</summary>
        public void Enter(int x, int y)
        {
            universe.Enqueue(new Command(CommandKind.EnterInterior, ActiveSurface, x, y));
            followLive = true;
        }

        public void Leave()
        {
            if (universe.Live is { } live)
            {
                universe.Enqueue(new Command(CommandKind.ExitInterior, -1, 0, 0));
                ActiveSurface = live.Surface;
            }
        }

        /// <summary>
        /// The grid-local point under a screen point: the camera's ground pick, then into the
        /// surface's frame. The 3D camera's world is in tiles, so there's no pixel scale to undo.
        /// </summary>
        public static Vector2 ScreenToLocal(CameraBase camera, Surface surface, Vector2 screen) =>
            surface.Frame.WorldToLocal(camera.ScreenToGround(screen));

        /// <param name="mouseOverUi">The UI has the mouse this frame: no picking, placing or removing.</param>
        /// <param name="worldInput">False in fleet mode and in the designer: the keyboard still works, the mouse belongs to them.</param>
        public void Update(Point screenMouse, bool mouseOverUi, bool worldInput)
        {
            var keyboard = InputManager.Instance.KeyboardInfo;
            var mouse = InputManager.Instance.MouseInfo;
            FollowLiveInterior();

            var toolbar = Toolbar;
            for (var i = 0; i < toolKeys.Length && i < toolbar.Length; i++)
            {
                if (keyboard.WasKeyJustPressed(toolKeys[i]))
                {
                    Tool = toolbar[i];
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

            if (keyboard.WasKeyJustPressed(Keys.Tab) && universe.Live is null)
            {
                // Interiors are reached through their warehouse, not by tabbing.
                var count = universe.Surfaces.Count;
                for (var step = 1; step <= count; step++)
                {
                    var next = (ActiveSurface + step) % count;
                    if (universe.Surfaces[next].Kind != SurfaceKind.Interior)
                    {
                        ActiveSurface = next;
                        break;
                    }
                }
            }

            var local = ScreenToLocal(camera, Surface, screenMouse.ToVector2());
            var size = Tool == BuildingKind.None ? 1 : Buildings.Size(Tool);
            // Multi-tile footprints centre on the cursor rather than hanging off its corner.
            Hovered = ((int)MathF.Floor(local.X - (size - 1) * 0.5f), (int)MathF.Floor(local.Y - (size - 1) * 0.5f));
            var pointed = ((int)MathF.Floor(local.X), (int)MathF.Floor(local.Y));

            if (keyboard.WasKeyJustPressed(Keys.T))
            {
                // With a tool, T cycles the recipe new assemblers get; with no tool, it re-sets the
                // assembler under the cursor.
                var recipes = Recipes.AssemblerRecipes;
                Recipe = recipes[(Array.IndexOf(recipes, Recipe) + 1) % recipes.Length];
                if (Tool == BuildingKind.None)
                {
                    universe.Enqueue(new Command(CommandKind.SetRecipe, ActiveSurface, pointed.Item1, pointed.Item2, Recipe: Recipe));
                }
            }

            if (keyboard.WasKeyJustPressed(Keys.E))
            {
                if (universe.Live is not null)
                {
                    Leave();
                }
                else if (Surface.Grid.InBounds(pointed.Item1, pointed.Item2) && Surface.Grid.OccupantAt(pointed.Item1, pointed.Item2) is var slot and >= 0
                    && Surface.Buildings.KindOf(slot) == BuildingKind.Warehouse)
                {
                    Enter(pointed.Item1, pointed.Item2);
                }
            }

            if (mouseOverUi || !worldInput)
            {
                lastPlaced = lastRemoved = (int.MinValue, int.MinValue);
                return;
            }

            if (mouse.IsButtonDown(MouseButton.Left) && Tool != BuildingKind.None)
            {
                if (Hovered != lastPlaced)
                {
                    var config = Tool == BuildingKind.InputDock ? (int)DockItem : 0;
                    universe.Enqueue(new Command(CommandKind.Place, ActiveSurface, Hovered.X, Hovered.Y, Tool, Direction, Recipe, config));
                    lastPlaced = Hovered;
                }
            }
            else
            {
                lastPlaced = (int.MinValue, int.MinValue);
            }

            if (mouse.WasButtonJustPressed(MouseButton.Left) && Tool == BuildingKind.None)
            {
                Selected = (ActiveSurface, pointed.Item1, pointed.Item2);
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

        private void FollowLiveInterior()
        {
            if (followLive && universe.Live is { } live)
            {
                ActiveSurface = live.Interior.Index;
                followLive = false;
                Tool = BuildingKind.None;
            }

            // Left by the sim (the warehouse was removed): back outside.
            if (Surface.Kind == SurfaceKind.Interior && !universe.IsLiveInterior(Surface))
            {
                ActiveSurface = universe.Surfaces.First(s => s.Kind != SurfaceKind.Interior).Index;
            }
        }
    }
}
