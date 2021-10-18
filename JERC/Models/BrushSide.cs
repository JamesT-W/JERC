using JERC.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace JERC.Models
{
    public class BrushSide
    {
        public List<Vertices> vertices = new List<Vertices>();
        public JercTypes jercType;
        public DisplacementStuff displacementStuff;


        public BrushSide() { }

        public BrushSide(JercTypes jercType)
        {
            this.jercType = jercType;
        }

        public BrushSide(DisplacementStuff displacementSide, JercTypes jercType)
        {
            this.displacementStuff = displacementSide;
            this.jercType = jercType;
        }
    }
}
