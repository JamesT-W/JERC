using JERC.Constants;
using JERC.Enums;
using JERC.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using VMFParser;

namespace JERC
{
    class Program
    {


        // TODO: this should be configured by hammer entities
        private static readonly int[] TEMPORARYrgbColourLayout = new int[3] { 127, 127, 127 };
        private static readonly int[] TEMPORARYrgbColourCover = new int[3] { 225, 225, 225 };
        private static readonly int[] TEMPORARYrgbColourOverlap = new int[3] { 0, 127, 127 };






        private static readonly string gameBinDirectoryPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\"));
        private static readonly string gameCsgoDirectoryPath = Path.GetFullPath(Path.Combine(Path.Combine(gameBinDirectoryPath, @"..\"), @"csgo\"));
        private static readonly string gameOverviewsDirectoryPath = Path.GetFullPath(Path.Combine(gameCsgoDirectoryPath, @"resource\overviews\"));

        private static string outputImageFilepath;
        private static string outputTxtFilepath;

        private static readonly string visgroupName = "JERC";

        private static string visgroupId;

        private static string mapName;

        private static VMF vmf;
        private static VmfRequiredData vmfRequiredData;


        static void Main(string[] args)
        {
            if (args.Count() != 2 || args[0].ToLower() != "-filepath" || !File.Exists(args[1]))
            {
                Console.WriteLine("Incorrect arguments. Reinstalling JERC recommended.");
                return;
            }

            // TODO: uncomment for release
            /*
            if (gameBinDirectoryPath.Split(@"\").LastOrDefault() != "bin")
            {
                Console.WriteLine(@"JERC's folder should be placed in ...\Counter-Strike Global Offensive\bin");
                return;
            }

            if (csgoBinDirectoryPath.Split(@"\").LastOrDefault() != "csgo")
            {
                Console.WriteLine(@"JERC's folder should be placed in ...\Counter-Strike Global Offensive\bin");
                return;
            }

            if (gameOverviewsDirectoryPath.Split(@"\").LastOrDefault() != "overviews")
            {
                Console.WriteLine(@"JERC's folder should be placed in ...\Counter-Strike Global Offensive\bin");
                return;
            }
            */

            var lines = File.ReadAllLines(args[1]);

            mapName = Path.GetFileNameWithoutExtension(args[1]);


            // TODO: uncomment for release
            /*
            outputImageFilepath = string.Concat(gameOverviewsDirectoryPath, mapName, "_radar");
            outputTxtFilepath = string.Concat(gameOverviewsDirectoryPath, mapName, ".txt");
            */
            outputImageFilepath = @"F:\Coding Stuff\GitHub Files\JERC\jerc_test_map_radar";
            outputTxtFilepath = @"F:\Coding Stuff\GitHub Files\JERC\jerc_test_map.txt";


            vmf = new VMF(lines);

            SetVisgroupId();

            vmfRequiredData = GetVmfRequiredData();

            var overviewPositionValues = SortScaleStuff();

            GenerateRadar(overviewPositionValues);

            GenerateTxt(overviewPositionValues);
        }


        private static void SetVisgroupId()
        {
            visgroupId = (from x in vmf.VisGroups.Body
                          from y in x.Body
                          where y.Name == "name"
                          where y.Value.ToLower() == visgroupName.ToLower()
                          select x.Body.FirstOrDefault(y => y.Name == "visgroupid").Value)
                          .FirstOrDefault();
        }


        private static VmfRequiredData GetVmfRequiredData()
        {
            var allWorldBrushes = vmf.World.Body.Where(x => x.Name == "solid");
            var allEntities = vmf.Body.Where(x => x.Name == "entity");

            var allWorldBrushesInVisgroup = from x in allWorldBrushes
                                            from y in x.Body
                                            where y.Name == "editor"
                                            from z in y.Body
                                            where z.Name == "visgroupid"
                                            where z.Value == visgroupId
                                            select x;

            var brushesLayout = GetBrushesLayout(allWorldBrushesInVisgroup);
            var brushesCover = GetBrushesCover(allWorldBrushesInVisgroup);
            var brushesOverlap = GetBrushesOverlap(allWorldBrushesInVisgroup);
            var brushesNegative = GetBrushesNegative(allWorldBrushesInVisgroup);

            var buyzoneBrushEntities = GetBuyzoneBrushEntities(allEntities);
            var bombsiteBrushEntities = GetBombsiteBrushEntities(allEntities);
            var rescueZoneBrushEntities = GetRescueZoneBrushEntities(allEntities);
            var hostageEntities = GetHostageEntities(allEntities);

            return new VmfRequiredData(
                brushesLayout, brushesCover, brushesOverlap, brushesNegative,
                buyzoneBrushEntities, bombsiteBrushEntities, rescueZoneBrushEntities, hostageEntities
            );
        }


        private static IEnumerable<IVNode> GetBrushesLayout(IEnumerable<IVNode> allWorldBrushesInVisgroup)
        {
            return from x in allWorldBrushesInVisgroup
                   from y in x.Body
                   where y.Name == "side"
                   from z in y.Body
                   where z.Name == "material"
                   where z.Value.ToLower() == TextureNames.LayoutTextureName.ToLower()
                   select x;
        }


        private static IEnumerable<IVNode> GetBrushesCover(IEnumerable<IVNode> allWorldBrushesInVisgroup)
        {
            return from x in allWorldBrushesInVisgroup
                   from y in x.Body
                   where y.Name == "side"
                   from z in y.Body
                   where z.Name == "material"
                   where z.Value.ToLower() == TextureNames.CoverTextureName.ToLower()
                   select x;
        }


        private static IEnumerable<IVNode> GetBrushesOverlap(IEnumerable<IVNode> allWorldBrushesInVisgroup)
        {
            return from x in allWorldBrushesInVisgroup
                   from y in x.Body
                   where y.Name == "side"
                   from z in y.Body
                   where z.Name == "material"
                   where z.Value.ToLower() == TextureNames.OverlapTextureName.ToLower()
                   select x;
        }


        private static IEnumerable<IVNode> GetBrushesNegative(IEnumerable<IVNode> allWorldBrushesInVisgroup)
        {
            return from x in allWorldBrushesInVisgroup
                   from y in x.Body
                   where y.Name == "side"
                   from z in y.Body
                   where z.Name == "material"
                   where z.Value.ToLower() == TextureNames.NegativeTextureName.ToLower()
                   select x;
        }


        private static IEnumerable<IVNode> GetBuyzoneBrushEntities(IEnumerable<IVNode> allEntities)
        {
            return from x in allEntities
                   from y in x.Body
                   where y.Name == "classname"
                   where y.Value == Classnames.ClassnameBuyzone
                   select x;
        }


        private static IEnumerable<IVNode> GetBombsiteBrushEntities(IEnumerable<IVNode> allEntities)
        {
            return from x in allEntities
                   from y in x.Body
                   where y.Name == "classname"
                   where y.Value == Classnames.ClassnameBombsite
                   select x;
        }


        private static IEnumerable<IVNode> GetRescueZoneBrushEntities(IEnumerable<IVNode> allEntities)
        {
            return from x in allEntities
                   from y in x.Body
                   where y.Name == "classname"
                   where y.Value == Classnames.ClassnameRescueZone
                   select x;
        }


        private static IEnumerable<IVNode> GetHostageEntities(IEnumerable<IVNode> allEntities)
        {
            return from x in allEntities
                   from y in x.Body
                   where y.Name == "classname"
                   where y.Value == Classnames.ClassnameHostage
                   select x;
        }


        private static OverviewPositionValues SortScaleStuff()
        {
            var allWorldBrushesExceptNegative = vmfRequiredData.brushesSidesLayout.Concat(vmfRequiredData.brushesSidesCover).Concat(vmfRequiredData.brushesSidesOverlap);
            //var allWorldBrushes = vmfRequiredData.brushesSidesNegative.Concat(allWorldBrushesExceptNegative);

            var minX = allWorldBrushesExceptNegative.Min(x => x.vertices_plus.Min(y => y.x));
            var maxX = allWorldBrushesExceptNegative.Max(x => x.vertices_plus.Max(y => y.x));
            var minY = allWorldBrushesExceptNegative.Min(x => x.vertices_plus.Min(y => y.y));
            var maxY = allWorldBrushesExceptNegative.Max(x => x.vertices_plus.Max(y => y.y));

            var sizeX = maxX - minX;
            var sizeY = maxY - minY;

            /*var scaleX = (sizeX - 1024) <= 0 ? 1 : ((sizeX - 1024) / OverviewOffsets.OverviewIncreasedUnitsShownPerScaleIntegerPosX) + 1;
            var scaleY = (sizeY - 1024) <= 0 ? 1 : ((sizeY - 1024) / OverviewOffsets.OverviewIncreasedUnitsShownPerScaleIntegerPosY) + 1;*/
            var scaleX = sizeX / OverviewOffsets.OverviewScaleDivider;
            var scaleY = sizeY / OverviewOffsets.OverviewScaleDivider;

            var scale = scaleX >= scaleY ? scaleX : scaleY;

            var overviewPositionValues = new OverviewPositionValues(minX, maxX, minY, maxY, scale);

            var pixelsPerUnitX = overviewPositionValues.outputResolution / sizeX;
            var pixelsPerUnitY = overviewPositionValues.outputResolution / sizeY;

            var unitsPerPixelX = sizeX / overviewPositionValues.outputResolution;
            var unitsPerPixelY = sizeY / overviewPositionValues.outputResolution;

            return overviewPositionValues;
        }


        private static void GenerateRadar(OverviewPositionValues overviewPositionValues)
        {
            Bitmap bmp = new Bitmap(overviewPositionValues.outputResolution, overviewPositionValues.outputResolution);

            using (var graphics = Graphics.FromImage(bmp))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                graphics.SetClip(Rectangle.FromLTRB(0, 0, overviewPositionValues.outputResolution, overviewPositionValues.outputResolution));

                var boundingBox = new BoundingBox();

                var verticesAndWorldHeightRangesList = GetBrushVerticesList(boundingBox);

                RenderBrushSides(bmp, graphics, boundingBox, overviewPositionValues, verticesAndWorldHeightRangesList);

                graphics.Save();

                FlipImage(bmp);

                Bitmap bmpNew = new Bitmap(bmp, 1024, 1024);

                SaveImage(outputImageFilepath, bmpNew);

                DisposeImage(bmpNew);
            }

            DisposeImage(bmp);
        }


        private static List<BrushVerticesAndWorldHeight> GetBrushVerticesList(BoundingBox boundingBox)
        {
            var verticesAndWorldHeightRangesList = new List<BrushVerticesAndWorldHeight>();

            verticesAndWorldHeightRangesList.AddRange(GetBrushNegativeVerticesList()); // add negative first to set to graphics' clip
            verticesAndWorldHeightRangesList.AddRange(GetBrushLayoutVerticesList());
            verticesAndWorldHeightRangesList.AddRange(GetBrushCoverVerticesList());
            verticesAndWorldHeightRangesList.AddRange(GetBrushOverlapVerticesList());

            boundingBox.minX = verticesAndWorldHeightRangesList.SelectMany(x => x.vertices.Select(y => y.X)).Min();
            boundingBox.maxX = verticesAndWorldHeightRangesList.SelectMany(x => x.vertices.Select(y => y.X)).Max();
            boundingBox.minY = verticesAndWorldHeightRangesList.SelectMany(x => x.vertices.Select(y => y.Y)).Min();
            boundingBox.maxY = verticesAndWorldHeightRangesList.SelectMany(x => x.vertices.Select(y => y.Y)).Max();

            boundingBox.minZ = verticesAndWorldHeightRangesList.Select(x => x.worldHeight).Min();
            boundingBox.maxZ = verticesAndWorldHeightRangesList.Select(x => x.worldHeight).Max();

            return verticesAndWorldHeightRangesList;
        }


        private static List<BrushVerticesAndWorldHeight> GetBrushLayoutVerticesList()
        {
            var verticesAndWorldHeightRangesList = new List<BrushVerticesAndWorldHeight>();

            foreach (var side in vmfRequiredData.brushesSidesLayout)
            {
                var verticesAndWorldHeight = new BrushVerticesAndWorldHeight(side.vertices_plus.Count());
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    verticesAndWorldHeight.vertices[i] = new PointF(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier);
                    verticesAndWorldHeight.worldHeight = vert.z;
                    verticesAndWorldHeight.jercType = JercTypes.Layout;
                }

                verticesAndWorldHeightRangesList.Add(verticesAndWorldHeight);
            }

            return verticesAndWorldHeightRangesList;
        }


        private static List<BrushVerticesAndWorldHeight> GetBrushCoverVerticesList()
        {
            var verticesAndWorldHeightRangesList = new List<BrushVerticesAndWorldHeight>();

            foreach (var side in vmfRequiredData.brushesSidesCover)
            {
                var verticesAndWorldHeight = new BrushVerticesAndWorldHeight(side.vertices_plus.Count());
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    verticesAndWorldHeight.vertices[i] = new PointF(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier);
                    verticesAndWorldHeight.worldHeight = vert.z;
                    verticesAndWorldHeight.jercType = JercTypes.Cover;
                }

                verticesAndWorldHeightRangesList.Add(verticesAndWorldHeight);
            }

            return verticesAndWorldHeightRangesList;
        }


        private static List<BrushVerticesAndWorldHeight> GetBrushOverlapVerticesList()
        {
            var verticesAndWorldHeightRangesList = new List<BrushVerticesAndWorldHeight>();

            foreach (var side in vmfRequiredData.brushesSidesOverlap)
            {
                var verticesAndWorldHeight = new BrushVerticesAndWorldHeight(side.vertices_plus.Count());
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    verticesAndWorldHeight.vertices[i] = new PointF(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier);
                    verticesAndWorldHeight.worldHeight = vert.z;
                    verticesAndWorldHeight.jercType = JercTypes.Overlap;
                }

                verticesAndWorldHeightRangesList.Add(verticesAndWorldHeight);
            }

            return verticesAndWorldHeightRangesList;
        }


        private static List<BrushVerticesAndWorldHeight> GetBrushNegativeVerticesList()
        {
            var verticesAndWorldHeightRangesList = new List<BrushVerticesAndWorldHeight>();

            foreach (var side in vmfRequiredData.brushesSidesNegative)
            {
                var verticesAndWorldHeight = new BrushVerticesAndWorldHeight(side.vertices_plus.Count());
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    verticesAndWorldHeight.vertices[i] = new PointF(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier);
                    verticesAndWorldHeight.worldHeight = vert.z;
                    verticesAndWorldHeight.jercType = JercTypes.Negative;
                }

                verticesAndWorldHeightRangesList.Add(verticesAndWorldHeight);
            }

            return verticesAndWorldHeightRangesList;
        }


