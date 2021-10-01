using JERC.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace JERC.Constants
{
    public static class PenColours
    {
        public static Pen PenRemove() => new Pen(Colours.ColourRemove(), 1);
        public static Pen PenPath(int[] rgb, int gradientValue) => new Pen(Colours.ColourPath(rgb, gradientValue), 1);
        public static Pen PenCover(int[] rgb, int gradientValue) => new Pen(Colours.ColourCover(rgb, gradientValue), 1);
        public static Pen PenOverlap(int[] rgb, int gradientValue) => new Pen(Colours.ColourOverlap(rgb, gradientValue), 1);

        public static Pen PenBuyzones() => new Pen(Colours.ColourBuyzones(), 10);
        public static Pen PenBombsites() => new Pen(Colours.ColourBombsites(), 10);
        public static Pen PenRescueZones() => new Pen(Colours.ColourRescueZones(), 10);
    }
}
