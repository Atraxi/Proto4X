using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonoGameLibrary.Systems.Infrastructure
{
    public interface IEventSystem<TEvent> : ISystem where TEvent : struct
    {
        void ProcessEvents(GameTime gameTime, ReadOnlySpan<TEvent> events, List<ArchetypeChunk> archetypeChunks);
    }
}
