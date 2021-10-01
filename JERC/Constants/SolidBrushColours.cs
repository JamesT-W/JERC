using JERC.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace JERC.Constants
{
    public static class SolidBrushColours
    {
        public static SolidBrush SolidBrushRemove() => new SolidBrush(Colours.ColourRemove());
        public static SolidBrush SolidBrushPath(int[] rgb, int gradientValue) => new SolidBrush(Colours.ColourPath(rgb, gradientValue));
        public static SolidBrush SolidBrushCover(int[] rgb, int gradientValue) => new SolidBrush(Colours.ColourCover(rgb, gradientValue));
        public static SolidBrush SolidBrushOverlap(int[] rgb, int gradientValue) => new SolidBrush(Colours.ColourOverlap(rgb, gradientValue));

        public static SolidBrush SolidBrushBuyzones() => new SolidBrush(Colours.ColourBuyzones());
        public static SolidBrush SolidBrushBombsites() => new SolidBrush(Colours.ColourBombsites());
        public static SolidBrush SolidBrushRescueZones() => new SolidBrush(Colours.ColourRescueZones());
    }
}
