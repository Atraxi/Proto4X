using Microsoft.Xna.Framework;
using Proto4X.Factory.Simulation.Components;
using Proto4X.Factory.Simulation.Data;
using Proto4X.Factory.Simulation.Grid;
using Proto4X.Factory.Simulation.Power;
using System;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// Read-outs of machine state that drawing needs - how far through a swing or a craft something
    /// is - kept apart from any one renderer so every view agrees on them.
    /// </summary>
    public static class MachineState
    {
        /// <summary>How far through its swing an inserter is: 0 over the pickup side, 1 over the drop side.</summary>
        public static float SwingFraction(in Inserter inserter)
        {
            const float swingUnits = Buildings.InserterSwingTicks * PowerGrid.Full;
            var t = inserter.Phase switch
            {
                InserterPhase.WaitingForPickup => 0f,
                InserterPhase.SwingingToDrop => inserter.Progress / swingUnits,
                InserterPhase.WaitingToDrop => 1f,
                _ => 1f - inserter.Progress / swingUnits,
            };
            return Math.Clamp(t, 0f, 1f);
        }

        /// <summary>Where the hand is, in grid-local tiles: a half-circle sweep from behind to in front.</summary>
        public static Vector2 Hand(in Placement placement, in Inserter inserter)
        {
            var centre = new Vector2(placement.X + 0.5f, placement.Y + 0.5f);
            var forward = SurfaceView.DirectionVector(placement.Direction);
            var side = SurfaceView.DirectionVector(placement.Direction.Left());
            var angle = MathF.PI * SwingFraction(inserter);
            return centre + (-forward * MathF.Cos(angle) + side * MathF.Sin(angle) * 0.6f) * 0.75f;
        }

        public static float CraftFraction(in Crafter crafter) =>
            Math.Clamp(crafter.Progress / (float)(Recipes.Get(crafter.Recipe).Ticks * PowerGrid.Full), 0f, 1f);

        public static float LaunchFraction(in Launcher launcher) =>
            Math.Clamp(launcher.Progress / (float)(Buildings.LaunchTicks * PowerGrid.Full), 0f, 1f);
    }
}
