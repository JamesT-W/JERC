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

        public Vertices vertices0; // bottom left
        public Vertices vertices1; // bottom right
        public Vertices vertices2; // top right
        public Vertices vertices3; // top left

        public Vertices[,] pointsLocations;


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

            var vertices0 = brushSideVertices.OrderBy(a => a.x + a.y).FirstOrDefault();
            var vertices2 = brushSideVertices.OrderByDescending(a => a.x + a.y).FirstOrDefault();
            var vertices1 = brushSideVertices.Where(x => x != vertices2).OrderByDescending(a => a.x).ThenBy(a => a.y).FirstOrDefault();
            var vertices3 = brushSideVertices.Where(x => x != vertices0).OrderBy(a => a.x).ThenByDescending(a => a.y).FirstOrDefault();

            pointsLocations = new Vertices[numOfRows, numOfRows];

            for (int x = 0; x < numOfRows; x++)
            {
                for (int y = 0; y < numOfRows; y++)
                {
                    if (x == 0 && y == 0)
                    {
                        pointsLocations[x, y] = vertices0;
                        continue;
                    }
                    else if (x == (numOfRows-1) && y == 0)
                    {
                        pointsLocations[x, y] = vertices1;
                        continue;
                    }
                    else if (x == (numOfRows - 1) && y == (numOfRows - 1))
                    {
                        pointsLocations[x, y] = vertices2;
                        continue;
                    }
                    else if (x == 0 && y == (numOfRows - 1))
                    {
                        pointsLocations[x, y] = vertices3;
                        continue;
                    }

                    var divisionValue = numOfRows - 1;

                    Vertices diff01 = new Vertices(vertices1.x - vertices0.x, vertices1.y - vertices0.y, (float)(vertices1.z - vertices0.z));
                    Vertices diff12 = new Vertices(vertices2.x - vertices1.x, vertices2.y - vertices1.y, (float)(vertices2.z - vertices1.z));
                    Vertices diff23 = new Vertices(vertices2.x - vertices3.x, vertices2.y - vertices3.y, (float)(vertices2.z - vertices3.z));
                    Vertices diff03 = new Vertices(vertices3.x - vertices0.x, vertices3.y - vertices0.y, (float)(vertices3.z - vertices0.z));

                    //var numOfRowsToMinusX = y == numOfRows - 1 ? numOfRows - 1 : numOfRows;
                    //var numOfRowsToMinusY = x == numOfRows - 1 ? numOfRows - 1 : numOfRows;
                    var numOfRowsToMinusX = numOfRows;
                    var numOfRowsToMinusY = numOfRows;

                    var xValue = vertices0.x +
                        (((((diff01.x / divisionValue * x) * Math.Abs(y - numOfRowsToMinusX)) / numOfRows) + ((diff23.x / divisionValue * x) * y) / numOfRows)) +
                        (((((diff03.x / divisionValue * y) * Math.Abs(x - numOfRowsToMinusY)) / numOfRows) + ((diff12.x / divisionValue * y) * x) / numOfRows));
                    var yValue = vertices0.y +
                        (((((diff03.y / divisionValue * y) * Math.Abs(x - numOfRowsToMinusY)) / numOfRows) + ((diff12.y / divisionValue * y) * x) / numOfRows)) +
                        (((((diff01.y / divisionValue * x) * Math.Abs(y - numOfRowsToMinusX)) / numOfRows) + ((diff23.y / divisionValue * x) * y) / numOfRows));

                    var zValue = vertices0.z + ((((diff01.z / divisionValue * x) + (diff23.z / divisionValue * x) + (diff03.z / divisionValue * y) + (diff12.z / divisionValue * y))) / 2);

                    pointsLocations[x, y] = new Vertices(xValue, yValue, (float)zValue);
                }
            }

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
                        xUsing++;
                        yUsing++;
                        break;
                    case 3:
                        yUsing++;
                        break;
                }

                var verticesUsing = pointsLocations[xUsing, yUsing];

                var xBrush = verticesUsing.x;////////// + (distanceBetweenPointsX * xUsing);
                var yBrush = verticesUsing.y;/////////// + (distanceBetweenPointsY * yUsing);
                var zBrush = verticesUsing.z; // + (distanceBetweenPointsZ * xUsing) + (distanceBetweenPointsZ * yUsing); //////// is this right ???

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

            return new List<Vertices>() { point0, point1, point2, point3 };
        }
    }
}
