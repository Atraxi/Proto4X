using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using System.Collections.Generic;

namespace MonoGameLibrary.UI
{
    /// <summary>Draws a <see cref="Gui"/>'s recorded commands in the screen pass, in recording order (later on top).</summary>
    public sealed class GuiRenderer(Gui gui, Texture2D pixel) : IDrawSystem
    {
        public DrawSpace DrawSpace => DrawSpace.Screen;
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => [];

        public void Draw(SpriteBatch spriteBatch, Rectangle viewport, List<ArchetypeChunk> archetypeChunks)
        {
            foreach (var command in gui.Commands)
            {
                var rect = command.Rect;
                switch (command.Kind)
                {
                    case GuiCommandKind.Fill:
                        spriteBatch.Draw(pixel, rect, command.Colour);
                        break;

                    case GuiCommandKind.Outline:
                        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), command.Colour);
                        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), command.Colour);
                        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), command.Colour);
                        spriteBatch.Draw(pixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), command.Colour);
                        break;

                    case GuiCommandKind.Text:
                        spriteBatch.DrawString(gui.Font, command.Text, rect.Location.ToVector2(), command.Colour);
                        break;
                }
            }
        }
    }
}
