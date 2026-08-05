using MonoGameLibrary.Components.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MonoGameLibrary.Archetypes
{
    internal class Archetype : IArchetype
    {
        private int[] DenseIndexToEntityId;
        private readonly Dictionary<int, int> EntityIdToDenseIndex = [];

        public readonly ComponentTypeMask ComponentMask;

        public int Count => count;
        private int count;

        private readonly List<int> DeferredEntityRemovalsByIndex = [];
        private readonly List<EntityBuilder> DeferredAdditions = [];

        private readonly Dictionary<int, int> componentTypeIdToComponentArrayIndex = [];
        private readonly IComponentColumn[] componentArrays;

        public Archetype(int initialCapacity, EntityBuilder entityBuilder)
        {
            DenseIndexToEntityId = new int[initialCapacity];
            ComponentMask = entityBuilder.ComponentTypeMask;
            
            componentArrays = new IComponentColumn[entityBuilder.Components.Count];
            for (int componentsIndex = 0; componentsIndex < entityBuilder.Components.Count; componentsIndex++)
            {
                entityBuilder.Components[componentsIndex].CreateComponentColumn(this, componentsIndex, initialCapacity);
            }

            QueueEntityForAddition(entityBuilder);
        }

        internal void CreateComponentColumn<T>(int componentsIndex, int initialCapacity) where T : struct
        {
            componentTypeIdToComponentArrayIndex[ComponentTypeRegistry.GetId(typeof(T))] = componentsIndex;
            componentArrays[componentsIndex] = new ComponentColumn<T>(new T[initialCapacity]);
        }

        private void AddEntity(EntityBuilder componentPayloads)
        {
            int entityId = EntityIdRegistry.GetNextFreeId();
            int entityIndex = count++;

            if (entityIndex == DenseIndexToEntityId.Length)
            {
                GrowEntityCapacity();
            }

            DenseIndexToEntityId[entityIndex] = entityId;
            EntityIdToDenseIndex[entityId] = entityIndex;

            foreach (var payload in componentPayloads.Components)
            {
                payload.WriteComponentByIndex(this, entityIndex);
            }
        }

        /// <summary>
        /// This method exists to allow an entity to provide an arbitrary number of components, while also getting around a generic type erasure boundary enforced by the necessity of said array of components of arbitrary value types
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="componentData"></param>
        internal void WriteComponentById<T>(T componentData, int entityId) where T : struct
        {
            WriteComponentByIndex(componentData, EntityIdToDenseIndex[entityId]);
        }

        /// <summary>
        /// This method exists to allow an entity to provide an arbitrary number of components, while also getting around a generic type erasure boundary enforced by the necessity of said array of components of arbitrary value types
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="componentData"></param>
        internal void WriteComponentByIndex<T>(T componentData, int entityIndex) where T : struct
        {
            Debug.Assert(entityIndex < count);
            var componentTypeId = ComponentTypeRegistry.GetId(componentData.GetType());
            int componentsIndex = componentTypeIdToComponentArrayIndex[componentTypeId];

            //TODO: Would it be better to just cast and crash on a ClassCastException? This should be an impossible situation that would indicate a critical data corruption bug if it happens so a hard crash might be preferable?
            Debug.Assert(componentArrays[componentsIndex].GetType().IsAssignableTo(typeof(ComponentColumn<T>)));
            (componentArrays[componentsIndex] as ComponentColumn<T>)?.Values[entityIndex] = componentData;
        }

        internal ArchetypeChunk GetMatchingComponents(ComponentTypeMask queriedTypesMask)
        {
            return new ArchetypeChunk(componentArrays.Where(componentColumn => queriedTypesMask.ContainsType(componentColumn.GetComponentType()))
                    .ToDictionary(componentColumn => componentColumn.GetComponentType()),
                count,
                this);
        }

        private void RemoveEntityAtIndex(int index)
        {
            RemoveEntity(DenseIndexToEntityId[index], index); ;
        }

        private void RemoveEntity(int entityId, int index)
        {
            Debug.Assert(HasEntity(entityId));
            int last = --count;
    
            if (index != last)
            {
                int movedEntity = DenseIndexToEntityId[last];
                DenseIndexToEntityId[index] = movedEntity;
                EntityIdToDenseIndex[movedEntity] = index;
            }
            EntityIdToDenseIndex.Remove(entityId);

            foreach (var componentData in componentArrays)
            {
                componentData.RemoveAtIndexAndCopyLast(index, last);
            }
        }

        public bool HasEntity(int entityId) => EntityIdToDenseIndex.ContainsKey(entityId);

        public Dictionary<Type, IComponentContainer> GetComponentsForEntity(int entityId)
        {
            Debug.Assert(HasEntity(entityId));

            var result = new Dictionary<Type, IComponentContainer>();
            foreach (var componentData in componentArrays)
            {
                var data = componentData.GetComponentContainerForEntity(EntityIdToDenseIndex[entityId], this);
                result.Add(data.GetComponentType(), data);
            }
            return result;
        }

        public ref T GetComponentRefTForEntityId<T>(int entityId) where T : struct
        {
            Debug.Assert(HasEntity(entityId));

            return ref ((ComponentColumn<T>)componentArrays[componentTypeIdToComponentArrayIndex[ComponentTypeRegistry.GetId(typeof(T))]])
                .GetComponentRefForEntity(EntityIdToDenseIndex[entityId]);
        }

        public T GetComponentTForEntityId<T>(int entityId) where T : struct
        {
            Debug.Assert(HasEntity(entityId));

            return ((ComponentColumn<T>)componentArrays[componentTypeIdToComponentArrayIndex[ComponentTypeRegistry.GetId(typeof(T))]])
                .GetComponentRefForEntity(EntityIdToDenseIndex[entityId]);
        }

        public void QueueEntityForAddition(EntityBuilder entityData)
        {
            DeferredAdditions.Add(entityData);
        }

        public void QueueEntityForRemovalById(int entityId)
        {
            Debug.Assert(HasEntity(entityId));
            DeferredEntityRemovalsByIndex.Add(EntityIdToDenseIndex[entityId]);
        }

        public void QueueEntityForRemovalByIndex(int entityIndex)
        {
            DeferredEntityRemovalsByIndex.Add(entityIndex);
        }

        internal void ProcessDeferredUpdates()
        {
            DeferredEntityRemovalsByIndex.Sort((A, B) => B - A);
            foreach (var  entityIndex in DeferredEntityRemovalsByIndex)
            {
                RemoveEntityAtIndex(entityIndex);
            }
            DeferredEntityRemovalsByIndex.Clear();

            foreach (EntityBuilder payload in DeferredAdditions)
            {
                AddEntity(payload);
            }
            DeferredAdditions.Clear();
        }

        private void GrowEntityCapacity()
        {
            int newSize = DenseIndexToEntityId.Length * 2;

            Array.Resize(ref DenseIndexToEntityId, newSize);

            foreach(var componentColumn in componentArrays)
            {
                componentColumn.Grow(newSize);
            }
        }
    }
}
