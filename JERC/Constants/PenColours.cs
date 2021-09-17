using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace JERC.Constants
{
    public static class PenColours
    {
        public static Pen PenLayout(int[] rgb, int gradientValue) => new Pen(Color.FromArgb(255, ClampGradientValue(((float)rgb[0] / 255) * gradientValue), ClampGradientValue(((float)rgb[1] / 255) * gradientValue), ClampGradientValue(((float)rgb[2] / 255) * gradientValue)), 1);
        public static Pen PenCover(int[] rgb, int gradientValue) => new Pen(Color.FromArgb(255, ClampGradientValue(((float)rgb[0] / 255) * gradientValue), ClampGradientValue(((float)rgb[1] / 255) * gradientValue), ClampGradientValue(((float)rgb[2] / 255) * gradientValue)), 1);
        public static Pen PenOverlap(int[] rgb, int gradientValue) => new Pen(Color.FromArgb(255, ClampGradientValue(((float)rgb[0] / 255) * gradientValue), ClampGradientValue(((float)rgb[1] / 255) * gradientValue), ClampGradientValue(((float)rgb[2] / 255) * gradientValue)), 1);
        //public static Pen PenNegative() => new Pen(Color.Transparent, 1);


        private static int ClampGradientValue(float gradientValue)
        {
            return (int)Math.Ceiling(Math.Clamp(gradientValue, 50, 200));
        }
    }
}
