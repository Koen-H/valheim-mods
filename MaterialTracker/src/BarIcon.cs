// The Materials button picture: two stacked metal bars, wider at the bottom (y = 0 is the bottom).

using Koendertjh.Shared;
using UnityEngine;

namespace MaterialTracker
{
    internal static class BarIcon
    {
        private static Sprite s_sprite;

        internal static Sprite Get() =>
            s_sprite != null
                ? s_sprite
                : s_sprite = ShapeIcon.Create("MaterialTracker_Bars", (x, y) =>
                    ShapeIcon.Trapezoid(x, y, 22f, 54f, 12f, 116f, 26f, 102f) ||
                    ShapeIcon.Trapezoid(x, y, 62f, 94f, 28f, 100f, 40f, 88f));
    }
}
