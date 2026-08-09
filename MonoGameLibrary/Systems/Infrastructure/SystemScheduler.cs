using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Components.Infrastructure;
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

        public void Add(GameWorld gameLayer, IUpdateSystem system)
        {
            _updateSystems.AddToInternalList(gameLayer, system);
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
                //TODO maybe we can cache these? type lookups every frame/update are not cheap but not brutal
                var archetypeChunks = gameLayer.QueryRelevantComponentArrays(ComponentTypeMask.FromTypes([.. system.RequiredComponentProviders.Select(componentProvider => componentProvider.Value)]));
                system.Update(gameTime, archetypeChunks);
            }

            foreach (var archetypesKeyPair in gameLayer.Archetypes)
            {
                archetypesKeyPair.Value.ProcessDeferredUpdates();
            }
        }

        public void Draw(SpriteBatch spriteBatch, UserInterfaceManager userInterfaceManager, GameWorld gameLayer)
        {
            spriteBatch.Begin(transformMatrix: userInterfaceManager.Camera.GetCameraTransform());
            //TODO if I ever swap to some sort of skewed/orthographic perspective then I may not be able to keep AABB, in the worst case that could become a massive region
            var cullingBounds = userInterfaceManager.GetVisibleScreenBoundsAABB();
            foreach (var worldSystem in _drawWorldSystems[gameLayer])
            {
                //TODO maybe we can cache these? type lookups every frame/update are not cheap but not brutal
                var archetypeChunks = gameLayer.QueryRelevantComponentArrays(ComponentTypeMask.FromTypes([.. worldSystem.RequiredComponentProviders.Select(componentProvider => componentProvider.Value)]));
                worldSystem.Draw(spriteBatch, cullingBounds, archetypeChunks);
            }
            spriteBatch.End();

            spriteBatch.Begin();
            foreach (var screenSystem in _drawScreenSystems[gameLayer])
            {
                //TODO maybe we can cache these? type lookups every frame/update are not cheap but not brutal
                var archetypeChunks = gameLayer.QueryRelevantComponentArrays(ComponentTypeMask.FromTypes([.. screenSystem.RequiredComponentProviders.Select(componentProvider => componentProvider.Value)]));
                screenSystem.Draw(spriteBatch, userInterfaceManager.GetViewport(), archetypeChunks);
            }
            spriteBatch.End();
        }
    }
}
