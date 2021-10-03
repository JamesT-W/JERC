using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Models
{
    public class VerticesToDraw
    {
        public Point vertices;
        public int zAxis;
        public Color colour;

        public VerticesToDraw(Point vertices, int zAxis, Color colour)
        {
            this.vertices = vertices;
            this.zAxis = zAxis;
            this.colour = colour;
        }
    }
}
