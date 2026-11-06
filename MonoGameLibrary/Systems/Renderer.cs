using Microsoft.Xna.Framework;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Systems.Infrastructure;
using System.Collections.Generic;

namespace MonoGameLibrary.Systems
{
    /// <summary>Draws every <see cref="Drawable"/> as its 3D model, standing on the ground at its position and turned by its rotation about z.</summary>
    public class Renderer : IGeometryDrawSystem
    {
        public IReadOnlyList<ComponentRef> RequiredComponentProviders => [ComponentRef.Of<Position>(), ComponentRef.Of<Drawable>()];

        public void Draw(GeometryBatch batch, in GeometryDrawContext context, List<ArchetypeChunk> archetypeChunks)
        {
            foreach (var archetypeChunk in archetypeChunks)
            {
                for (var entityIndex = 0; entityIndex < archetypeChunk.EntityCount; entityIndex++)
                {
                    ref var drawable = ref archetypeChunk.Get<Drawable>(entityIndex);
                    ref var position = ref archetypeChunk.Get<Position>(entityIndex);

                    var model = ModelLibrary.Get(drawable.ModelId);
                    if (!context.LocalFrustum.Intersects(new BoundingSphere(new Vector3(position.Location, 0f), model.Radius)))
                    {
                        continue;
                    }

                    var transform = Matrix.CreateRotationZ(position.Rotation) * Matrix.CreateTranslation(position.Location.X, position.Location.Y, 0f);
                    ModelMesher.EmitModel(batch.Immediate, GeometryLayer.Opaque, model, transform, drawable.Tint);
                }
            }
        }
    }
}
