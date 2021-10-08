using JERC.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Models
{
    public class EntityBrushSide
    {
        public List<Vertices> vertices = new List<Vertices>();
        public EntityTypes entityType;
        public Color colour;
        public Color colourStroke;
        public int strokeWidth;

        public EntityBrushSide() {}
    }
}
