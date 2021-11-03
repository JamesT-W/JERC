using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Models
{
    public class SquarePlane : IEquatable<SquarePlane>
    {
        public Vertices vertices1;
        public Vertices vertices2;
        public Vertices vertices3;
        public Vertices vertices4;

        public SquarePlane(Vertices vertices1, Vertices vertices2, Vertices vertices3)
        {
            this.vertices1 = vertices1;
            this.vertices2 = vertices2;
            this.vertices3 = vertices3;

            var allVerticesInPlane = new List<Vertices>() { vertices1, vertices2, vertices3 };

            var x = allVerticesInPlane.Select(a => a.x).GroupBy(a => a).OrderBy(a => a.Count()).FirstOrDefault().Key;
            var y = allVerticesInPlane.Select(a => a.y).GroupBy(a => a).OrderBy(a => a.Count()).FirstOrDefault().Key;
            var z = allVerticesInPlane.Select(a => a.z).GroupBy(a => a).OrderBy(a => a.Count()).FirstOrDefault().Key;

            this.vertices4 = new Vertices(x, y, (float)z);
        }


        public bool Equals(SquarePlane other)
        {
            if (vertices1 == other.vertices1 && vertices2 == other.vertices2 && vertices3 == other.vertices3 && vertices4 == other.vertices4)
                return true;

            return false;
        }


        public override int GetHashCode()
        {
            int hashVertices1 = vertices1 == null ? 0 : vertices1.GetHashCode();
            int hashVertices2 = vertices2 == null ? 0 : vertices2.GetHashCode();
            int hashVertices3 = vertices3 == null ? 0 : vertices3.GetHashCode();
            int hashVertices4 = vertices4 == null ? 0 : vertices4.GetHashCode();

            return hashVertices1 ^ hashVertices2 ^ hashVertices3 ^ hashVertices4;
        }
    }
}
