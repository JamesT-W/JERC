using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Models
{
    public class EntityBrush
    {
        public int entityId;
        public int brushId;
        public List<EntityBrushSide> entityBrushSides = new List<EntityBrushSide>();


        public EntityBrush(int entityId, int brushId)
        {
            this.entityId = entityId;
            this.brushId = brushId;
        }


        public EntityBrush(int entityId, int brushId, List<EntityBrushSide> entityBrushSides)
        {
            this.entityId = entityId;
            this.brushId = brushId;
            this.entityBrushSides = entityBrushSides;
        }
    }
}
