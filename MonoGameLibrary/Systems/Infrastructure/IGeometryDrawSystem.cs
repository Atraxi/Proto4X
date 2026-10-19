using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;

namespace MonoGameLibrary.Systems.Infrastructure
{
    /// <summary>
    /// A world-space draw system that emits triangles instead of sprites. Coordinates are the
    /// world's own local space: the scheduler applies the world's transform and the camera's view
    /// and projection on the GPU, so a system never transforms a vertex by hand.
    /// </summary>
    /// <remarks>
    /// <paramref name="localCullingBounds"/> in <see cref="Draw"/> is the camera's visible ground
    /// footprint (z = 0) mapped into that local space, as an axis-aligned box. Anything taller than
    /// the ground plane can be visible from outside it, so systems drawing height pad it themselves.
    /// </remarks>
    public interface IGeometryDrawSystem : ISystem
    {
        void Draw(GeometryBatch batch, Rectangle localCullingBounds, List<ArchetypeChunk> archetypeChunks);
    }
}