        private static void RenderBrushSides(Bitmap bmp, Graphics graphics, BoundingBox boundingBox, OverviewPositionValues overviewPositionValues, List<BrushVerticesAndWorldHeight> verticesAndWorldHeightRangesList)
        {
            Pen pen = null;
            SolidBrush solidBrush = null;

            foreach (var verticesAndWorldHeightRanges in verticesAndWorldHeightRangesList)
            {
                var heightAboveMin = verticesAndWorldHeightRanges.worldHeight - boundingBox.minZ;

                var percentageAboveMin = -1.00f;
                if (heightAboveMin == 0)
                {
                    if (boundingBox.minZ == boundingBox.maxZ)
                    {
                        percentageAboveMin = 255;
                    }
                    else
                    {
                        percentageAboveMin = 1;
                    }
                }
                else
                {
                    percentageAboveMin = heightAboveMin / (boundingBox.maxZ - boundingBox.minZ);
                }

                var gradientValue = (int)Math.Round(percentageAboveMin * 255, 0);

                if (gradientValue < 1)
                    gradientValue = 1;
                else if (gradientValue > 255)
                    gradientValue = 255;

                pen = verticesAndWorldHeightRanges.jercType switch
                {
                    JercTypes.Layout => PenColours.PenLayout(TEMPORARYrgbColourLayout, gradientValue),
                    JercTypes.Cover => PenColours.PenCover(TEMPORARYrgbColourCover, gradientValue),
                    JercTypes.Overlap => PenColours.PenOverlap(TEMPORARYrgbColourOverlap, gradientValue),
                    //JercTypes.Negative => PenColours.PenNegative(gradientValue),
                    _ => null,
                };

                solidBrush = verticesAndWorldHeightRanges.jercType switch
                {
                    JercTypes.Layout => BrushColours.SolidBrushLayout(TEMPORARYrgbColourLayout, gradientValue),
                    JercTypes.Cover => BrushColours.SolidBrushCover(TEMPORARYrgbColourCover, gradientValue),
                    JercTypes.Overlap => BrushColours.SolidBrushOverlap(TEMPORARYrgbColourOverlap, gradientValue),
                    //JercTypes.Negative => BrushColours.SolidBrushNegative(gradientValue),
                    _ => null,
                };


                // corrects the verts to tax into account the movement from space in world to the space in the image (which starts at (0,0))
                var verticesOffset = verticesAndWorldHeightRanges.vertices;
                for (var i = 0; i < verticesOffset.Count(); i++)
                {
                    verticesOffset[i].X = verticesOffset[i].X - overviewPositionValues.brushVerticesPosMinX + overviewPositionValues.brushVerticesOffsetX;
                    verticesOffset[i].Y = verticesOffset[i].Y - overviewPositionValues.brushVerticesPosMinY + overviewPositionValues.brushVerticesOffsetY;
                }

                if (verticesAndWorldHeightRanges.jercType == JercTypes.Negative)
                {
                    DrawTransparency(bmp, graphics, verticesAndWorldHeightRanges);
                }
                else
                {
                    DrawFilledPolygonObject(graphics, solidBrush, pen, verticesOffset);
                }
            }

            pen?.Dispose();
            solidBrush?.Dispose();
        }


