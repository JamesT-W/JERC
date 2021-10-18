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
        public bool isDisplacement;
        public JercTypes? jercType;
        public EntityTypes? entityType;
        public Color? colour; // brush entities only
        public Color? colourStroke; // brush entities only
        public int? strokeWidth; // brush entities only


        public ObjectToDraw(List<VerticesToDraw> verticesToDraw, bool isDisplacement, JercTypes jercType)
        {
            this.verticesToDraw = verticesToDraw;
            zAxisAverage = (int)verticesToDraw.Select(x => x.vertices.z).Average();
            this.jercType = jercType;
        }


        public ObjectToDraw(List<VerticesToDraw> verticesToDraw, bool isDisplacement, EntityTypes entityType)
        {
            this.verticesToDraw = verticesToDraw;
            zAxisAverage = (int)verticesToDraw.Select(x => x.vertices.z).Average();
            this.entityType = entityType;
        }


        public ObjectToDraw(List<VerticesToDraw> verticesToDraw, bool isDisplacement, EntityTypes entityType, Color colour, Color colourStroke, int strokeWidth)
        {
            this.verticesToDraw = verticesToDraw;
            zAxisAverage = (int)verticesToDraw.Select(x => x.vertices.z).Average();
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
                verticesToDrawNew.Add(new VerticesToDraw(new Vertices(verticesToDraw.vertices.x, verticesToDraw.vertices.y, (float)verticesToDraw.vertices.z), verticesToDraw.colour));
            }

            var isDisplacementNew = objectToDraw.isDisplacement;
            var jercTypeNew = objectToDraw.jercType;
            var entityTypeNew = objectToDraw.entityType;
            var colourNew = objectToDraw.colour;
            var colourStrokeNew = objectToDraw.colourStroke;
            var strokeWidthNew = objectToDraw.strokeWidth;

            if (jercTypeNew != null)
                return new ObjectToDraw(verticesToDrawNew, isDisplacementNew, (JercTypes)jercTypeNew);
            else if (entityTypeNew != null)
                if (colourNew == null || colourStrokeNew == null || strokeWidthNew == null)
                    return new ObjectToDraw(verticesToDrawNew, isDisplacementNew, (EntityTypes)entityTypeNew);
                else
                    return new ObjectToDraw(verticesToDrawNew, isDisplacementNew, (EntityTypes)entityTypeNew, (Color)colourNew, (Color)colourStrokeNew, (int)strokeWidthNew);
            else
                return null;
        }
    }
}
