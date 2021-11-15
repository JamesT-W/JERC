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
        public ConfigurationValues configurationValues;

        public int brushSideId;
        public int brushId;
        public Vertices center;
        public List<VerticesToDraw> verticesToDraw;
        public int zAxisAverage;
        public bool isDisplacement;
        public Color? colour; // brush entities only
        public Color? colourStroke; // brush entities only
        public int? strokeWidth; // brush entities only

        public JercTypes? jercType;
        public EntityTypes? entityType;

        public bool? needsRotating90;
        public bool? needsRotating180;
        public bool? needsRotating270;


        public ObjectToDraw(ConfigurationValues configurationValues, int brushSideId, int brushId, Vertices center, List<VerticesToDraw> verticesToDraw, bool isDisplacement, JercTypes jercType)
        {
            SetMainValues(configurationValues, brushSideId, brushId, center, verticesToDraw, isDisplacement);

            this.jercType = jercType;
        }


        public ObjectToDraw(ConfigurationValues configurationValues, int brushSideId, int brushId, Vertices center, List<VerticesToDraw> verticesToDraw, bool isDisplacement, EntityTypes entityType)
        {
            SetMainValues(configurationValues, brushSideId, brushId, center, verticesToDraw, isDisplacement);

            this.entityType = entityType;
        }


        public ObjectToDraw(ConfigurationValues configurationValues, int brushSideId, int brushId, Vertices center, List<VerticesToDraw> verticesToDraw, bool isDisplacement, EntityTypes entityType, Color colour, Color colourStroke, int strokeWidth)
        {
            SetMainValues(configurationValues, brushSideId, brushId, center, verticesToDraw, isDisplacement);

            this.entityType = entityType;

            this.colour = colour;
            this.colourStroke = colourStroke;
            this.strokeWidth = strokeWidth;
        }


        public static ObjectToDraw Clone(ObjectToDraw objectToDraw)
        {
            var brushSideIdNew = objectToDraw.brushSideId;
            var brushIdNew = objectToDraw.brushId;
            var centerNew = objectToDraw.center;
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

            /*
            var needsRotating90New = objectToDraw.needsRotating90;
            var needsRotating180New = objectToDraw.needsRotating180;
            var needsRotating270New = objectToDraw.needsRotating270;
            */

            if (jercTypeNew != null)
                return new ObjectToDraw(objectToDraw.configurationValues, brushSideIdNew, brushIdNew, centerNew, verticesToDrawNew, isDisplacementNew, (JercTypes)jercTypeNew);
            else if (entityTypeNew != null)
                if (colourNew == null || colourStrokeNew == null || strokeWidthNew == null)
                    return new ObjectToDraw(objectToDraw.configurationValues, brushSideIdNew, brushIdNew, centerNew, verticesToDrawNew, isDisplacementNew, (EntityTypes)entityTypeNew);
                else
                    return new ObjectToDraw(objectToDraw.configurationValues, brushSideIdNew, brushIdNew, centerNew, verticesToDrawNew, isDisplacementNew, (EntityTypes)entityTypeNew, (Color)colourNew, (Color)colourStrokeNew, (int)strokeWidthNew);
            else
                return null;
        }


        private void SetMainValues(ConfigurationValues configurationValues, int brushSideId, int brushId, Vertices center, List<VerticesToDraw> verticesToDraw, bool isDisplacement)
        {
            this.configurationValues = configurationValues;

            this.brushSideId = brushSideId;
            this.brushId = brushId;
            this.center = center;
            this.verticesToDraw = verticesToDraw;
            zAxisAverage = (int)verticesToDraw.Select(x => x.vertices.z).Average();
            this.isDisplacement = isDisplacement;

            this.needsRotating90 = GetNeedsRotating90();
            this.needsRotating180 = GetNeedsRotating180();
            this.needsRotating270 = GetNeedsRotating270();
        }


        private bool GetNeedsRotating90()
        {
            if (isDisplacement && configurationValues.displacementRotationSideIds90.Any(x => x == brushSideId))
                return true;

            return false;
        }


        private bool GetNeedsRotating180()
        {
            if (isDisplacement && configurationValues.displacementRotationSideIds180.Any(x => x == brushSideId))
                return true;

            return false;
        }


        private bool GetNeedsRotating270()
        {
            if (isDisplacement && configurationValues.displacementRotationSideIds270.Any(x => x == brushSideId))
                return true;

            return false;
        }
    }
}
