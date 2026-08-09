using Microsoft.Xna.Framework;
using MonoGameLibrary.Components;
using MonoGameLibrary.Components.Infrastructure;
using System;

namespace Proto4X.Sandbox.Components
{
    public struct MoveOrder(Vector2? destination = null) : IDependsOnOtherComponents
    {
        public Vector2? Destination = destination;

        /// <summary>
        /// This unit is parked on purpose and must not be nudged around.
        /// </summary>
        /// <remarks>
        /// A null <c>Destination</c> was doing double duty: it means both "nothing is driving this
        /// ship" and "this ship arrived and is now doing its job". <c>SeparationSystem</c> only
        /// cares about the first meaning, and without this flag it happily shoved miners off the
        /// asteroids they were mining. Anything deliberately stationary sets this.
        /// </remarks>
        public bool IsHoldingPosition = false;

        public readonly Type[] GetComponentDependencies => [typeof(Position), typeof(Motion)];
    }
}
