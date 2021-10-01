using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Constants
{
    public static class Colours
    {
        public static Color ColourRemove() => Color.Transparent;
        public static Color ColourPath(int[] rgb, int gradientValue) =>
            Color.FromArgb(255,
                ClampGradientValue(((float)rgb[0] / 255) * gradientValue),
                ClampGradientValue(((float)rgb[1] / 255) * gradientValue),
                ClampGradientValue(((float)rgb[2] / 255) * gradientValue)
            );
        public static Color ColourCover(int[] rgb, int gradientValue) =>
            Color.FromArgb(255,
                ClampGradientValue(((float)rgb[0] / 255) * gradientValue),
                ClampGradientValue(((float)rgb[1] / 255) * gradientValue),
                ClampGradientValue(((float)rgb[2] / 255) * gradientValue)
            );
        public static Color ColourOverlap(int[] rgb, int gradientValue) =>
            Color.FromArgb(255,
                ClampGradientValue(((float)rgb[0] / 255) * gradientValue),
                ClampGradientValue(((float)rgb[1] / 255) * gradientValue),
                ClampGradientValue(((float)rgb[2] / 255) * gradientValue)
            );

        public static Color ColourBrushesStroke() => Color.FromArgb(255, 255, 255, 255);

        public static Color ColourBuyzones() => Color.FromArgb(75, 0, 255, 0);
        public static Color ColourBombsites() => Color.FromArgb(75, 255, 0, 0);
        public static Color ColourRescueZones() => Color.FromArgb(75, 0, 0, 255);

        public static Color ColourBuyzonesStroke() => Color.FromArgb(255, 0, 255, 0);
        public static Color ColourBombsitesStroke() => Color.FromArgb(255, 255, 0, 0);
        public static Color ColourRescueZonesStroke() => Color.FromArgb(255, 0, 0, 255);


        private static int ClampGradientValue(float value)
        {
            return (int)Math.Ceiling(Math.Clamp(value, 10, 245));
        }
    }
}
