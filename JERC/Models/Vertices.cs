using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JERC.Models
{
    public class Vertices : IEquatable<Vertices>
    {
        public float x;
        public float y;
        public float z;

        public Vertices(string vertices)
        {
            var verticesSplit = vertices.Split(" ");

            if (verticesSplit.Count() != 3)
                return;

            var style = System.Globalization.NumberStyles.Number;
            var culture = System.Globalization.CultureInfo.CreateSpecificCulture("en-GB");

            float.TryParse(verticesSplit[0], style, culture, out x);
            float.TryParse(verticesSplit[1], style, culture, out y);
            float.TryParse(verticesSplit[2], style, culture, out z);
        }

        public Vertices(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vertices(float x, float y)
        {
            this.x = x;
            this.y = y;
        }


        public bool Equals(Vertices other)
        {
            if (x == other.x && y == other.y && z == other.z)
                return true;

            return false;
        }


        public override int GetHashCode()
        {
            int hashX = x == 0 ? 0 : x.GetHashCode();
            int hashY = y == 0 ? 0 : y.GetHashCode();
            int hashZ = z == 0 ? 0 : z.GetHashCode();

            return hashX ^ hashY ^ hashZ;
        }
    }
}