        private static void DisposeImage(Bitmap bmp)
        {
            bmp.Dispose();
        }


        private static void DrawTransparency(Bitmap bmp, Graphics graphics, BrushVerticesAndWorldHeight verticesAndWorldHeightRanges)
        {
            var rectangles = new List<RectangleF>();

            var verticesToUse = verticesAndWorldHeightRanges.vertices;
            if (verticesToUse.Count() < 3)
            {
                return;
            }

            var graphicsPath = new GraphicsPath();
            graphicsPath.AddPolygon(verticesToUse);
            
            var region = new Region(graphicsPath);
            graphics.ExcludeClip(region);

            graphicsPath.CloseFigure();
        }


        private static void DrawFilledPolygonObject(Graphics graphics, SolidBrush solidBrush, Pen pen, PointF[] vertices)
        {
            graphics.DrawPolygon(pen, vertices);
            graphics.FillPolygon(solidBrush, vertices);
        }


        private static void SaveImage(string filepath, Bitmap bmp)
        {
            var canSave = false;

            // check if the files are locked
            if (File.Exists(filepath))
            {
                var fileAccessible = CheckFileIsNotLocked(filepath, true, true);

                if (fileAccessible)
                {
                    canSave = true;
                }
            }
            else
            {
                canSave = true;
            }

            // only create the image if the file is not locked
            if (canSave)
            {
                bmp.Save(filepath + ".png", ImageFormat.Png);
                bmp.Save(filepath + ".dds");
            }
        }


