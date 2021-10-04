using System;
using System.Collections.Generic;
using System.Text;

namespace JERC.Constants
{
    public static class Sizes
    {
        public static readonly float MaxHammerGridSize = 32768.000000f;
        public static readonly int FinalOutputImageResolution = 1024;
        public static readonly float SizeReductionMultiplier = 1.0f; // this is not taken into account currently, values other than 1.0 will make the overview image offset and not aligned in game either
    }
}
