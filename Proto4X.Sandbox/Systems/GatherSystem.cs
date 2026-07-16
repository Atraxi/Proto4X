using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Sandbox.Components;
using System.Collections.Generic;

namespace Proto4X.Sandbox.Systems
{
    // Resource nodes (Position+Gatherable) and the player (Position+Motion) are different
    // archetypes, and a system only ever gets chunks matching its own RequiredComponentProviders,
    // so there's no built-in way to join the two within one Update call. Reads the player's
    // Position directly through GameWorld.GetComponentsForEntity instead, the same single-entity
    // random-access path UserInterfaceManager already uses for the WASD-selected entity.
    public class GatherSystem(int playerEntityId, float gatherRadius) : SystemBase, IUpdateSystem
    {
        public int Stockpile { get; private set; }

        public override ComponentTypeMask RequiredComponentProviders => ComponentTypeMask.FromTypes(
            typeof(Position),
            typeof(Gatherable)
            );

        public void Update(GameTime gameTime, List<ArchetypeChunk> archetypeChunks)
        {
            var world = Core.Instance!.World;
            if (!world.HasEntity(playerEntityId))
            {
                return;
            }

            var playerComponents = world.GetComponentsForEntity(playerEntityId);
            if (playerComponents[typeof(Position)] is not ComponentContainerDetached<Position> playerPositionContainer)
            {
                return;
            }
            var playerLocation = playerPositionContainer.Component.Location;

            var gatherRadiusSquared = gatherRadius * gatherRadius;

            foreach (var archetypeChunk in archetypeChunks)
            {
                var positionSpan = archetypeChunk.GetSpan<Position>();
                var gatherableSpan = archetypeChunk.GetSpan<Gatherable>();
                for (var i = 0; i < archetypeChunk.EntityCount; i++)
                {
                    if (Vector2.DistanceSquared(positionSpan[i].Location, playerLocation) <= gatherRadiusSquared)
                    {
                        Stockpile += gatherableSpan[i].Amount;
                        archetypeChunk.QueueEntityForRemovalByIndex(i);
                    }
                }
            }
        }
    }
}
