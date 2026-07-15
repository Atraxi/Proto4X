using MonoGameLibrary.Archetypes;
using System;

namespace MonoGameLibrary.Components.Infrastructure
{
    public interface IComponentContainer
    {
        internal void WriteComponentByIndex(Archetype archetype, int entityIndex);
        internal void CreateComponentColumn(Archetype archetype, int columnIndex, int initialCapacity);
        Type GetComponentType();
    }

    public class ComponentContainerDetached<T>(T payload) : IComponentContainer where T : struct
    {
        protected readonly T Payload = payload;

        public T Component => Payload;

        public Type GetComponentType() => typeof(T);

        void IComponentContainer.CreateComponentColumn(Archetype archetype, int columnIndex, int initialCapacity)
        {
            archetype.CreateComponentColumn<T>(columnIndex, initialCapacity);
        }

        void IComponentContainer.WriteComponentByIndex(Archetype archetype, int entityIndex)
        {
            archetype.WriteComponentByIndex(Payload, entityIndex);
        }
    }

    internal class ComponentContainerAttached<T>(T payload, Archetype archetype) : ComponentContainerDetached<T>(payload) where T : struct
    {
        public void WriteComponentById(int entityId, T newPayload)
        {
            archetype.WriteComponentById(newPayload, entityId);
        }
    }
}
