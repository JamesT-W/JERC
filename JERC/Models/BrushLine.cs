using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Models
{
    public class BrushLine
    {
        public Vertices vertices1;
        public Vertices vertices2;
        public List<Vertices> verticesOnLine = new List<Vertices>();

        public BrushLine(Vertices vertices1, Vertices vertices2)
        {
            if (vertices1 == vertices2 || (vertices1.x == vertices2.x && vertices1.y == vertices2.y))
                return;

            if (vertices1.x < vertices2.x)
            {
                var tempVertices = vertices1;
                vertices1 = vertices2;
                vertices2 = tempVertices;
            }

            this.vertices1 = new Vertices((int)vertices1.x, (int)vertices1.y);
            this.vertices2 = new Vertices((int)vertices2.x, (int)vertices2.y);

            SetVerticesOnLine(vertices1, vertices2);
        }


        private void SetVerticesOnLine(Vertices vertices1, Vertices vertices2)
        {
            if (vertices1 == vertices2)
                return;


            verticesOnLine.Add(new Vertices(vertices1.x, vertices1.y));


            // x
            var m1 = (vertices1.y - vertices2.y) / (vertices1.x - vertices2.x);
            var c1 = vertices1.y - vertices1.x * m1;

            for (int x = (int)vertices1.x; x < (int)vertices2.x; x++)
            {
                var y = (m1 * x) + c1; // y = mx + c


                if (verticesOnLine.FirstOrDefault().x == x && verticesOnLine.FirstOrDefault().y == y) // don't add duplicates
                    continue;


                verticesOnLine.Add(new Vertices(x, y));
            }

            // y
            var m2 = (vertices1.x - vertices2.x) / (vertices1.y - vertices2.y);
            var c2 = vertices1.x - vertices1.y * m2;

            for (int y = (int)vertices1.y; y < (int)vertices2.y; y++)
            {
                var x = (m2 * y) + c2; // y = mx + c

                verticesOnLine.Add(new Vertices(x, y));
            }


            if (verticesOnLine.LastOrDefault().x != vertices2.x || verticesOnLine.LastOrDefault().y != vertices2.y) // don't add duplicates
            {
                verticesOnLine.Add(new Vertices(vertices2.x, vertices2.y));
            }
        }
    }
}
