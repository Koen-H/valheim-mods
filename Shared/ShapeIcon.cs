// Shared by several mods (linked into each csproj): an icon drawn in code from a shape, for buttons whose Image
// colour tints them (like the inventory's button row, tinted gold). The shape is greyscale with the look of the
// game's own icons: mottled, a bit darker to the right, a dark rim and an inner border line.

using System;
using UnityEngine;

namespace Koendertjh.Shared
{
    internal static class ShapeIcon
    {
        private const int Size = 128;
        private const int MaxDistance = 16;

        /// <summary>
        /// A 128x128 sprite: <paramref name="inside"/> gets x and y in pixels (y = 0 at the bottom) and says whether
        /// that point is part of the shape. Edges are smoothed with 4x4 samples per pixel.
        /// </summary>
        internal static Sprite Create(string name, Func<float, float, bool> inside)
        {
            var coverage = new float[Size * Size];
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
                    coverage[y * Size + x] = hits / 16f;
                }
            }

            var distance = new float[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                    distance[y * Size + x] = coverage[y * Size + x] > 0f ? DistanceToEdge(coverage, x, y) : 0f;
            }

            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float alpha = coverage[y * Size + x];
                    float shade = alpha > 0f ? Shade(distance, x, y) : 1f;
                    var grey = (byte)(Mathf.Clamp01(shade) * 255f);
                    pixels[y * Size + x] = new Color32(grey, grey, grey, (byte)(alpha * 255f));
                }
            }

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        private static float Shade(float[] distance, int x, int y)
        {
            // Mottled like painted metal, and a little darker to the right.
            float noise = Mathf.PerlinNoise(x * 0.07f + 3.1f, y * 0.07f + 7.7f) * 0.6f +
                          Mathf.PerlinNoise(x * 0.19f + 11.3f, y * 0.19f + 1.9f) * 0.4f;
            float shade = Mathf.Lerp(0.78f, 1f, noise) * Mathf.Lerp(1f, 0.86f, x / (float)Size);

            float d = distance[y * Size + x];
            if (d < 2.5f)
                shade *= Mathf.Lerp(0.55f, 1f, d / 2.5f);
            // The inner border line, only where the shape is wide enough for it.
            else if (d >= 6f && d < 8f && DeepNearby(distance, x, y))
                shade *= 0.62f;
            return shade;
        }

        // A point at least 13 px from the edge within 5 px: the part is wide enough for an inner line.
        private static bool DeepNearby(float[] distance, int x, int y)
        {
            for (int dy = -5; dy <= 5; dy++)
            {
                for (int dx = -5; dx <= 5; dx++)
                {
                    int px = x + dx;
                    int py = y + dy;
                    if (px >= 0 && py >= 0 && px < Size && py < Size && distance[py * Size + px] >= 13f)
                        return true;
                }
            }
            return false;
        }

        // Distance to the nearest mostly-outside pixel, up to a limit.
        private static float DistanceToEdge(float[] coverage, int x, int y)
        {
            float best = MaxDistance;
            for (int dy = -MaxDistance; dy <= MaxDistance; dy++)
            {
                for (int dx = -MaxDistance; dx <= MaxDistance; dx++)
                {
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d >= best)
                        continue;
                    int px = x + dx;
                    int py = y + dy;
                    bool outside = px < 0 || py < 0 || px >= Size || py >= Size || coverage[py * Size + px] < 0.5f;
                    if (outside)
                        best = d;
                }
            }
            return best;
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
