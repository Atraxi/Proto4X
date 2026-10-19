using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Input;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Power;
using System.Collections.Generic;

namespace Proto4X.Factory.Rendering
{
    /// <summary>The placement preview under the cursor: green if the build would succeed, red if not.</summary>
    public sealed class GhostRenderer(Surface surface, BuildController controller) : IGeometryDrawSystem
    {
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => [];

        public void Draw(GeometryBatch batch, Rectangle localCullingBounds, List<ArchetypeChunk> archetypeChunks)
        {
            if (controller.Surface != surface)
            {
                return;
            }

            var (x, y) = controller.Hovered;
            var size = controller.Tool == BuildingKind.None ? 1 : Buildings.Size(controller.Tool);
            var colour = controller.Tool == BuildingKind.None
                ? Color.White * 0.25f
                : surface.CanPlace(controller.Tool, x, y, controller.Recipe) == PlacementResult.Placed ? Palette.Valid : Palette.Invalid;

            Shapes.FillRect(batch, GeometryLayer.Translucent, x, y, size, size, FlatDepth.Overlay, colour);

            if (controller.Tool != BuildingKind.None)
            {
                var centre = new Vector2(x + size * 0.5f, y + size * 0.5f);
                var tip = centre + SurfaceView.DirectionVector(controller.Direction) * (size * 0.5f);
                Shapes.Line(batch, GeometryLayer.Translucent, centre, tip, 3f / SurfaceView.TileSize, FlatDepth.Overlay, Color.White);
            }
        }
    }

    /// <summary>
    /// Per-surface screen overlay: the hovered building's power satisfaction as a bar. Every world
    /// needs at least one screen-space system or <c>SystemScheduler.Draw</c> throws, so this is
    /// also what keeps each surface drawable (ENGINE_REQUESTS.md, "Scheduler throws").
    /// </summary>
    public sealed class SurfaceStatusRenderer(Surface surface, BuildController controller, Texture2D pixel) : IDrawSystem
    {
        public DrawSpace DrawSpace => DrawSpace.Screen;
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => [];

        public void Draw(SpriteBatch spriteBatch, Rectangle viewport, List<ArchetypeChunk> archetypeChunks)
        {
            if (controller.Surface != surface)
            {
                return;
            }

            var (x, y) = controller.Hovered;
            var network = surface.Power.NetworkAtTile(x, y);
            if (network < 0)
            {
                return;
            }

            var satisfaction = surface.Power.SatisfactionOfIndex(network + 1) / (float)PowerGrid.Full;
            var colour = satisfaction >= 0.99f ? Palette.PowerGood : satisfaction > 0f ? Palette.PowerLow : Palette.PowerNone;
            var corner = new Vector2(16, 64);
            Primitives.FillRectangle(spriteBatch, pixel, corner, new Vector2(160, 10), 0f, Color.Black * 0.6f);
            Primitives.FillRectangle(spriteBatch, pixel, corner, new Vector2(160 * satisfaction, 10), 0f, colour);
        }
    }

    /// <summary>
    /// The toolbar: one swatch per building, the selected one outlined, with the facing arrow and the
    /// recipe new assemblers will get. Registered on the meta world, so it draws above every surface.
    /// </summary>
    public sealed class HudRenderer(BuildController controller, Texture2D pixel, IReadOnlyList<Surface> surfaces) : IDrawSystem
    {
        public DrawSpace DrawSpace => DrawSpace.Screen;
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => [];

        public void Draw(SpriteBatch spriteBatch, Rectangle viewport, List<ArchetypeChunk> archetypeChunks)
        {
            const int slot = 40;
            var origin = new Vector2(16, 16);

            for (var i = 0; i < BuildController.Toolbar.Length; i++)
            {
                var kind = BuildController.Toolbar[i];
                var corner = origin + new Vector2(i * (slot + 4), 0);
                Primitives.FillRectangle(spriteBatch, pixel, corner, new Vector2(slot), 0f, Color.Black * 0.55f);
                var inner = 10 + 8 * (Buildings.Size(kind) - 1);
                Primitives.FillCentred(spriteBatch, pixel, corner + new Vector2(slot / 2f), new Vector2(inner), 0f, Palette.Building(kind));

                if (kind == controller.Tool)
                {
                    Primitives.DrawOutline(spriteBatch, pixel, corner, corner + new Vector2(slot, 0), corner + new Vector2(slot), corner + new Vector2(0, slot), Color.White, 2f);
                    var centre = corner + new Vector2(slot / 2f);
                    Primitives.DrawLine(spriteBatch, pixel, centre, centre + SurfaceView.DirectionVector(controller.Direction) * (slot / 2f), Color.White, 2f);
                }
            }

            // Recipe swatch for assemblers, after the toolbar.
            var recipeCorner = origin + new Vector2(BuildController.Toolbar.Length * (slot + 4) + 12, 0);
            Primitives.FillRectangle(spriteBatch, pixel, recipeCorner, new Vector2(slot), 0f, Color.Black * 0.55f);
            Primitives.FillCentred(spriteBatch, pixel, recipeCorner + new Vector2(slot / 2f), new Vector2(18), 0.785f, Palette.Item(Recipes.Get(controller.Recipe).Result));

            // Surface tabs: one pip per surface, the active one lit; a ship's pip is a diamond.
            for (var i = 0; i < surfaces.Count; i++)
            {
                var centre = new Vector2(viewport.Width - 24 - i * 28, 28);
                var lit = surfaces[i] == controller.Surface;
                var rotation = surfaces[i].Kind == SurfaceKind.Orbit ? 0.785f : 0f;
                Primitives.FillCentred(spriteBatch, pixel, centre, new Vector2(lit ? 18 : 12), rotation, lit ? Color.White : Color.Gray);
            }
        }
    }
}
