using System;
using UnityEngine;

namespace Kugon.BetterAnimationEvents
{
    [Serializable]
    public class KColor
    {
        public static KColor Default() => new KColor(new Color32(60, 60, 60, 255));
        public static KColor Gray() => new KColor(new Color32(60, 60, 60, 255));
        

        public Color32 Color;

        public KColor(Color32 color)
        {
            Color = color;
        }
        
        public static float GetLuminance(Color color)
        {
            return 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
        }
        
        public static Color GetReadableTextColor(Color bgColor)
        {
            float luminance = GetLuminance(bgColor);

            if (luminance > 0.3f)
                return UnityEngine.Color.black;
            else
                return UnityEngine.Color.white;
        }
        
        public Color32 Darker(float factor = 0.5f)
        {
            factor = Mathf.Clamp01(factor);

            Color32 c = Color;
            byte r = (byte)(c.r * factor);
            byte g = (byte)(c.g * factor);
            byte b = (byte)(c.b * factor);

            return new Color32(r, g, b, c.a);
        }
        
        public static bool AreColorsEqual(KColor a, KColor b, float tolerance = 0.01f)
        {
            return Mathf.Abs(a.Color.r - b.Color.r) < tolerance &&
                   Mathf.Abs(a.Color.g - b.Color.g) < tolerance &&
                   Mathf.Abs(a.Color.b - b.Color.b) < tolerance &&
                   Mathf.Abs(a.Color.a - b.Color.a) < tolerance;
        }

        public bool IsEqual(KColor compare, float tolerance = 0.01f)
        {
            return AreColorsEqual(this, compare, tolerance);
        }
    }
}