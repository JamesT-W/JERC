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
        public int zAxisAverage;
        public JercTypes? jercType;
        public EntityTypes? entityType;


        public ObjectToDraw(List<VerticesToDraw> verticesToDraw, int zAxisAverage, JercTypes jercType)
        {
            this.verticesToDraw = verticesToDraw;
            this.zAxisAverage = zAxisAverage;
            this.jercType = jercType;
        }


        public ObjectToDraw(List<VerticesToDraw> verticesToDraw, int zAxisAverage, EntityTypes entityType)
        {
            this.verticesToDraw = verticesToDraw;
            this.zAxisAverage = zAxisAverage;
            this.entityType = entityType;
        }


        public static ObjectToDraw Clone(ObjectToDraw objectToDraw)
        {
            var verticesToDrawNew = new List<VerticesToDraw>();

            foreach (var verticesToDraw in objectToDraw.verticesToDraw)
            {
                verticesToDrawNew.Add(new VerticesToDraw(verticesToDraw.vertices, verticesToDraw.zAxis, verticesToDraw.colour));
            }

            var zAxisAverage = objectToDraw.zAxisAverage;
            var jercTypeNew = objectToDraw.jercType;
            var entityTypeNew = objectToDraw.entityType;

            if (jercTypeNew != null)
                return new ObjectToDraw(verticesToDrawNew, zAxisAverage, (JercTypes)jercTypeNew);
            else if (entityTypeNew != null)
                return new ObjectToDraw(verticesToDrawNew, zAxisAverage, (EntityTypes)entityTypeNew);
            else
                return null;
        }
    }
}
