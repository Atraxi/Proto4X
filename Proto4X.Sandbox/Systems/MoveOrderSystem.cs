using Microsoft.Xna.Framework;
using MonoGameLibrary.Input;
using MonoGameLibrary.UI;
using MonoGameLibrary.Utils;
using MonoGameLibrary.World;
using Proto4X.Sandbox.Components;

namespace Proto4X.Sandbox.Systems
{
    public class MoveOrderSystem(GameWorld gameWorld, Camera camera, SelectionSystem selectionSystem)
    {
        public void Update()
        {
            var mouse = InputManager.Instance.MouseInfo;
            if (!mouse.WasButtonJustPressed(MouseButton.Right) || selectionSystem.SelectedEntityId is not int selectedEntityId)
            {
                return;
            }

            var targetLocation = mouse.Position.ToVector2().Transform(Matrix.Invert(camera.GetCameraTransform()));

            gameWorld.MutateComponent<MoveOrder>(selectedEntityId, (ref MoveOrder order) =>
            {
                order.Destination = targetLocation;
            });
        }
    }
}