        private static bool CheckFileIsNotLocked(string filepath, bool checkRead = true, bool checkWrite = true, int maxRetries = 20, int waitTimeSeconds = 1)
        {
            CreateFileIfDoesntExist(filepath);

            var fileReadable = false;
            var fileWriteable = false;

            var retries = 0;
            while (retries < maxRetries)
            {
                try
                {
                    if (checkRead)
                    {
                        using (FileStream fs = File.OpenRead(filepath))
                        {
                            if (fs.CanRead)
                            {
                                fileReadable = true;
                            }
                        }
                    }
                    if (checkWrite)
                    {
                        using (FileStream fs = File.OpenWrite(filepath))
                        {
                            if (fs.CanWrite)
                            {
                                fileWriteable = true;
                            }
                        }
                    }

                    if ((!checkRead || fileReadable) && (!checkWrite || fileWriteable))
                    {
                        return true;
                    }
                }
                catch { }

                retries++;

                if (retries < maxRetries)
                {
                    Console.WriteLine(string.Concat("File has been locked ", retries, " time(s). Waiting ", waitTimeSeconds, " seconds before trying again. Filepath: ", filepath));

                    Thread.Sleep(waitTimeSeconds * 1000);
                    continue;
                }
            }

            Console.WriteLine("SKIPPING! File has been locked ", maxRetries, " times. Filepath: ", filepath);

            return false;
        }


