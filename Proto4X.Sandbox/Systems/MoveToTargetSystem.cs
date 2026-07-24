using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Sandbox.Components;
using System.Collections.Generic;

namespace Proto4X.Sandbox.Systems
{
    public class MoveToTargetSystem : SystemBase, IUpdateSystem
    {
        private const float Acceleration = 250f;
        private const float ArriveRadius = 6f;
        private const float SlowRadius = 80f;

        public override ComponentTypeMask RequiredComponentProviders => ComponentTypeMask.FromTypes(
            typeof(Position),
            typeof(Motion),
            typeof(MoveOrder)
            );

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            foreach (var archetypeChunk in archetypeChunks)
            {
                var positionSpan = archetypeChunk.GetSpan<Position>();
                var motionSpan = archetypeChunk.GetSpan<Motion>();
                var moveOrderSpan = archetypeChunk.GetSpan<MoveOrder>();

                for (var i = 0; i < archetypeChunk.EntityCount; i++)
                {
                    ref var moveOrder = ref moveOrderSpan[i];
                    ref var motion = ref motionSpan[i];

                    if (moveOrder.Destination is not Vector2 destination)
                    {
                        motion.Acceleration = Vector2.Zero;
                        continue;
                    }

                    ref var position = ref positionSpan[i];
                    var toTarget = destination - position.Location;
                    var distance = toTarget.Length();

                    if (distance <= ArriveRadius)
                    {
                        moveOrder.Destination = null;
                        motion.Acceleration = Vector2.Zero;
                        motion.Velocity = Vector2.Zero;
                        continue;
                    }

                    var speedScale = distance < SlowRadius ? distance / SlowRadius : 1f;
                    motion.Acceleration = toTarget / distance * Acceleration * speedScale;
                }
            }
        }
    }
}
