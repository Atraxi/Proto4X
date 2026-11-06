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
    /// <summary>Per-surface screen overlay: the hovered building's power satisfaction as a bar.</summary>
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
}
