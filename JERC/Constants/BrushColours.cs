using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace JERC.Constants
{
    public static class BrushColours
    {
        public static SolidBrush BrushLayout(int gradientValue) => new SolidBrush(Color.FromArgb(255, ClampGradientValue(gradientValue), ClampGradientValue(gradientValue), ClampGradientValue(gradientValue)));
        public static SolidBrush BrushCover(int gradientValue) => new SolidBrush(Color.FromArgb(255, 255, 0, 0));
        public static SolidBrush BrushOverlap(int gradientValue) => new SolidBrush(Color.FromArgb(255, 0, 255, 0));
        public static SolidBrush BrushNegative(int gradientValue) => new SolidBrush(Color.FromArgb(255, 0, 0, 255));


        private static int ClampGradientValue(int gradientValue)
        {
            return Math.Clamp(gradientValue, 50, 200);
        }
    }
}
