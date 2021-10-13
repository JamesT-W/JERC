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
        public Vertices vertices;
        public Color colour;

        public VerticesToDraw(Vertices vertices, Color colour)
        {
            this.vertices = vertices;
            this.colour = colour;
        }
    }
}
