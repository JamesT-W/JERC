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
        public PointF[] vertices;
        public float worldHeight;
        public EntityTypes entityType;

        public EntityBrushSide(int numOfVertices)
        {
            vertices = new PointF[numOfVertices];
        }
    }
}
