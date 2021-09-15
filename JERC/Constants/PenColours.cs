using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace JERC.Constants
{
    public static class PenColours
    {
        public static Pen PenLayout(int gradientValue) => new Pen(Color.FromArgb(255, ClampGradientValue(gradientValue), ClampGradientValue(gradientValue), ClampGradientValue(gradientValue)), 1);
        public static Pen PenCover(int gradientValue) => new Pen(Color.FromArgb(255, 200, 200, 200), 1);
        public static Pen PenNegative(int gradientValue) => new Pen(Color.FromArgb(255, 204, 102, 0), 1);
        public static Pen PenOverlap(int gradientValue) => new Pen(Color.FromArgb(255, 204, 102, 0), 1);


        private static int ClampGradientValue(int gradientValue)
        {
            return Math.Clamp(gradientValue, 50, 200);
        }
    }
}
