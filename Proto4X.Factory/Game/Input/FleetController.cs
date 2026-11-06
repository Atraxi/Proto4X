using Microsoft.Xna.Framework;
using MonoGameLibrary.Input;
using MonoGameLibrary.UI;
using Proto4X.Factory.Simulation;
using Proto4X.Factory.Simulation.Space;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Proto4X.Factory.Input
{
    /// <summary>
    /// RTS fleet control over the space layer, as in the Sandbox (and every RTS since Dune II): left
    /// click or drag a box to select your ships, right click to move them there. Ships hold the new
    /// spot and defend it (AoE defensive stance). Orders go out as <see cref="CommandKind.MoveShips"/>.
    /// </summary>
    public sealed class FleetController(Universe universe, CameraBase camera)
    {
        private const float PickRadius = 3f;
        private const int DragThreshold = 6;

        private Vector2? dragStart;

        public HashSet<int> Selected { get; } = [];

        /// <summary>The selection box being dragged, in screen pixels, for the overlay.</summary>
        public Rectangle? DragBox { get; private set; }

        public void Update(MouseInfo mouse, bool mouseOverUi)
        {
            // Drop ships that died.
            Selected.IntersectWith(universe.Space.Snapshot.Select(v => v.Serial));

            var position = mouse.Position.ToVector2();
            if (mouse.WasButtonJustPressed(MouseButton.Left) && !mouseOverUi)
            {
                dragStart = position;
            }

            if (dragStart is { } start)
            {
                var box = new Rectangle((int)MathF.Min(start.X, position.X), (int)MathF.Min(start.Y, position.Y),
                    (int)MathF.Abs(position.X - start.X), (int)MathF.Abs(position.Y - start.Y));
                DragBox = box.Width > DragThreshold || box.Height > DragThreshold ? box : null;
                if (mouse.WasButtonJustReleased(MouseButton.Left))
                {
                    Select(DragBox, position);
                    dragStart = null;
                    DragBox = null;
                }
            }

            if (mouse.WasButtonJustPressed(MouseButton.Right) && !mouseOverUi && Selected.Count > 0)
            {
                var target = camera.ScreenToGround(position);
                universe.Enqueue(new Command(CommandKind.MoveShips, -1, (int)MathF.Floor(target.X), (int)MathF.Floor(target.Y), Payload: Selected.ToArray()));
            }
        }

        private void Select(Rectangle? box, Vector2 click)
        {
            Selected.Clear();
            var ships = universe.Space.Snapshot.Where(v => v.Faction == Faction.Player && v.Role == BodyRole.Ship);
            if (box is { } rect)
            {
                foreach (var ship in ships)
                {
                    var screen = camera.GroundToScreen(ship.Position);
                    if (rect.Contains((int)screen.X, (int)screen.Y))
                    {
                        Selected.Add(ship.Serial);
                    }
                }

                return;
            }

            var ground = camera.ScreenToGround(click);
            var nearest = ships.Select(s => (s.Serial, Distance: Vector2.Distance(s.Position, ground))).Where(p => p.Distance < PickRadius).OrderBy(p => p.Distance).FirstOrDefault();
            if (nearest.Serial != 0)
            {
                Selected.Add(nearest.Serial);
            }
        }

        /// <summary>Selects every player ship (Ctrl+A in most RTSes).</summary>
        public void SelectAll()
        {
            Selected.Clear();
            Selected.UnionWith(universe.Space.Snapshot.Where(v => v.Faction == Faction.Player && v.Role == BodyRole.Ship).Select(v => v.Serial));
        }
    }
}