        private static void CreateFileIfDoesntExist(string filepath)
        {
            if (!File.Exists(filepath))
            {
                File.Create(filepath).Close();
            }
        }


        private static void FlipImage(Bitmap bmp)
        {
            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
        }


        private static void GenerateTxt(OverviewPositionValues overviewPositionValues)
        {
            var overviewTxt = GetOverviewTxt(overviewPositionValues);

            var lines = overviewTxt.GetInExportableFormat(mapName);

            SaveOutputTxtFile(outputTxtFilepath, lines);
        }


        private static OverviewTxt GetOverviewTxt(OverviewPositionValues overviewPositionValues)
        {
            // TODO: uncomment
            /*
            var overviewTxt = new OverviewTxt(
                mapName, pos_x, pos_y, scale, rotate, zoom,
                inset_left, inset_top, inset_right, inset_bottom,
                CTSpawn_x, CTSpawn_y, TSpawn_x, TSpawn_y,
                bombA_x, bombA_y, bombB_x, bombB_y,
                Hostage1_x, Hostage1_y, Hostage2_x, Hostage2_y
            );
            */
            return new OverviewTxt(
                mapName, overviewPositionValues.posX.ToString(), overviewPositionValues.posY.ToString(), overviewPositionValues.scale.ToString(), null, null,
                null, null, null, null,
                null, null, null, null,
                null, null, null, null,
                null, null, null, null
            );
        }


        private static void SaveOutputTxtFile(string filepath, List<string> lines)
        {
            var canSave = false;

            // check if the files are locked
            if (File.Exists(filepath))
            {
                var fileAccessible = CheckFileIsNotLocked(filepath, true, true);

                if (fileAccessible)
                {
                    canSave = true;
                }
            }
            else
            {
                canSave = true;
            }

            // only create the image if the file is not locked
            if (canSave)
            {
                File.WriteAllLines(filepath, lines);
            }
        }
    }
}
