using System;
using System.Collections.Generic;
using System.Text;

namespace JERC.Models
{
    public class BoundingBox
    {
        public float minX { get; set; } = 0.00f;
        public float maxX { get; set; } = 0.00f;
        public float minY { get; set; } = 0.00f;
        public float maxY { get; set; } = 0.00f;
        public float minZ { get; set; } = 0.00f;
        public float maxZ { get; set; } = 0.00f;

        public BoundingBox() { }
    }
}
