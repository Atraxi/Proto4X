using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using System.Collections.Generic;

namespace Proto4X.Sandbox.Systems
{
    public class PlaceholderSystem : SystemBase, IUpdateSystem, IDrawSystem
    {
        public override ComponentTypeMask RequiredComponentProviders => default;

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle viewport, List<ArchetypeChunk> archetypeChunks)
        {
        }
    }
}
