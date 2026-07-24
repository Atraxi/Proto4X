using Microsoft.Xna.Framework;
using MonoGameLibrary.Components;
using MonoGameLibrary.Input;
using MonoGameLibrary.UI;
using MonoGameLibrary.Utils;
using MonoGameLibrary.World;
using System.Collections.Generic;

namespace Proto4X.Sandbox.Systems
{
    // Selectable entity ids are known up front (see Sandbox.OnInitialize) and checked directly via
    // GameWorld.TryGetComponent rather than scanning an archetype chunk - there's currently no way
    // to recover an entity's id from a chunk index during iteration, so this can't yet generalize to
    // "whichever ships happen to exist" without that id. Logged in ENGINE_REQUESTS.md.
    public class SelectionSystem(GameWorld gameWorld, Camera camera, IReadOnlyList<int> selectableEntityIds, float pickRadius)
    {
        public int? SelectedEntityId { get; private set; }

        public void Update()
        {
            var mouse = InputManager.Instance.MouseInfo;
            if (!mouse.WasButtonJustPressed(MouseButton.Left))
            {
                return;
            }

            var clickLocation = mouse.Position.ToVector2().Transform(Matrix.Invert(camera.GetCameraTransform()));
            var pickRadiusSquared = pickRadius * pickRadius;

            int? nearestEntityId = null;
            var nearestDistanceSquared = float.MaxValue;
            foreach (var entityId in selectableEntityIds)
            {
                if (!gameWorld.TryGetComponent<Position>(entityId, out var position))
                {
                    continue;
                }

                var distanceSquared = Vector2.DistanceSquared(position.Location, clickLocation);
                if (distanceSquared <= pickRadiusSquared && distanceSquared < nearestDistanceSquared)
                {
                    nearestEntityId = entityId;
                    nearestDistanceSquared = distanceSquared;
                }
            }

            SelectedEntityId = nearestEntityId;
        }
    }
}
