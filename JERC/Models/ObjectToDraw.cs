using JERC.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Models
{
    public class ObjectToDraw
    {
        public List<VerticesToDraw> verticesToDraw;
        public List<BrushLine> brushLines;
        public JercTypes? jercType;
        public EntityTypes? entityType;
        public int? entityId;
        public int? entityBrushId;


        public ObjectToDraw(List<VerticesToDraw> verticesToDraw, List<BrushLine> brushLines, JercTypes jercType)
        {
            this.verticesToDraw = verticesToDraw;
            this.brushLines = brushLines;
            this.jercType = jercType;
        }


        public ObjectToDraw(List<VerticesToDraw> verticesToDraw, List<BrushLine> brushLines, EntityTypes entityType, int entityId, int entityBrushId)
        {
            this.verticesToDraw = verticesToDraw;
            this.brushLines = brushLines;
            this.entityType = entityType;
            this.entityId = entityId;
            this.entityBrushId = entityBrushId;
        }
    }
}
