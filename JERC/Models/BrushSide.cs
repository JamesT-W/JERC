using JERC.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace JERC.Models
{
    public class BrushSide
    {
        public int brushId;
        public List<Vertices> vertices = new List<Vertices>();
        public JercTypes jercType;
        public DisplacementStuff displacementStuff;


        public BrushSide(int brushId)
        {
            this.brushId = brushId;
        }

        public BrushSide(int brushId, JercTypes jercType)
        {
            this.brushId = brushId;
            this.jercType = jercType;
        }

        public BrushSide(int brushId, DisplacementStuff displacementSide, JercTypes jercType)
        {
            this.displacementStuff = displacementSide;
            this.jercType = jercType;
        }
    }
}
