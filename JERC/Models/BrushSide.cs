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

        public BrushSide() { }
        public BrushSide(JercTypes jercType)
        {
            this.jercType = jercType;
        }
    }
}
