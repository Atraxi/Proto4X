using MonoGameLibrary.Components.Infrastructure;

namespace MonoGameLibrary.Systems.Infrastructure
{
    public abstract class SystemBase
    {
        public abstract ComponentTypeMask RequiredComponentProviders { get; }
    }
}
