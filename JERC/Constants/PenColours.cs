using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace JERC.Constants
{
    public static class PenColours
    {
        public static Pen PenLayout(int gradientValue) => new Pen(Color.FromArgb(255, ClampGradientValue(gradientValue), ClampGradientValue(gradientValue), ClampGradientValue(gradientValue)), 1);
        public static Pen PenCover(int gradientValue) => new Pen(Color.FromArgb(255, 255, 0, 0), 1);
        public static Pen PenOverlap(int gradientValue) => new Pen(Color.FromArgb(255, 0, 255, 0), 1);
        //public static Pen PenNegative(int gradientValue) => new Pen(Color.Transparent, 1);


        private static int ClampGradientValue(int gradientValue)
        {
            return Math.Clamp(gradientValue, 50, 200);
        }
    }
}
