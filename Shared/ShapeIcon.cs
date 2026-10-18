// Shared by several mods (linked into each csproj): a white icon drawn in code from a shape, for buttons whose
// Image colour tints them (like the inventory's button row, tinted gold).

using System;
using UnityEngine;

namespace Koendertjh.Shared
{
    internal static class ShapeIcon
    {
        private const int Size = 128;

        /// <summary>
        /// A 128x128 white sprite: <paramref name="inside"/> gets x and y in pixels (y = 0 at the bottom) and says
        /// whether that point is part of the shape. Edges are smoothed with 4x4 samples per pixel.
        /// </summary>
        internal static Sprite Create(string name, Func<float, float, bool> inside)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < 4; sy++)
                    {
                        for (int sx = 0; sx < 4; sx++)
                        {
                            if (inside(x + (sx + 0.5f) / 4f, y + (sy + 0.5f) / 4f))
                                hits++;
                        }
                    }
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(hits * 255 / 16));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        internal static bool Trapezoid(float x, float y, float bottom, float top, float bottomLeft, float bottomRight,
            float topLeft, float topRight)
        {
            if (y < bottom || y > top)
                return false;
            float t = (y - bottom) / (top - bottom);
            return x >= Mathf.Lerp(bottomLeft, topLeft, t) && x <= Mathf.Lerp(bottomRight, topRight, t);
        }

        internal static bool Ellipse(float x, float y, float cx, float cy, float rx, float ry)
        {
            float dx = (x - cx) / rx;
            float dy = (y - cy) / ry;
            return dx * dx + dy * dy <= 1f;
        }
    }
}
