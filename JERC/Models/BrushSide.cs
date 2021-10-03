using JERC.Enums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace JERC.Models
{
    public class BrushSide
    {
        public List<Vertices> vertices { get; } = new List<Vertices>(); // DO NOT ADD TO THIS LIST DIRECTLY, use AddVertices()
        public List<BrushLine> brushLines { get; private set; }
        public JercTypes jercType;


        public BrushSide() { }


        public BrushSide(List<Vertices> vertices, List<BrushLine> brushLines, JercTypes jercType)
        {
            this.vertices = vertices;
            this.brushLines = brushLines;
            this.jercType = jercType;
        }


        public void AddVertices(Vertices vertices)
        {
            this.vertices.Add(vertices);

            SetBrushLines(this.vertices); // this will set brushLines from scratch every time a vertices is added
        }


        private void SetBrushLines(List<Vertices> vertices)
        {
            brushLines = new List<BrushLine>();

            if (vertices.Count() < 2)
                return;

            for (int i = 0, j = i + 1; i < vertices.Count(); i++, j++)
            {
                if (i == vertices.Count() - 1)
                    j = 0;

                var brushLineNew = new BrushLine(vertices[i], vertices[j]);
                if (brushLineNew != null && brushLineNew.vertices1 != null && brushLineNew.vertices2 != null && (brushLineNew.vertices1.x != 0 || brushLineNew.vertices1.y != 0 || brushLineNew.vertices2.x != 0 || brushLineNew.vertices2.y != 0))
                {
                    brushLines.Add(brushLineNew);
                }

                if (vertices.Count() == 2) // no need to add the same brushLine in both directions
                    return;
            }
        }
    }
}
