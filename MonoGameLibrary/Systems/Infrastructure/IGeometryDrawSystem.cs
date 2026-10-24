using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Graphics;
using System.Collections.Generic;

namespace MonoGameLibrary.Systems.Infrastructure
{
    /// <summary>
    /// A world-space draw system that emits triangles or retained meshes instead of sprites.
    /// Coordinates are the world's own local space: the scheduler applies the world's transform and
    /// the camera's view and projection on the GPU, so a system never transforms a vertex by hand.
    /// </summary>
    public interface IGeometryDrawSystem : ISystem
    {
        void Draw(GeometryBatch batch, in GeometryDrawContext context, List<ArchetypeChunk> archetypeChunks);
    }

    /// <summary>What a geometry system knows about this frame's view, all in the world's local space.</summary>
    /// <param name="LocalCullingBounds">
    /// The camera's visible ground footprint (z = 0) as an axis-aligned box of whole units. Anything
    /// taller than the ground can be visible from outside it, so systems drawing height pad it.
    /// </param>
    /// <param name="LocalFrustum">
    /// The camera's view volume. Test local-space bounding boxes against it directly - it was built
    /// from world × view × projection, so it already accounts for the world's transform.
    /// </param>
    /// <param name="WorldTransform">Local space to camera world space.</param>
    public readonly record struct GeometryDrawContext(Rectangle LocalCullingBounds, BoundingFrustum LocalFrustum, Matrix WorldTransform);
}
