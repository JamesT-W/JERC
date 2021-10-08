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
        public Color? colour; // brush entities only
        public Color? colourStroke; // brush entities only
        public int? strokeWidth; // brush entities only


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


        public ObjectToDraw(List<VerticesToDraw> verticesToDraw, int zAxisAverage, EntityTypes entityType, Color colour, Color colourStroke, int strokeWidth)
        {
            this.verticesToDraw = verticesToDraw;
            this.zAxisAverage = zAxisAverage;
            this.entityType = entityType;
            this.colour = colour;
            this.colourStroke = colourStroke;
            this.strokeWidth = strokeWidth;
        }


        public static ObjectToDraw Clone(ObjectToDraw objectToDraw)
        {
            var verticesToDrawNew = new List<VerticesToDraw>();

            foreach (var verticesToDraw in objectToDraw.verticesToDraw)
            {
                verticesToDrawNew.Add(new VerticesToDraw(verticesToDraw.vertices, verticesToDraw.zAxis, verticesToDraw.colour));
            }

            var zAxisAverageNew = objectToDraw.zAxisAverage;
            var jercTypeNew = objectToDraw.jercType;
            var entityTypeNew = objectToDraw.entityType;
            var colourNew = objectToDraw.colour;
            var colourStrokeNew = objectToDraw.colourStroke;
            var strokeWidthNew = objectToDraw.strokeWidth;

            if (jercTypeNew != null)
                return new ObjectToDraw(verticesToDrawNew, zAxisAverageNew, (JercTypes)jercTypeNew);
            else if (entityTypeNew != null)
                if (colourNew == null || colourStrokeNew == null || strokeWidthNew == null)
                    return new ObjectToDraw(verticesToDrawNew, zAxisAverageNew, (EntityTypes)entityTypeNew);
                else
                    return new ObjectToDraw(verticesToDrawNew, zAxisAverageNew, (EntityTypes)entityTypeNew, (Color)colourNew, (Color)colourStrokeNew, (int)strokeWidthNew);
            else
                return null;
        }
    }
}
