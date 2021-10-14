using JERC.Models;
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
        public static Color ColourError => Color.DarkRed;

        public static Color GetGreyscaleGradient(float value) => Color.FromArgb(255, ClampGradientValue(value), ClampGradientValue(value), ClampGradientValue(value));

        public static Color ColourRemove() => Color.Transparent;
        public static Color ColourBrush(Color rgbLow, Color rgbHigh, float percentageAboveMin, int? alphaOverride = null) =>
            Color.FromArgb(alphaOverride == null ? 255 : (int)alphaOverride,
                GetColour(rgbLow.R, rgbHigh.R, percentageAboveMin),
                GetColour(rgbLow.G, rgbHigh.G, percentageAboveMin),
                GetColour(rgbLow.B, rgbHigh.B, percentageAboveMin)
            );

        public static Color ColourRemoveStroke(Color strokeColour) => strokeColour;
        public static Color ColourBrushesStroke(Color strokeColour) => strokeColour;

        public static Color ColourBuyzones() => Color.FromArgb(50, 25, 255, 25);
        public static Color ColourBombsites() => Color.FromArgb(50, 255, 25, 25);
        public static Color ColourRescueZones() => Color.FromArgb(50, 25, 25, 255);

        public static Color ColourBuyzonesStroke() => Color.FromArgb(255, 0, 255, 0);
        public static Color ColourBombsitesStroke() => Color.FromArgb(255, 255, 0, 0);
        public static Color ColourRescueZonesStroke() => Color.FromArgb(255, 0, 0, 255);


        private static int GetColour(int rgbLow, int rgbHigh, float percentageAboveMin)
        {
            var highestNum = rgbLow > rgbHigh ? rgbLow : rgbHigh;
            var lowestNum = rgbLow > rgbHigh ? rgbHigh : rgbLow;

            var diff = highestNum - lowestNum;

            var diffMultiplied = diff * percentageAboveMin;

            var value = lowestNum + diffMultiplied;

            return ClampGradientValue(value);
        }


        private static int ClampGradientValue(float value)
        {
            return Math.Clamp((int)Math.Ceiling(value), 0, 255);
        }
    }
}
