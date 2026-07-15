using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonoGameLibrary.World
{
    public class GameWorld(int xPos, int yPos, int width, int height)
    {
        internal readonly Dictionary<ComponentTypeMask, Archetype> Archetypes = [];
        public readonly Rectangle Bounds = new(xPos, yPos, width, height);

        internal List<ArchetypeChunk> QueryRelevantComponentArrays(ComponentTypeMask componentProviderTypes)
        {
            List<ArchetypeChunk> result = [];
            
            foreach (var archetypesKeyPair in Archetypes)
            {
                if (archetypesKeyPair.Key.ContainsAll(componentProviderTypes))
                {
                    var archetype = archetypesKeyPair.Value;
                    var archetypeChunk = archetype.GetMatchingComponents(componentProviderTypes);

                    result.Add(archetypeChunk);
                }
            }
            return result;
        }

        public void AddEntity(EntityBuilder entityBuilder)
        {
            Archetypes.TryGetValue(entityBuilder.ComponentTypeMask, out var archetype);
            if (archetype == null)
            {
                archetype = new Archetype(50, entityBuilder);
                Archetypes.Add(entityBuilder.ComponentTypeMask, archetype);
            }
            else
            {
                archetype.QueueEntityForAddition(entityBuilder);
            }
        }

        public void RemoveEntity(int entityId)
        {
            foreach (var archetype in Archetypes)
            {
                if(archetype.Value.HasEntity(entityId))
                {
                    archetype.Value.QueueEntityForRemovalById(entityId);
                }
            }
        }

        public bool HasEntity(int entityId)
        {
            return Archetypes.Any(archetype => archetype.Value.HasEntity(entityId));
        }

        public Dictionary<Type, IComponentContainer> GetComponentsForEntity(int entityId)
        {
            foreach (var archetype in Archetypes)
            {
                if(archetype.Value.HasEntity(entityId))
                {
                    return archetype.Value.GetComponentsForEntity(entityId);
                }
            }
            throw new InvalidOperationException($"An entity with the id {entityId} doesn't exist");
        }
    }
}