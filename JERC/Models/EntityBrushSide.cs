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
        public int id;
        public int brushId;
        public List<Vertices> vertices = new List<Vertices>();
        public EntityTypes entityType;
        public int orderNum;
        public Color rendercolor;
        public Color colourStroke;
        public int strokeWidth;
        public string material;


        public EntityBrushSide(int id, int brushId)
        {
            this.id = id;
            this.brushId = brushId;
        }
    }
}
