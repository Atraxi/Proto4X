using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Archetypes;
using MonoGameLibrary.Components.Infrastructure;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.UI;
using MonoGameLibrary.Utils;
using MonoGameLibrary.World;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonoGameLibrary.Systems.Infrastructure
{
    public class SystemScheduler
    {
        private readonly Dictionary<GameWorld, List<IUpdateSystem>> _updateSystems = [];
        private readonly Dictionary<GameWorld, List<IDrawSystem>> _drawWorldSystems = [];
        private readonly Dictionary<GameWorld, List<IDrawSystem>> _drawScreenSystems = [];
        private readonly Dictionary<GameWorld, List<IGeometryDrawSystem>> _geometrySystems = [];

        private readonly Dictionary<ISystem, List<ArchetypeChunk>> _cachedComponentArrays = [];

        /// <summary>The batch every world's geometry pass fills and flushes. Set its texture (atlas) and sampler once.</summary>
        public GeometryBatch Geometry { get; } = new();

        public void Add(GameWorld gameLayer, IUpdateSystem system)
        {
            _updateSystems.AddToInternalList(gameLayer, system);
        }

        /// <summary>Geometry systems draw in registration order, before the world's SpriteBatch systems.</summary>
        public void Add(GameWorld gameLayer, IGeometryDrawSystem system)
        {
            _geometrySystems.AddToInternalList(gameLayer, system);
        }

        public void Add(GameWorld gameLayer, IDrawSystem system)
        {
            switch (system.DrawSpace)
            {
                case DrawSpace.World:
                    _drawWorldSystems.AddToInternalList(gameLayer, system);
                    break;
                case DrawSpace.Screen:
                    _drawScreenSystems.AddToInternalList(gameLayer, system);
                    break;
                default:
                    throw new ArgumentException("The SystemScheduler only has support for screen space and world map camera transforms, other DrawSpaces are not supported", nameof(system));
            }
        }

        public void Update(GameWorld gameLayer, GameTime gameTime)
        {
            foreach (var system in _updateSystems[gameLayer])
            {
                var archetypeChunks = _cachedComponentArrays.GetValueOrDefault(system, gameLayer.QueryRelevantComponentArrays(ComponentTypeMask.FromTypes([.. system.RequiredComponentProviders.Select(componentProvider => componentProvider.Value)])));
                system.Update(gameTime, archetypeChunks);
            }

            foreach (var archetypesKeyPair in gameLayer.Archetypes)
            {
                archetypesKeyPair.Value.ProcessDeferredUpdates();
            }
        }

        public void Draw(SpriteBatch spriteBatch, UserInterfaceManager userInterfaceManager, GameWorld gameLayer)
        {
            Draw(spriteBatch, userInterfaceManager, gameLayer, Matrix.Identity);
        }

        /// <summary>
        /// Draws one world: its geometry pass, then its SpriteBatch world pass, then its screen pass.
        /// <paramref name="worldTransform"/> maps the world's local coordinates into camera world
        /// space (e.g. a moving ship's frame), and applies to both world passes.
        /// </summary>
        /// <remarks>
        /// A world without systems of one kind just skips that pass - previously a world needed at
        /// least one world and one screen system or this threw.
        /// </remarks>
        public void Draw(SpriteBatch spriteBatch, UserInterfaceManager userInterfaceManager, GameWorld gameLayer, Matrix worldTransform)
        {
            var camera = userInterfaceManager.Camera;
            //TODO if I ever swap to some sort of skewed/orthographic perspective then I may not be able to keep AABB, in the worst case that could become a massive region
            var cullingBounds = userInterfaceManager.GetVisibleLocalBounds(worldTransform);

            if (_geometrySystems.ContainsKey(gameLayer))
            {
                Geometry.Clear();
                CollectGeometry(gameLayer, Geometry, cullingBounds);
                Geometry.Flush(spriteBatch.GraphicsDevice, worldTransform, camera.GetViewMatrix(), camera.GetProjectionMatrix());
            }

            if (_drawWorldSystems.TryGetValue(gameLayer, out var worldSystems))
            {
                spriteBatch.Begin(transformMatrix: worldTransform * camera.GetCameraTransform());
                foreach (var worldSystem in worldSystems)
                {
                    var archetypeChunks = _cachedComponentArrays.GetValueOrDefault(worldSystem, gameLayer.QueryRelevantComponentArrays(ComponentTypeMask.FromTypes([.. worldSystem.RequiredComponentProviders.Select(componentProvider => componentProvider.Value)])));
                    worldSystem.Draw(spriteBatch, cullingBounds, archetypeChunks);
                }
                spriteBatch.End();
            }

            if (_drawScreenSystems.TryGetValue(gameLayer, out var screenSystems))
            {
                spriteBatch.Begin();
                foreach (var screenSystem in screenSystems)
                {
                    var archetypeChunks = _cachedComponentArrays.GetValueOrDefault(screenSystem, gameLayer.QueryRelevantComponentArrays(ComponentTypeMask.FromTypes([.. screenSystem.RequiredComponentProviders.Select(componentProvider => componentProvider.Value)])));
                    screenSystem.Draw(spriteBatch, userInterfaceManager.GetViewport(), archetypeChunks);
                }
                spriteBatch.End();
            }
        }

        /// <summary>
        /// Runs a world's geometry systems into <paramref name="batch"/> without touching the GPU -
        /// the half of the geometry pass a headless renderer or a test can call.
        /// </summary>
        public void CollectGeometry(GameWorld gameLayer, GeometryBatch batch, Rectangle localCullingBounds)
        {
            if (!_geometrySystems.TryGetValue(gameLayer, out var geometrySystems))
            {
                return;
            }

            foreach (var geometrySystem in geometrySystems)
            {
                var archetypeChunks = _cachedComponentArrays.GetValueOrDefault(geometrySystem, gameLayer.QueryRelevantComponentArrays(ComponentTypeMask.FromTypes([.. geometrySystem.RequiredComponentProviders.Select(componentProvider => componentProvider.Value)])));
                geometrySystem.Draw(batch, localCullingBounds, archetypeChunks);
            }
        }
    }
}
