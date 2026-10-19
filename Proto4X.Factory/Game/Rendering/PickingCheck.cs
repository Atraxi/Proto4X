using Microsoft.Xna.Framework;
using MonoGameLibrary.Systems.Infrastructure;
using Proto4X.Factory.Input;
using System;

namespace Proto4X.Factory.Rendering
{
    /// <summary>
    /// Picking, verified without a window: every tile centre on screen is projected through the
    /// style's world transform and camera, then sent back through the exact conversion the build
    /// controller uses (<see cref="BuildController.ScreenToLocal"/>). It must land in the same tile.
    /// Covers the planet, a close and a far zoom, and the turned, moving ship.
    /// </summary>
    public static class PickingCheck
    {
        public static int Run()
        {
            var style = new RenderStyle();
            var scheduler = new SystemScheduler();
            var universe = FactoryScene.Create(scheduler, out _, out _);
            for (var t = 0; t < 900; t++)
            {
                universe.Step();
            }

            var camera = style.CreateCamera();
            camera.Resize(new Rectangle(0, 0, 1280, 720));
            var failures = 0;
            (int Surface, Vector2 Tile, float Zoom, float Rotation)[] views =
            [
                (0, new Vector2(15f, 8.5f), 1.25f, 0f),
                (0, new Vector2(60f, 40f), 0.3f, 0f),
                (1, new Vector2(11.5f, 8f), 1.9f, 0.55f),
            ];

            foreach (var (surfaceIndex, tile, zoom, rotation) in views)
            {
                var surface = universe.Surfaces[surfaceIndex];
                style.AimCamera(camera, surface, tile, zoom, rotation);
                style.PrepareFrame(camera, surface);
                var world = style.WorldTransform(surface);
                var checkedTiles = 0;
                var wrong = 0;
                for (var y = 0; y < surface.Grid.Height; y++)
                {
                    for (var x = 0; x < surface.Grid.Width; x++)
                    {
                        var point = Vector3.Transform(new Vector3(x + 0.5f, y + 0.5f, 0f), world);
                        var screen = camera.WorldToScreen(new Vector2(point.X, point.Y));
                        if (screen.X < 2 || screen.Y < 2 || screen.X > camera.Viewport.Width - 2 || screen.Y > camera.Viewport.Height - 2)
                        {
                            continue;
                        }

                        checkedTiles++;
                        var back = BuildController.ScreenToLocal(camera, surface, screen);
                        if ((int)MathF.Floor(back.X) != x || (int)MathF.Floor(back.Y) != y)
                        {
                            if (wrong < 3)
                            {
                                Console.WriteLine($"   tile ({x},{y}) -> screen {screen} -> ({back.X:F3},{back.Y:F3})");
                            }

                            wrong++;
                        }
                    }
                }

                var ok = wrong == 0 && checkedTiles > 0;
                Console.WriteLine($"{(ok ? "ok  " : "FAIL")} {style.Name} picking on {surface.Name} at zoom {zoom}: {checkedTiles:N0} on-screen tiles round-trip, {wrong} wrong");
                failures += ok ? 0 : 1;
            }

            return failures;
        }
    }
}
