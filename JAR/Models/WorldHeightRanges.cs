using System;
using System.Collections.Generic;
using System.Text;

namespace JAR.Models
{
    public class WorldHeightRanges
    {
        public float min;
        public float max;

        public WorldHeightRanges(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }
}
