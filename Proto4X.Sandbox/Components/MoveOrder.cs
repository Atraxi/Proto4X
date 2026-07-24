using Microsoft.Xna.Framework;
using MonoGameLibrary.Components;
using MonoGameLibrary.Components.Infrastructure;
using System;

namespace Proto4X.Sandbox.Components
{
    public struct MoveOrder(Vector2? destination = null) : IDependsOnOtherComponents
    {
        public Vector2? Destination = destination;

        public readonly Type[] GetComponentDependencies => [typeof(Position), typeof(Motion)];
    }
}
