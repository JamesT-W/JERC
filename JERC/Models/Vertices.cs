using JERC.Constants;
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
        public float? z;

        public Vertices(string vertices)
        {
            if (string.IsNullOrWhiteSpace(vertices))
            {
                Logger.LogImportantWarning("vertices string was null or white space when creating a new Vertices, skipping.");
                return;
            }

            var verticesSplit = vertices.Split(" ");

            if (verticesSplit.Count() != 3)
            {
                Logger.LogImportantWarning($"Incorrect number of vertices found after splitting string: {verticesSplit.Count()}");
                return;
            }

            float.TryParse(verticesSplit[0], Globalization.Style, Globalization.Culture, out x);
            float.TryParse(verticesSplit[1], Globalization.Style, Globalization.Culture, out y);
            float.TryParse(verticesSplit[2], Globalization.Style, Globalization.Culture, out float zTemp);

            z = zTemp;
        }

        public Vertices(string vertices, string origin)
        {
            if (string.IsNullOrWhiteSpace(vertices))
            {
                Logger.LogImportantWarning("vertices string was null or white space when creating a new Vertices, skipping.");
                return;
            }
            if (string.IsNullOrWhiteSpace(origin))
            {
                Logger.LogImportantWarning("origin string was null or white space when creating a new Vertices, skipping.");
                return;
            }

            var verticesSplit = vertices.Split(" ");
            var originSplit = origin.Split(" ");

            if (verticesSplit.Count() != 3)
            {
                Logger.LogImportantWarning($"Incorrect number of vertices found after splitting vertices string when creating a new Vertices: {verticesSplit.Count()}");
                return;
            }
            if (originSplit.Count() != 3)
            {
                Logger.LogImportantWarning($"Incorrect number of vertices found after splitting origin string when creating a new Vertices: {originSplit.Count()}");
                return;
            }

            float.TryParse(verticesSplit[0], Globalization.Style, Globalization.Culture, out float xVertices);
            float.TryParse(verticesSplit[1], Globalization.Style, Globalization.Culture, out float yVertices);
            float.TryParse(verticesSplit[2], Globalization.Style, Globalization.Culture, out float zVertices);

            float.TryParse(originSplit[0], Globalization.Style, Globalization.Culture, out float xOrigin);
            float.TryParse(originSplit[1], Globalization.Style, Globalization.Culture, out float yOrigin);
            float.TryParse(originSplit[2], Globalization.Style, Globalization.Culture, out float zOrigin);

            x = xVertices + xOrigin;
            y = yVertices + yOrigin;
            z = zVertices + zOrigin;
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
            if (x == other.x && y == other.y && (z == null || z == other.z))
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


        public string GetPlaneFormatForSingleVertices()
        {
            //return z == null ? string.Concat($"({x} {y}") : string.Concat($"({x} {y} {z})");
            return z == null ? string.Concat($"({x} {y} 0)") : string.Concat($"({x} {y} {z})");
        }
    }
}
