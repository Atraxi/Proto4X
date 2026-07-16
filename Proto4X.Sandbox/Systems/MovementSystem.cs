using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using System.Collections.Generic;

namespace Proto4X.Sandbox.Systems
{
    public class MovementSystem : SystemBase, IUpdateSystem
    {
        public const float Friction = 0.01f;

        public override ComponentTypeMask RequiredComponentProviders => ComponentTypeMask.FromTypes(
            typeof(Position),
            typeof(Motion)
            );

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            var elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            foreach (var archetypeChunk in archetypeChunks)
            {
                var positionSpan = archetypeChunk.GetSpan<Position>();
                var motionSpan = archetypeChunk.GetSpan<Motion>();
                for (var i = 0; i < archetypeChunk.EntityCount; i++)
                {
                    ref var position = ref positionSpan[i];
                    ref var motion = ref motionSpan[i];

                    motion.Velocity += motion.Acceleration * elapsedSeconds;
                    motion.Velocity *= 1 - Friction;
                    position.Location += motion.Velocity * elapsedSeconds;

                    motion.AngularVelocity += motion.AngularAcceleration * elapsedSeconds;
                    motion.AngularVelocity *= 1 - Friction;
                    position.Rotation += motion.AngularVelocity * elapsedSeconds;
                }
            }
        }
    }
}
