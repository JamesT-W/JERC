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
        public static Color ColourRemove() => Color.Transparent;
        public static Color ColourBrush(Color rgbLow, Color rgbHigh, float percentageAboveMin) =>
            Color.FromArgb(255,
                GetColour(rgbLow.R, rgbHigh.R, percentageAboveMin),
                GetColour(rgbLow.G, rgbHigh.G, percentageAboveMin),
                GetColour(rgbLow.B, rgbHigh.B, percentageAboveMin)
            );

        public static Color ColourRemoveStroke(Color strokeColour) => strokeColour;
        public static Color ColourBrushesStroke(Color strokeColour) => strokeColour;

        public static Color ColourBuyzones() => Color.FromArgb(75, 0, 255, 0);
        public static Color ColourBombsites() => Color.FromArgb(75, 255, 0, 0);
        public static Color ColourRescueZones() => Color.FromArgb(75, 0, 0, 255);

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
