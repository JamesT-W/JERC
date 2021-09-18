using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JERC.Models
{
    public class Vertices
    {
        public float x;
        public float y;
        public float z;

        public Vertices(string vertices)
        {
            var verticesSplit = vertices.Split(" ");

            if (verticesSplit.Count() != 3)
                return;

            float.TryParse(verticesSplit[0], out x);
            float.TryParse(verticesSplit[1], out y);
            float.TryParse(verticesSplit[2], out z);
        }

        public Vertices(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}
