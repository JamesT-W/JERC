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
        public float minZGradient { get; set; } = 0.00f;
        public float maxZGradient { get; set; } = 0.00f;

        public BoundingBox() { }

        public BoundingBox(float minX, float maxX, float minY, float maxY, float minZ, float maxZ, float minZGradient, float maxZGradient)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
            this.minZ = minZ;
            this.maxZ = maxZ;
            this.minZGradient = minZGradient;
            this.maxZGradient = maxZGradient;
        }
    }
}
