// The Materials button picture: two stacked metal bars as a white shape, drawn once in code. The inventory
// tints its button icons gold through the Image colour, so a white shape gets the same look as the others.

using UnityEngine;

namespace MaterialTracker
{
    internal static class BarIcon
    {
        private const int Size = 128;
        private static Sprite s_sprite;

        internal static Sprite Get()
        {
            if (s_sprite != null)
                return s_sprite;

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
                    // 4x4 samples per pixel for smooth slanted edges.
                    int hits = 0;
                    for (int sy = 0; sy < 4; sy++)
                    {
                        for (int sx = 0; sx < 4; sx++)
                        {
                            if (Inside(x + (sx + 0.5f) / 4f, y + (sy + 0.5f) / 4f))
                                hits++;
                        }
                    }
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(hits * 255 / 16));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            s_sprite = Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            s_sprite.name = "MaterialTracker_Bars";
            return s_sprite;
        }

        // Bottom bar, a gap, and a smaller bar on top: each a trapezoid, wider at the bottom (y = 0 is the bottom).
        private static bool Inside(float x, float y) =>
            Trapezoid(x, y, 22f, 54f, 12f, 116f, 26f, 102f) ||
            Trapezoid(x, y, 62f, 94f, 28f, 100f, 40f, 88f);

        private static bool Trapezoid(float x, float y, float bottom, float top, float bottomLeft, float bottomRight,
            float topLeft, float topRight)
        {
            if (y < bottom || y > top)
                return false;
            float t = (y - bottom) / (top - bottom);
            float left = Mathf.Lerp(bottomLeft, topLeft, t);
            float right = Mathf.Lerp(bottomRight, topRight, t);
            return x >= left && x <= right;
        }
    }
}
