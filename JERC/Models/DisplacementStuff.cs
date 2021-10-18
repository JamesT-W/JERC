using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMFParser;

namespace JERC.Models
{
    public class DisplacementStuff
    {
        public int power;
        public string startPosition;
        public int flags;
        public float elevation;
        public float subdiv;
        public List<DisplacementSideNormalsRow> normals = new List<DisplacementSideNormalsRow>();
        public List<DisplacementSideDistancesRow> distances = new List<DisplacementSideDistancesRow>();
        public List<IVNode> offsets;
        public List<IVNode> offset_normals;
        public List<IVNode> alphas;
        public List<IVNode> triangle_tags;
        public List<IVNode> allowed_verts;

        public int numOfRows;

        public float pointPosMinX;
        public float pointPosMinY;
        public float pointPosMinZ;
        public float distanceBetweenPointsX;
        public float distanceBetweenPointsY;
        public float distanceBetweenPointsZ;


        public DisplacementStuff(DispInfo dispInfo, List<Vertices> brushSideVertices)
        {
            power = dispInfo.power;
            startPosition = dispInfo.startPosition;
            flags = dispInfo.flags;
            elevation = dispInfo.elevation;
            subdiv = dispInfo.subdiv;

            offsets = dispInfo.offsets;
            offset_normals = dispInfo.offset_normals;
            alphas = dispInfo.alphas;
            triangle_tags = dispInfo.triangle_tags;
            allowed_verts = dispInfo.allowed_verts;

            // 
            numOfRows = (int)Math.Pow(2, power) + 1;

            ////////// TODO: CURRENTLY ONLY WORKS PROPERLY FOR SQUARE DISPLACEMENT BRUSHES ////////////////
            pointPosMinX = brushSideVertices.Min(a => a.x);
            pointPosMinY = brushSideVertices.Min(a => a.y);
            pointPosMinZ = (float)brushSideVertices.Min(a => a.z);
            distanceBetweenPointsX = (brushSideVertices.Max(a => a.x) - pointPosMinX) / (numOfRows - 1); // brushSideVertices, numOfRowsDistances
            distanceBetweenPointsY = (brushSideVertices.Max(a => a.y) - pointPosMinY) / (numOfRows - 1); // brushSideVertices, numOfRowsDistances
            distanceBetweenPointsZ = (float)(brushSideVertices.Max(a => a.z) - pointPosMinZ) / (numOfRows - 1); // brushSideVertices, numOfRowsDistances

            // adds default values for each row and vert for normals and distances
            for (int i = 0; i < numOfRows; i++)
            {
                var displacementSideNormalsRow = new DisplacementSideNormalsRow(i, dispInfo.normals.FirstOrDefault().Body.FirstOrDefault(x => x.Name == ("row" + i)));
                var displacementSideDistancesRow = new DisplacementSideDistancesRow(i, dispInfo.distances.FirstOrDefault().Body.FirstOrDefault(x => x.Name == ("row" + i)));

                // add default values if null
                if (displacementSideNormalsRow == null || displacementSideDistancesRow == null)
                {
                    normals.Add(new DisplacementSideNormalsRow(i, numOfRows));
                    distances.Add(new DisplacementSideDistancesRow(i, numOfRows));
                }

                // add parsed values
                normals.Add(displacementSideNormalsRow);
                distances.Add(displacementSideDistancesRow);
            }
        }


        ////////// TODO: CURRENTLY ONLY WORKS PROPERLY FOR SQUARE DISPLACEMENT BRUSHES ////////////////
        public List<Vertices> GetSquareVerticesPositions(int x, int y)
        {
            var numOfPoints = 4;

            // x, y
            Vertices point0 = null;
            Vertices point1 = null;
            Vertices point2 = null;
            Vertices point3 = null;

            for (int i = 0; i < numOfPoints; i++)
            {
                var xUsing = x;
                var yUsing = y;

                switch (i)
                {
                    case 1:
                        xUsing++;
                        break;
                    case 2:
                        yUsing++;
                        break;
                    case 3:
                        xUsing++;
                        yUsing++;
                        break;
                }

                var xBrush = pointPosMinX + (distanceBetweenPointsX * xUsing);
                var yBrush = pointPosMinY + (distanceBetweenPointsY * yUsing);
                var zBrush = pointPosMinZ + (distanceBetweenPointsZ * xUsing) + (distanceBetweenPointsZ * yUsing); //////// is this right ???

                var xDisp = normals.FirstOrDefault(a => a.rowNum == yUsing).valuesList[xUsing].x * distances.FirstOrDefault(a => a.rowNum == yUsing).valuesList[xUsing];
                var yDisp = normals.FirstOrDefault(a => a.rowNum == yUsing).valuesList[xUsing].y * distances.FirstOrDefault(a => a.rowNum == yUsing).valuesList[xUsing];
                var zDisp = normals.FirstOrDefault(a => a.rowNum == yUsing).valuesList[xUsing].z * distances.FirstOrDefault(a => a.rowNum == yUsing).valuesList[xUsing];

                var xValue = xBrush + xDisp;
                var yValue = yBrush + yDisp;
                var zValue = zBrush + zDisp;

                switch (i)
                {
                    case 0:
                        point0 = new Vertices(xValue, yValue, (float)zValue);
                        break;
                    case 1:
                        point1 = new Vertices(xValue, yValue, (float)zValue);
                        break;
                    case 2:
                        point2 = new Vertices(xValue, yValue, (float)zValue);
                        break;
                    case 3:
                        point3 = new Vertices(xValue, yValue, (float)zValue);
                        break;
                }
            }

            return new List<Vertices>() { point0, point1, point3, point2 }; // point3 is added before point2 so that a rectangle can be created
        }
    }
}
