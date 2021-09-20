﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace JERC.Constants
{
    public static class SolidBrushColours
    {
        //public static SolidBrush SolidBrushRemove() => new SolidBrush(Color.Transparent);
        public static SolidBrush SolidBrushPath(int[] rgb, int gradientValue) => new SolidBrush(Color.FromArgb(255, ClampGradientValue(((float)rgb[0] / 255) * gradientValue), ClampGradientValue(((float)rgb[1] / 255) * gradientValue), ClampGradientValue(((float)rgb[2] / 255) * gradientValue)));
        public static SolidBrush SolidBrushCover(int[] rgb, int gradientValue) => new SolidBrush(Color.FromArgb(255, ClampGradientValue(((float)rgb[0] / 255) * gradientValue), ClampGradientValue(((float)rgb[1] / 255) * gradientValue), ClampGradientValue(((float)rgb[2] / 255) * gradientValue)));
        public static SolidBrush SolidBrushOverlap(int[] rgb, int gradientValue) => new SolidBrush(Color.FromArgb(255, ClampGradientValue(((float)rgb[0] / 255) * gradientValue), ClampGradientValue(((float)rgb[1] / 255) * gradientValue), ClampGradientValue(((float)rgb[2] / 255) * gradientValue)));

        public static SolidBrush SolidBrushBuyzones() => new SolidBrush(Color.FromArgb(40, 0, 255, 0));
        public static SolidBrush SolidBrushBombsites() => new SolidBrush(Color.FromArgb(40, 255, 0, 0));
        public static SolidBrush SolidBrushRescueZones() => new SolidBrush(Color.FromArgb(40, 0, 0, 255));


        private static int ClampGradientValue(float gradientValue)
        {
            return (int)Math.Ceiling(Math.Clamp(gradientValue, 50, 200));
        }
    }
}
