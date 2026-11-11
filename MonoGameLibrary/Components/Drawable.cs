using Microsoft.Xna.Framework;
using MonoGameLibrary.Components.Infrastructure;
using System;

namespace MonoGameLibrary.Components
{
    /// <param name="modelId">An id from <see cref="Graphics.ModelLibrary"/>.</param>
    /// <param name="tint">Multiplied into every tinted box of the model, e.g. a faction colour. White leaves the model as authored.</param>
    public struct Drawable(int modelId, Color tint) : IDependsOnOtherComponents
    {
        public int ModelId { get; set; } = modelId;
        public Color Tint { get; set; } = tint;

        public readonly Type[] GetComponentDependencies => [typeof(Position)];
    }
}
