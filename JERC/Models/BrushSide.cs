using JERC.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace JERC.Models
{
    public class BrushSide
    {
        public int id;
        public int brushId;
        public List<Vertices> vertices = new List<Vertices>();
        public JercTypes jercType;
        public DisplacementStuff displacementStuff;


        public BrushSide(int id, int brushId)
        {
            this.id = id;
            this.brushId = brushId;
        }

        public BrushSide(int id, int brushId, JercTypes jercType)
        {
            this.id = id;
            this.brushId = brushId;
            this.jercType = jercType;
        }

        public BrushSide(int id, int brushId, DisplacementStuff displacementSide, JercTypes jercType)
        {
            this.id = id;
            this.brushId = brushId;
            this.displacementStuff = displacementSide;
            this.jercType = jercType;
        }
    }
}
