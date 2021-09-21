using System;
using System.Collections.Generic;
using System.Text;

namespace JERC.Constants
{
    public static class Sizes
    {
        public static readonly int MaxHammerGridSize = 32768;
        public static readonly int FinalOutputImageResolution = 1024;
        public static readonly int SizeReductionMultiplier = 1; // this should use scale from the overview txt
        public static readonly int StrokeWidthMultiplier = 20;
    }
}
