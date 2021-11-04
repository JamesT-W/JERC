using JERC.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Constants
{
    public static class VanillaHammerVmfFixer
    {
        public static void CalculateVerticesPlusForAllBrushSides(List<Side> brushesSides)
        {
            if (!GameConfigurationValues.isVanillaHammer)
                return;

            if (brushesSides == null || brushesSides.Count() == 0)
                return;

            //
            var positionsToRemove = new List<int>();
            var brushSideIdsFound = new List<int>();

            for (int i = brushesSides.Count()-1; i >= 0 ; i--)
            {
                if (brushSideIdsFound.Any(x => x == brushesSides[i].id))
                    positionsToRemove.Add(i);
                else
                    brushSideIdsFound.Add(brushesSides[i].id);
            }

            foreach (var index in positionsToRemove)
            {
                brushesSides.RemoveAt(index);
            }
            //


            foreach (var brushSide in brushesSides)
            {
                brushSide.vertices_plus = GetBrushSideBoundingBoxFromThreeVertices(brushSide, brushesSides.Where(x => x.brushId == brushSide.brushId).Count());
            }


            /*foreach (var brushSide in brushesSides)
            {
                var brushSideBoundingBox = GetBrushSideBoundingBoxFromThreeVertices(brushSide, brushesSides.Where(x => x.brushId == brushSide.brushId).Count());

                var vertices0 = brushSideBoundingBox.OrderBy(a => a.x + a.y).FirstOrDefault(); // bottom left
                var vertices2 = brushSideBoundingBox.OrderByDescending(a => a.x + a.y).FirstOrDefault(); // top right
                var vertices1 = brushSideBoundingBox.Where(x => x != vertices2).OrderByDescending(a => a.x).ThenBy(a => a.y).FirstOrDefault(); // bottom right
                var vertices3 = brushSideBoundingBox.Where(x => x != vertices0).OrderBy(a => a.x).ThenByDescending(a => a.y).FirstOrDefault(); // top left

                var foundBrushSideInSameBrush = false;

                for (int i = 0; i < brushesSides.Count(); i++)
                {
                    var brushSideExcluding = brushesSides[i];

                    if (brushSide.brushId != brushSideExcluding.brushId || brushSide.id == brushSideExcluding.id)
                        continue;

                    foundBrushSideInSameBrush = true;

                    switch (i)
                    {
                        case 0:
                            break;
                        case 1:
                            break;
                        case 2:
                            var brushSideExcludingBoundingBox = GetAllVerticesInPlane(brushSideExcluding.plane);

                            var top = brushSideExcludingBoundingBox.OrderByDescending(a => a.y).ThenByDescending(a => a.x).FirstOrDefault();
                            var bottom = brushSideExcludingBoundingBox.OrderBy(a => a.y).ThenByDescending(a => a.x).FirstOrDefault();

                            vertices3.x = top.x;
                            vertices0.x = bottom.x;
                            break;
                        case 3:
                            brushSideExcludingBoundingBox = GetAllVerticesInPlane(brushSideExcluding.plane);

                            top = brushSideExcludingBoundingBox.OrderByDescending(a => a.y).ThenBy(a => a.x).FirstOrDefault();
                            bottom = brushSideExcludingBoundingBox.OrderBy(a => a.y).ThenBy(a => a.x).FirstOrDefault();

                            vertices2.x = top.x;
                            vertices1.x = bottom.x;
                            break;
                        case 4:
                            brushSideExcludingBoundingBox = GetAllVerticesInPlane(brushSideExcluding.plane);

                            var left = brushSideExcludingBoundingBox.OrderBy(a => a.x).ThenByDescending(a => a.y).FirstOrDefault();
                            var right = brushSideExcludingBoundingBox.OrderByDescending(a => a.x).ThenByDescending(a => a.y).FirstOrDefault();

                            vertices3.y = left.y;
                            vertices2.y = right.y;
                            break;
                        case 5:
                            brushSideExcludingBoundingBox = GetAllVerticesInPlane(brushSideExcluding.plane);

                            left = brushSideExcludingBoundingBox.OrderBy(a => a.x).ThenBy(a => a.y).FirstOrDefault();
                            right = brushSideExcludingBoundingBox.OrderByDescending(a => a.x).ThenBy(a => a.y).FirstOrDefault();

                            vertices0.y = left.y;
                            vertices1.y = right.y;
                            break;
                        default:
                            break;
                    }


                    var allVertices = new List<Vertices>() { vertices0, vertices1, vertices2, vertices3 };

                    var verticesOrdered0 = allVertices.OrderBy(a => a.x + a.y).FirstOrDefault(); // bottom left
                    var verticesOrdered2 = allVertices.OrderByDescending(a => a.x + a.y).FirstOrDefault(); // top right
                    var verticesOrdered1 = allVertices.Where(x => x != vertices2).OrderByDescending(a => a.x).ThenBy(a => a.y).FirstOrDefault(); // bottom right
                    var verticesOrdered3 = allVertices.Where(x => x != vertices0).OrderBy(a => a.x).ThenByDescending(a => a.y).FirstOrDefault(); // top left

                    brushSide.vertices_plus = new List<Vertices>() { verticesOrdered0, verticesOrdered1, verticesOrdered2, verticesOrdered3 };
                }

                if (!foundBrushSideInSameBrush)
                {
                    var allVertices = new List<Vertices>() { vertices0, vertices1, vertices2, vertices3 };

                    var verticesOrdered0 = allVertices.OrderBy(a => a.x + a.y).FirstOrDefault(); // bottom left
                    var verticesOrdered2 = allVertices.OrderByDescending(a => a.x + a.y).FirstOrDefault(); // top right
                    var verticesOrdered1 = allVertices.Where(x => x != vertices2).OrderByDescending(a => a.x).ThenBy(a => a.y).FirstOrDefault(); // bottom right
                    var verticesOrdered3 = allVertices.Where(x => x != vertices0).OrderBy(a => a.x).ThenByDescending(a => a.y).FirstOrDefault(); // top left

                    brushSide.vertices_plus = new List<Vertices>() { verticesOrdered0, verticesOrdered1, verticesOrdered2, verticesOrdered3 };
                }
            }*/
        }


        private static List<Vertices> GetBrushSideBoundingBoxFromThreeVertices(Side brushSide, int numOfBrushSidesOnBrush)
        {
            var allVerticesInPlane = GetAllVerticesInPlane(brushSide.plane);

            //var temp = allVerticesInPlane.Select(a => a.x).GroupBy(a => a);

            // work out vertices3
            float x = allVerticesInPlane.Select(a => a.x).GroupBy(a => a).OrderBy(a => a.Count()).FirstOrDefault().Key;
            float y = allVerticesInPlane.Select(a => a.y).GroupBy(a => a).OrderBy(a => a.Count()).FirstOrDefault().Key;
            float z = (float)allVerticesInPlane.Select(a => a.z).GroupBy(a => a).OrderBy(a => a.Count()).FirstOrDefault().Key;

            // check if we need to guess the position of vertices3
            if (numOfBrushSidesOnBrush < 6)
            {
                return new List<Vertices>() { allVerticesInPlane[0], allVerticesInPlane[1], allVerticesInPlane[2] };
            }
            else
            {
                if (allVerticesInPlane.Select(a => a.x).GroupBy(a => a).Count() == 3)
                {
                    var xDiff01 = Math.Abs(allVerticesInPlane[0].x - allVerticesInPlane[1].x);
                    var xDiff12 = Math.Abs(allVerticesInPlane[1].x - allVerticesInPlane[2].x);
                    var xDiff20 = Math.Abs(allVerticesInPlane[2].x - allVerticesInPlane[0].x);

                    var allXDiffs = new List<float>() { xDiff01, xDiff12, xDiff20 };
                    allXDiffs = allXDiffs.OrderByDescending(a => a).ToList();

                    if (allXDiffs.FirstOrDefault() == xDiff01)
                        x = allVerticesInPlane[0].x > allVerticesInPlane[1].x ? allVerticesInPlane[0].x : allVerticesInPlane[1].x;
                    else if (allXDiffs.FirstOrDefault() == xDiff12)
                        x = allVerticesInPlane[1].x > allVerticesInPlane[2].x ? allVerticesInPlane[1].x : allVerticesInPlane[2].x;
                    else
                        x = allVerticesInPlane[2].x > allVerticesInPlane[0].x ? allVerticesInPlane[2].x : allVerticesInPlane[0].x;
                }

                if (allVerticesInPlane.Select(a => a.y).GroupBy(a => a).Count() == 3)
                {
                    var yDiff01 = Math.Abs(allVerticesInPlane[0].y - allVerticesInPlane[1].y);
                    var yDiff12 = Math.Abs(allVerticesInPlane[1].y - allVerticesInPlane[2].y);
                    var yDiff20 = Math.Abs(allVerticesInPlane[2].y - allVerticesInPlane[0].y);

                    var allYDiffs = new List<float>() { yDiff01, yDiff12, yDiff20 };
                    allYDiffs = allYDiffs.OrderByDescending(a => a).ToList();

                    if (allYDiffs.FirstOrDefault() == yDiff01)
                        y = allVerticesInPlane[0].y > allVerticesInPlane[1].y ? allVerticesInPlane[0].y : allVerticesInPlane[1].y;
                    else if (allYDiffs.FirstOrDefault() == yDiff12)
                        y = allVerticesInPlane[1].y > allVerticesInPlane[2].y ? allVerticesInPlane[1].y : allVerticesInPlane[2].y;
                    else
                        y = allVerticesInPlane[2].y > allVerticesInPlane[0].y ? allVerticesInPlane[2].y : allVerticesInPlane[0].y;
                }

                if (allVerticesInPlane.Select(a => a.z).GroupBy(a => a).Count() == 3)
                {
                    var zDiff01 = Math.Abs((float)(allVerticesInPlane[0].z - allVerticesInPlane[1].z));
                    var zDiff12 = Math.Abs((float)(allVerticesInPlane[1].z - allVerticesInPlane[2].z));
                    var zDiff20 = Math.Abs((float)(allVerticesInPlane[2].z - allVerticesInPlane[0].z));

                    var allZDiffs = new List<float>() { zDiff01, zDiff12, zDiff20 };
                    allZDiffs = allZDiffs.OrderByDescending(a => a).ToList();

                    if (allZDiffs.FirstOrDefault() == zDiff01)
                        z = allVerticesInPlane[0].z > allVerticesInPlane[1].z ? (float)allVerticesInPlane[0].z : (float)allVerticesInPlane[1].z;
                    else if (allZDiffs.FirstOrDefault() == zDiff12)
                        z = allVerticesInPlane[1].z > allVerticesInPlane[2].z ? (float)allVerticesInPlane[1].z : (float)allVerticesInPlane[2].z;
                    else
                        z = allVerticesInPlane[2].z > allVerticesInPlane[0].z ? (float)allVerticesInPlane[2].z : (float)allVerticesInPlane[0].z;
                }

                var vertices3 = new Vertices(x, y, (float)z);

                return new List<Vertices>() { allVerticesInPlane[0], allVerticesInPlane[1], allVerticesInPlane[2], vertices3 };
            }
        }


        private static List<Vertices> GetAllVerticesInPlane(string plane)
        {
            var planesVerticesList = plane.Replace("(", string.Empty).Replace(")", string.Empty).Split(" ").ToList();

            var vertices0 = new Vertices(planesVerticesList[0] + " " + planesVerticesList[1] + " " + planesVerticesList[2]); // 3 vertices per plane in vanilla hammer
            var vertices1 = new Vertices(planesVerticesList[3] + " " + planesVerticesList[4] + " " + planesVerticesList[5]); // 3 vertices per plane in vanilla hammer
            var vertices2 = new Vertices(planesVerticesList[6] + " " + planesVerticesList[7] + " " + planesVerticesList[8]); // 3 vertices per plane in vanilla hammer

            return new List<Vertices>() { vertices0, vertices1, vertices2 };
        }
    }
}
