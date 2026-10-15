using System;
using UnityEngine;

namespace UI.Bubble.Utils
{
    public static class Math
    {
        public static void TimesDo(this uint input, Action action)
        {
            for (var i = 0; i < input; i++) action();
        }

        public static Vector2 MultiplyBy(this Vector2 vec, float factor)
        {
            return new Vector2(vec.x * factor, vec.y * factor);
        }

        public static Vector2 ClampVector2(this Vector2 vec, float minX, float maxX, float minY, float maxY)
        {
            return new Vector2(Mathf.Clamp(vec.x, minX, maxX), Mathf.Clamp(vec.y, minY, maxY));
        }
    }
}