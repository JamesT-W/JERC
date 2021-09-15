using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace JAR.Models
{
    public class BrushVerticesAndWorldHeight
    {
        public PointF[] vertices;
        public float worldHeight; // used to figure out the colour gradient values needed for the Pen and SolidBrush

        public BrushVerticesAndWorldHeight(int numOfVertices)
        {
            vertices = new PointF[numOfVertices];
        }
    }
}
