using System;
using System.Collections.Generic;
using System.Text;

namespace JERC.Constants
{
    public static class Sizes
    {
        public static int MaxHammerGridSize = 32768;
        public static int FinalOutputImageResolution = 1024;
        public static int SizeReductionMultiplier = 1; // this should use scale from the overview txt
        public static int StrokeWidthMultiplier = 20;
    }
}
