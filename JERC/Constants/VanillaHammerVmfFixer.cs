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
        public static void CalculateVerticesPlusForAllBrushes(List<Models.Brush> brushes)
        {
            if (GameConfigurationValues.isVanillaHammer == false)
                return;

            if (brushes == null)
                return;

            brushes.RemoveAll(x => x.side == null || !x.side.Any());

            if (brushes.Select(x => x.side) == null || !brushes.Select(x => x.side).Any())
                return;


            foreach (var brush in brushes)
            {
                var brushSides = brush.side;

                // if -software is provided and is vanilla hammer, force all brush sides' vertices to be calculated (even if it is a hammer++ vmf)
                var brushSidesNoVertics = GameConfigurationValues.softwareProvided ? brushSides : brushSides.Where(x => x.vertices_plus == null || !x.vertices_plus.Any()).ToList();

                if (brushSidesNoVertics == null || brushSidesNoVertics.Count() == 0)
                    return;

                //
                var positionsToRemove = new List<int>();
                var brushSideIdsFound = new List<int>();

                for (int i = brushSidesNoVertics.Count() - 1; i >= 0; i--)
                {
                    if (brushSideIdsFound.Any(x => x == brushSidesNoVertics[i].id))
                        positionsToRemove.Add(i);
                    else
                        brushSideIdsFound.Add(brushSidesNoVertics[i].id);
                }

                foreach (var index in positionsToRemove)
                {
                    brushSidesNoVertics.RemoveAt(index);
                }
                //


                for (int i = 0; i < brushSidesNoVertics.Count(); i++)
                {
                    brushSidesNoVertics[i].vertices_plus = CalculateBrushSideVerticesUsingPlane(brushSidesNoVertics);
                }
            }
        }


        private static List<Vertices> CalculateBrushSideVerticesUsingPlane(List<Side> brushSidesNoVertics)
        {
            var verticesCalculated = new List<Vertices>();

            Region region = new Region(new Rectangle(0, 0, Sizes.MaxHammerGridSize, Sizes.MaxHammerGridSize)); // creates a region the size of hammer's grid

            var allPoints = new List<Point>();


            var brushesSidesUsing = brushSidesNoVertics; // this should now be covered by this check further down:  'if (pointsToUse.Count() != 2)'
            //var brushesSidesUsing = brushSidesNoVertics.Where(x => x.material.ToLower() != TextureNames.IgnoreTextureName.ToLower()).ToList(); // should use this material on bottom face

            foreach (var side in brushesSidesUsing)
            {
                var verticesToUse = GetAllVerticesInPlane(side.plane);
                var fourthVertices = GetFourthVerticesFromPlane(side.plane);
                verticesToUse.Add(fourthVertices);

                var pointsToUse = verticesToUse.Select(x => new Point((int)Math.Ceiling(x.x), (int)Math.Ceiling(x.y))).ToArray();

                allPoints.AddRange(pointsToUse);
            }

            if (allPoints.Count() == 0)
                return new List<Vertices>();

            var maxX = allPoints.Max(a => a.X);
            var minX = allPoints.Min(a => a.X);
            var maxY = allPoints.Max(a => a.Y);
            var minY = allPoints.Min(a => a.Y);
            //var maxZ = allPoints.Max(a => a.Z);
            //var minZ = allPoints.Min(a => a.Z);

            var width = maxX - minX;
            var height = maxY - minY;

            for (int i = 0; i < brushesSidesUsing.Count(); i++)
            {
                GraphicsPath path = new GraphicsPath();
                var verticesToUse = GetAllVerticesInPlane(brushesSidesUsing[i].plane);
                var fourthVertices = GetFourthVerticesFromPlane(brushesSidesUsing[i].plane);
                verticesToUse.Add(fourthVertices);

                var pointsToUse = verticesToUse.Select(a => new Point((int)Math.Ceiling(a.x), (int)Math.Ceiling(a.y))).Distinct().ToList();

                if (pointsToUse.Count() != 2)
                {
                    //Logger.LogImportantWarning("More than 2 points to use when calculating brush side vertices using plane for vanilla Hammer vmf, probably a top down facing brush face, ignoring");

                    continue;
                }

                // find distance to all bounding box corners to see which should be used as a Point
                var distancePointBL0 = new Point(Math.Abs(pointsToUse[0].X - minX), Math.Abs(pointsToUse[0].Y - minY));
                var distancePointBR0 = new Point(Math.Abs(pointsToUse[0].X - maxX), Math.Abs(pointsToUse[0].Y - minY));
                var distancePointTR0 = new Point(Math.Abs(pointsToUse[0].X - maxX), Math.Abs(pointsToUse[0].Y - maxY));
                var distancePointTL0 = new Point(Math.Abs(pointsToUse[0].X - minX), Math.Abs(pointsToUse[0].Y - maxY));

                var distancePointBL1 = new Point(Math.Abs(pointsToUse[1].X - minX), Math.Abs(pointsToUse[1].Y - minY));
                var distancePointBR1 = new Point(Math.Abs(pointsToUse[1].X - maxX), Math.Abs(pointsToUse[1].Y - minY));
                var distancePointTR1 = new Point(Math.Abs(pointsToUse[1].X - maxX), Math.Abs(pointsToUse[1].Y - maxY));
                var distancePointTL1 = new Point(Math.Abs(pointsToUse[1].X - minX), Math.Abs(pointsToUse[1].Y - maxY));

                /*
                var distanceBL0 = Math.Sqrt(Math.Pow(distancePointBL0.X, 2) + Math.Pow(distancePointBL0.Y, 2)); // get hypotenuse length
                var distanceBR0 = Math.Sqrt(Math.Pow(distancePointBR0.X, 2) + Math.Pow(distancePointBR0.Y, 2));
                var distanceTR0 = Math.Sqrt(Math.Pow(distancePointTR0.X, 2) + Math.Pow(distancePointTR0.Y, 2));
                var distanceTL0 = Math.Sqrt(Math.Pow(distancePointTL0.X, 2) + Math.Pow(distancePointTL0.Y, 2));

                var distanceBL1 = Math.Sqrt(Math.Pow(distancePointBL1.X, 2) + Math.Pow(distancePointBL1.Y, 2));
                var distanceBR1 = Math.Sqrt(Math.Pow(distancePointBR1.X, 2) + Math.Pow(distancePointBR1.Y, 2));
                var distanceTR1 = Math.Sqrt(Math.Pow(distancePointTR1.X, 2) + Math.Pow(distancePointTR1.Y, 2));
                var distanceTL1 = Math.Sqrt(Math.Pow(distancePointTL1.X, 2) + Math.Pow(distancePointTL1.Y, 2));
                */

                var distanceBL0 = distancePointBL0.X + distancePointBL0.Y;
                var distanceBR0 = distancePointBR0.X + distancePointBR0.Y;
                var distanceTR0 = distancePointTR0.X + distancePointTR0.Y;
                var distanceTL0 = distancePointTL0.X + distancePointTL0.Y;

                var distanceBL1 = distancePointBL1.X + distancePointBL1.Y;
                var distanceBR1 = distancePointBR1.X + distancePointBR1.Y;
                var distanceTR1 = distancePointTR1.X + distancePointTR1.Y;
                var distanceTL1 = distancePointTL1.X + distancePointTL1.Y;

                var verticesAlreadyOnCorner = new List<int>();
                if (distanceBL0 == 0 || distanceBL1 == 0)
                    verticesAlreadyOnCorner.Add(0);
                if (distanceBR0 == 0 || distanceBR1 == 0)
                    verticesAlreadyOnCorner.Add(1);
                if (distanceTR0 == 0 || distanceTR1 == 0)
                    verticesAlreadyOnCorner.Add(2);
                if (distanceTL0 == 0 || distanceTL1 == 0)
                    verticesAlreadyOnCorner.Add(3);

                if (verticesAlreadyOnCorner.Count() > 1 ||
                    (
                        ((pointsToUse[0].X == minX || pointsToUse[0].X == maxX) && (pointsToUse[1].X == minX || pointsToUse[1].X == maxX)) ||
                        ((pointsToUse[0].Y == minY || pointsToUse[0].Y == maxY) && (pointsToUse[1].Y == minY || pointsToUse[1].Y == maxY)) // ||
                        /*((pointsToUse[0].X == minX || pointsToUse[0].X == maxX) && (pointsToUse[1].Y == minY || pointsToUse[1].Y == maxY)) ||
                        ((pointsToUse[0].Y == minY || pointsToUse[0].Y == maxY) && (pointsToUse[1].X == minX || pointsToUse[1].X == maxX))*/
                    )
                )
                {
                    verticesAlreadyOnCorner.Clear();
                }

                var maxDistancesByCornerVerticesNum = new Dictionary<int, double>();
                maxDistancesByCornerVerticesNum.Add(0, new List<double>() { distanceBL0, distanceBL1 }.Max());
                maxDistancesByCornerVerticesNum.Add(1, new List<double>() { distanceBR0, distanceBR1 }.Max());
                maxDistancesByCornerVerticesNum.Add(2, new List<double>() { distanceTR0, distanceTR1 }.Max());
                maxDistancesByCornerVerticesNum.Add(3, new List<double>() { distanceTL0, distanceTL1 }.Max());

                var closestBoundingBoxCornerVerticesNum = maxDistancesByCornerVerticesNum.Where(x => !verticesAlreadyOnCorner.Any(y => y == x.Key)).OrderBy(x => x.Value).FirstOrDefault().Key;
                switch (closestBoundingBoxCornerVerticesNum)
                {
                    case 0:
                        pointsToUse.Add(new Point(minX, minY));
                        break;
                    case 1:
                        pointsToUse.Add(new Point(maxX, minY));
                        break;
                    case 2:
                        pointsToUse.Add(new Point(maxX, maxY));
                        break;
                    case 3:
                        pointsToUse.Add(new Point(minX, maxY));
                        break;
                }
                var pointsToUseArray = pointsToUse.ToArray();


                // temporarily change all values to start at 0 minimum
                for (int j = 0; j < pointsToUseArray.Count(); j++)
                {
                    var point = pointsToUseArray[j];
                    pointsToUseArray[j] = new Point((point.X - minX), (point.Y - minY));
                }


                path.AddPolygon(pointsToUseArray);

                region.Exclude(path); // excludes from the region

                path.Dispose();

                foreach (var point in pointsToUseArray.Take(pointsToUseArray.Length - 1)) // ignores the last point that was a bounding box corner point added in the closestBoundingBoxCornerVerticesNum switch
                {
                    verticesCalculated.Add(new Vertices(point.X, point.Y, (float)verticesToUse.Max(a => a.z))); ////////////////// TODO: should this be min? average? Something else?
                }
            }


            // change back all temporarily changed values to start at 0 minimum
            for (int j = 0; j < verticesCalculated.Count(); j++)
            {
                var vert = verticesCalculated[j];
                verticesCalculated[j] = new Vertices((vert.x + minX), (vert.y + minY), (float)vert.z);
            }

            region.Dispose();


            var verticesCalculatedTopHeightOnly = new List<Vertices>();
            foreach (var vertices in verticesCalculated.OrderByDescending(a => a.z).Distinct())
            {
                if (!verticesCalculatedTopHeightOnly.Any(a => Math.Ceiling(a.x) == Math.Ceiling(vertices.x) && Math.Ceiling(a.y) == Math.Ceiling(vertices.y)))
                {
                    verticesCalculatedTopHeightOnly.Add(vertices);
                }
            }

            // order the vertices
            var newVerticesOrder = GetAnyNumOfVerticesInPathOrder(verticesCalculatedTopHeightOnly);

            return newVerticesOrder;
        }


        private static List<Vertices> GetAnyNumOfVerticesInPathOrder(List<Vertices> vertices)
        {
            List<Vertices> verticesOrdered = new List<Vertices>();

            List<Vertices> verticesLeftToAddAtBeginningOfLoop = new List<Vertices>();
            verticesLeftToAddAtBeginningOfLoop.AddRange(vertices);

            List<Vertices> verticesAddedThisLoop = new List<Vertices>();

            var vertices0 = vertices.OrderBy(a => a.y).ThenBy(a => a.x).FirstOrDefault();
            verticesOrdered.Add(vertices0);
            verticesLeftToAddAtBeginningOfLoop.RemoveAll(x => x == vertices0);

            var previousMovingDirection = 0; //0 = right && right/up, 1 = up && up/left, 2 = left && left/down, 3 = down && down/right

            while (verticesLeftToAddAtBeginningOfLoop.Any())
            {
                verticesAddedThisLoop.Clear();

                foreach (var vert in verticesLeftToAddAtBeginningOfLoop)
                {
                    if (verticesOrdered.Any(x => x == vert))
                        continue;

                    if (previousMovingDirection >= 4)
                        previousMovingDirection = 0;

                    var previousVerticesAdded = verticesOrdered.Last();

                    if ((previousMovingDirection == 0 && previousVerticesAdded.x == verticesLeftToAddAtBeginningOfLoop.Max(a => a.x)) ||
                        (previousMovingDirection == 1 && previousVerticesAdded.y == verticesLeftToAddAtBeginningOfLoop.Max(a => a.y)) ||
                        (previousMovingDirection == 2 && previousVerticesAdded.x == verticesLeftToAddAtBeginningOfLoop.Min(a => a.x)) ||
                        (previousMovingDirection == 3 && previousVerticesAdded.y == verticesLeftToAddAtBeginningOfLoop.Min(a => a.y))
                    )
                    {
                        previousMovingDirection++;
                    }

                    var verticesLeftToAdd = verticesLeftToAddAtBeginningOfLoop.Where(a => !verticesAddedThisLoop.Any(b => b == a));

                    Vertices nextVerticesToAdd = new Vertices(0, 0, 0);
                    switch (previousMovingDirection)
                    {
                        case 0:
                            var possibleVerticesToAdd = verticesLeftToAdd.Where(a => a.x > previousVerticesAdded.x && a.y >= previousVerticesAdded.y);
                            if (!possibleVerticesToAdd.Any())
                            {
                                previousMovingDirection++;
                                continue;
                            }

                            var nextVertices = possibleVerticesToAdd.OrderBy(a => a.y).ThenByDescending(a => a.x).FirstOrDefault();
                            //var closestVertices = verticesLeftToAdd.OrderBy(a => Math.Sqrt(Math.Pow(Math.Abs(a.x - previousVerticesAdded.x), 2) + Math.Pow(Math.Abs(a.y - previousVerticesAdded.y), 2))).FirstOrDefault();
                            nextVerticesToAdd = nextVertices;
                            break;
                        case 1:
                            possibleVerticesToAdd = verticesLeftToAdd.Where(a => a.x <= previousVerticesAdded.x && a.y > previousVerticesAdded.y);
                            if (!possibleVerticesToAdd.Any())
                            {
                                previousMovingDirection++;
                                continue;
                            }

                            nextVertices = possibleVerticesToAdd.OrderByDescending(a => a.x).ThenByDescending(a => a.y).FirstOrDefault();
                            nextVerticesToAdd = nextVertices;
                            break;
                        case 2:
                            possibleVerticesToAdd = verticesLeftToAdd.Where(a => a.x < previousVerticesAdded.x && a.y <= previousVerticesAdded.y);
                            if (!possibleVerticesToAdd.Any())
                            {
                                previousMovingDirection++;
                                continue;
                            }

                            nextVertices = possibleVerticesToAdd.OrderByDescending(a => a.y).ThenBy(a => a.x).FirstOrDefault();
                            nextVerticesToAdd = nextVertices;
                            break;
                        case 3:
                            possibleVerticesToAdd = verticesLeftToAdd.Where(a => a.x >= previousVerticesAdded.x && a.y < previousVerticesAdded.y);
                            if (!possibleVerticesToAdd.Any())
                            {
                                previousMovingDirection++;
                                continue;
                            }

                            nextVertices = possibleVerticesToAdd.OrderBy(a => a.x).ThenBy(a => a.y).FirstOrDefault();
                            nextVerticesToAdd = nextVertices;
                            break;
                        default:
                            // default should never get hit
                            Logger.LogImportantWarning("Default hit in switch when attempting to get vertices in order, errors likely to occur");
                            continue;
                    }

                    verticesOrdered.Add(nextVerticesToAdd);
                    verticesAddedThisLoop.Add(nextVerticesToAdd);
                }

                foreach (var vert in verticesAddedThisLoop)
                {
                    verticesLeftToAddAtBeginningOfLoop.RemoveAll(x => x == vert);
                }
            }

            return verticesOrdered;
        }


        private static Vertices GetFourthVerticesFromPlane(string plane)
        {
            var allVerticesInPlane = GetAllVerticesInPlane(plane);

            //var temp = allVerticesInPlane.Select(a => a.x).GroupBy(a => a);

            // work out vertices3
            float x = allVerticesInPlane.Select(a => a.x).GroupBy(a => a).OrderBy(a => a.Count()).FirstOrDefault().Key;
            float y = allVerticesInPlane.Select(a => a.y).GroupBy(a => a).OrderBy(a => a.Count()).FirstOrDefault().Key;
            float z = (float)allVerticesInPlane.Select(a => a.z).GroupBy(a => a).OrderBy(a => a.Count()).FirstOrDefault().Key;

            if (allVerticesInPlane.Distinct().Count() != 3)
            {
                Logger.LogImportantWarning("brushSide's plane does not contain 3 vertices, errors likely to occur");
            }

            // finish
            var vertices3 = new Vertices(x, y, (float)z);

            return vertices3;
        }


        private static List<Vertices> GetAllVerticesInPlane(string plane)
        {
            var planesVerticesList = plane.Replace("(", string.Empty).Replace(")", string.Empty).Split(" ").ToList();

            var vertices0 = new Vertices(planesVerticesList[0] + " " + planesVerticesList[1] + " " + planesVerticesList[2]); // 3 vertices per plane in vanilla hammer
            var vertices1 = new Vertices(planesVerticesList[3] + " " + planesVerticesList[4] + " " + planesVerticesList[5]); // 3 vertices per plane in vanilla hammer
            var vertices2 = new Vertices(planesVerticesList[6] + " " + planesVerticesList[7] + " " + planesVerticesList[8]); // 3 vertices per plane in vanilla hammer

            return new List<Vertices>() { vertices0, vertices1, vertices2 };
        }


        private static void DisposeGraphics(Graphics graphics)
        {
            graphics.Dispose();
        }


        private static void DisposeImage(Bitmap bmp)
        {
            bmp.Dispose();
        }
    }
}
