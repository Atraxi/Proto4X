using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace MonoGameLibrary.Graphics
{
    /// <summary>One axis-aligned box of a model: x forward, y right, z up (model space).</summary>
    /// <param name="Emissive">Drawn at full brightness, unlit - glow panels, screens.</param>
    /// <param name="Tinted">Multiplied by the instance's tint. Off for parts that keep their own colour whatever the owner: engine glow, canopies.</param>
    public readonly record struct ModelBox(Vector3 Min, Vector3 Max, Color Colour, bool Emissive = false, bool Tinted = true);

    /// <summary>A model as a handful of coloured boxes, modelled facing +x (east on the ground).</summary>
    public class Model3D(ModelBox[] boxes)
    {
        public ModelBox[] Boxes { get; } = boxes;

        public float Height { get; } = boxes.Length == 0 ? 0f : MaxOf(boxes, box => box.Max.Z);

        /// <summary>Nose to tail, in model units.</summary>
        public float Length { get; } = boxes.Length == 0 ? 0f : MaxOf(boxes, box => box.Max.X) - MinOf(boxes, box => box.Min.X);

        /// <summary>Distance from the model origin to its farthest corner: a bounding sphere for culling.</summary>
        public float Radius { get; } = boxes.Length == 0 ? 0f : MaxOf(boxes, box => new Vector3(
            MathF.Max(MathF.Abs(box.Min.X), MathF.Abs(box.Max.X)),
            MathF.Max(MathF.Abs(box.Min.Y), MathF.Abs(box.Max.Y)),
            MathF.Max(MathF.Abs(box.Min.Z), MathF.Abs(box.Max.Z))).Length());

        private static float MaxOf(ModelBox[] boxes, Func<ModelBox, float> value)
        {
            var best = float.MinValue;
            foreach (var box in boxes)
            {
                best = MathF.Max(best, value(box));
            }

            return best;
        }

        private static float MinOf(ModelBox[] boxes, Func<ModelBox, float> value)
        {
            var best = float.MaxValue;
            foreach (var box in boxes)
            {
                best = MathF.Min(best, value(box));
            }

            return best;
        }
    }

    /// <summary>Every model the game uses, by the id a <see cref="Components.Drawable"/> carries.</summary>
    public static class ModelLibrary
    {
        private static readonly List<Model3D> models = [];

        public static int Add(Model3D model)
        {
            models.Add(model);
            return models.Count - 1;
        }

        public static Model3D Get(int id) => models[id];
    }
}
