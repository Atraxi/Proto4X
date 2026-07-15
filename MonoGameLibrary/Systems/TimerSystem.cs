using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using System.Collections.Generic;

namespace MonoGameLibrary.Systems
{
    public class TimerSystem : SystemBase, IUpdateSystem
    {
        public override ComponentTypeMask RequiredComponentProviders => ComponentTypeMask.FromTypes(
            typeof(Timer)
            );

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            var elapsedTime = gameTime.ElapsedGameTime;

            foreach (var archetypeChunk in archetypeChunks)
            {

                foreach(ref var timer in archetypeChunk.GetSpan<Timer>())
                {
                    if (timer.HasJustElapsed)
                    {
                        timer.HasJustElapsed = false;
                    }
                    if (timer.Remaining > elapsedTime.Ticks)
                    {
                        timer.Remaining -= elapsedTime.Ticks;

                        if (timer.Remaining <= 0f)
                        {
                            timer.HasJustElapsed = true;

                            if (timer.ShouldRepeat)
                            {
                                timer.Remaining = timer.Duration;
                            }
                        }
                    }
                }
            }
        }
    }
}
