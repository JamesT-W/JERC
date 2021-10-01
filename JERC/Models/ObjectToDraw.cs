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
        public JercTypes? jercType;
        public EntityTypes? entityType;


        public ObjectToDraw(List<VerticesToDraw> verticesToDraw, JercTypes jercType)
        {
            this.verticesToDraw = verticesToDraw;
            this.jercType = jercType;
        }


        public ObjectToDraw(List<VerticesToDraw> verticesToDraw, EntityTypes entityType)
        {
            this.verticesToDraw = verticesToDraw;
            this.entityType = entityType;
        }


        public static ObjectToDraw Clone(ObjectToDraw objectToDraw)
        {
            var verticesToDrawNew = new List<VerticesToDraw>();

            foreach (var verticesToDraw in objectToDraw.verticesToDraw)
            {
                verticesToDrawNew.Add(new VerticesToDraw(verticesToDraw.vertices, verticesToDraw.colour));
            }

            var jercTypeNew = objectToDraw.jercType;
            var entityTypeNew = objectToDraw.entityType;

            if (jercTypeNew != null)
                return new ObjectToDraw(verticesToDrawNew, (JercTypes)jercTypeNew);
            else if (entityTypeNew != null)
                return new ObjectToDraw(verticesToDrawNew, (EntityTypes)entityTypeNew);
            else
                return null;
        }
    }
}
