using JAR.Constants;
using JAR.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using VMFParser;

namespace JAR
{
    class Program
    {
        private static readonly string visgroupIdJarLayoutName = "jar_layout";
        private static readonly string visgroupIdJarCoverName = "jar_cover";
        private static readonly string visgroupIdJarNegativeName = "jar_negative";
        private static readonly string visgroupIdJarOverlapName = "jar_overlap";

        private static string visgroupIdJarLayoutId;
        private static string visgroupIdJarCoverId;
        private static string visgroupIdJarNegativeId;
        private static string visgroupIdJarOverlapId;

        private static string mapName;

        private static VMF vmf;
        private static VmfRequiredData vmfRequiredData;

        private static OverviewTxt overviewTxt = new OverviewTxt();


        static void Main(string[] args)
        {
            if (args.Count() != 2 || args[0].ToLower() != "-filepath" || !File.Exists(args[1]))
                return;

            var lines = File.ReadAllLines(args[1]);

            mapName = Path.GetFileNameWithoutExtension(args[1]);

            vmf = new VMF(lines);

            SetVisgroupIds();

            vmfRequiredData = GetVmfRequiredData();

            GenerateRadar();

            GenerateTxt();
        }


        private static void SetVisgroupIds()
        {
            //var visgroupLayout = vmf.VisGroups.Body.Where(x => x.Body.Any(y => y.Name == "name" && y.Value == visgroupIdJarLayoutName));

            visgroupIdJarLayoutId = (from x in vmf.VisGroups.Body
                                     from y in x.Body
                                     where y.Name == "name"
                                     where y.Value == visgroupIdJarLayoutName
                                     select x.Body.FirstOrDefault(y => y.Name == "visgroupid").Value)
                                     .FirstOrDefault();

            visgroupIdJarCoverId = (from x in vmf.VisGroups.Body
                                    from y in x.Body
                                    where y.Name == "name"
                                    where y.Value == visgroupIdJarCoverName
                                    select x.Body.FirstOrDefault(y => y.Name == "visgroupid").Value)
                                    .FirstOrDefault();

            visgroupIdJarNegativeId = (from x in vmf.VisGroups.Body
                                       from y in x.Body
                                       where y.Name == "name"
                                       where y.Value == visgroupIdJarNegativeName
                                       select x.Body.FirstOrDefault(y => y.Name == "visgroupid").Value)
                                       .FirstOrDefault();

            visgroupIdJarOverlapId = (from x in vmf.VisGroups.Body
                                      from y in x.Body
                                      where y.Name == "name"
                                      where y.Value == visgroupIdJarOverlapName
                                      select x.Body.FirstOrDefault(y => y.Name == "visgroupid").Value)
                                      .FirstOrDefault();
        }


        private static VmfRequiredData GetVmfRequiredData()
        {
            var allEntities = vmf.Body.Where(x => x.Name == "entity");
            var allWorldBrushes = vmf.World.Body.Where(x => x.Name == "solid");

            var propsLayout = GetPropsLayout(allEntities);
            var propsCover = GetPropsCover(allEntities);
            var propsNegative = GetPropsNegative(allEntities);
            var propsOverlap = GetPropsOverlap(allEntities);

            var brushesLayout = GetBrushesLayout(allWorldBrushes);
            var brushesCover = GetBrushesCover(allWorldBrushes);
            var brushesNegative = GetBrushesNegative(allWorldBrushes);
            var brushesOverlap = GetBrushesOverlap(allWorldBrushes);

            var buyzoneBrushEntities = GetBuyzoneBrushEntities(allEntities);
            var bombsiteBrushEntities = GetBombsiteBrushEntities(allEntities);
            var rescueZoneBrushEntities = GetRescueZoneBrushEntities(allEntities);
            var hostageEntities = GetHostageEntities(allEntities);

            return new VmfRequiredData(
                propsLayout, propsCover, propsNegative, propsOverlap,
                brushesLayout, brushesCover, brushesNegative, brushesOverlap,
                buyzoneBrushEntities, bombsiteBrushEntities, rescueZoneBrushEntities, hostageEntities
            );
        }


        private static IEnumerable<IVNode> GetPropsLayout(IEnumerable<IVNode> allEntities)
        {
            return from x in allEntities
                   from y in x.Body
                   where y.Name == "editor"
                   from z in y.Body
                   where z.Name == "visgroupid"
                   where z.Value == visgroupIdJarLayoutId
                   select x;
        }


        private static IEnumerable<IVNode> GetPropsCover(IEnumerable<IVNode> allEntities)
        {
            return from x in allEntities
                   from y in x.Body
                   where y.Name == "editor"
                   from z in y.Body
                   where z.Name == "visgroupid"
                   where z.Value == visgroupIdJarCoverId
                   select x;
        }


        private static IEnumerable<IVNode> GetPropsNegative(IEnumerable<IVNode> allEntities)
        {
            return from x in allEntities
                   from y in x.Body
                   where y.Name == "editor"
                   from z in y.Body
                   where z.Name == "visgroupid"
                   where z.Value == visgroupIdJarNegativeId
                   select x;
        }


        private static IEnumerable<IVNode> GetPropsOverlap(IEnumerable<IVNode> allEntities)
        {
            return from x in allEntities
                   from y in x.Body
                   where y.Name == "editor"
                   from z in y.Body
                   where z.Name == "visgroupid"
                   where z.Value == visgroupIdJarOverlapId
                   select x;
        }


        private static IEnumerable<IVNode> GetBrushesLayout(IEnumerable<IVNode> allWorldBrushes)
        {
            return from x in allWorldBrushes
                   from y in x.Body
                   where y.Name == "editor"
                   from z in y.Body
                   where z.Name == "visgroupid"
                   where z.Value == visgroupIdJarLayoutId
                   select x;
        }


        private static IEnumerable<IVNode> GetBrushesCover(IEnumerable<IVNode> allWorldBrushes)
        {
            return from x in allWorldBrushes
                   from y in x.Body
                   where y.Name == "editor"
                   from z in y.Body
                   where z.Name == "visgroupid"
                   where z.Value == visgroupIdJarCoverId
                   select x;
        }


        private static IEnumerable<IVNode> GetBrushesNegative(IEnumerable<IVNode> allWorldBrushes)
        {
            return from x in allWorldBrushes
                   from y in x.Body
                   where y.Name == "editor"
                   from z in y.Body
                   where z.Name == "visgroupid"
                   where z.Value == visgroupIdJarNegativeId
                   select x;
        }


        private static IEnumerable<IVNode> GetBrushesOverlap(IEnumerable<IVNode> allWorldBrushes)
        {
            return from x in allWorldBrushes
                   from y in x.Body
                   where y.Name == "editor"
                   from z in y.Body
                   where z.Name == "visgroupid"
                   where z.Value == visgroupIdJarOverlapId
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


        private static void GenerateRadar()
        {
            Image bmp = new Bitmap(Sizes.OutputFileResolution, Sizes.OutputFileResolution);
            
            using (var graphics = Graphics.FromImage(bmp))
            {
                string outputFilepath = @"F:\Coding Stuff\GitHub Files\JAR\testradar.jpg";
                //string outputFilepath = string.Concat(overviewsFolder, vmfName, ".jpg");

                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                var boundingBox = new BoundingBox();

                var verticesAndWorldHeightRangesList = GetBrushVerticesList(boundingBox);
                //var verticesPerPropList = GetPropVerticesList();

                RenderBrushSides(graphics, boundingBox, verticesAndWorldHeightRangesList);
                //RenderProps(graphics, boundingBox); //// verticesPerPropList

                graphics.Save();

                FlipImage(bmp);

                SaveImage(bmp, outputFilepath);
            }

            DisposeImage(bmp);
        }


        private static List<BrushVerticesAndWorldHeight> GetBrushVerticesList(BoundingBox boundingBox)
        {
            var verticesAndWorldHeightRangesList = new List<BrushVerticesAndWorldHeight>();

            foreach (var side in vmfRequiredData.brushesSidesLayout)
            {
                var verticesAndWorldHeight = new BrushVerticesAndWorldHeight(side.vertices_plus.Count());
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    if (vert.x < boundingBox.minX)
                        boundingBox.minX = vert.x;
                    else if (vert.x > boundingBox.maxX)
                        boundingBox.maxX = vert.x;

                    if (vert.y < boundingBox.minY)
                        boundingBox.minY = vert.y;
                    else if (vert.y > boundingBox.maxY)
                        boundingBox.maxY = vert.y;

                    if (vert.z < boundingBox.minZ)
                        boundingBox.minZ = vert.z;
                    else if (vert.z > boundingBox.maxZ)
                        boundingBox.maxZ = vert.z;

                    verticesAndWorldHeight.vertices[i] = new PointF(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier);
                    verticesAndWorldHeight.worldHeight = vert.z;
                }

                verticesAndWorldHeightRangesList.Add(verticesAndWorldHeight);
            }

            return verticesAndWorldHeightRangesList;
        }


        private static void RenderBrushSides(Graphics graphics, BoundingBox boundingBox, List<BrushVerticesAndWorldHeight> verticesAndWorldHeightRangesList)
        {
            Pen pen = null;
            SolidBrush brush = null;

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

                pen = PenColours.PenLayout(gradientValue);
                brush = BrushColours.BrushLayout(gradientValue);

                DrawFilledPolygonObjective(graphics, brush, pen, verticesAndWorldHeightRanges.vertices);
            }

            pen?.Dispose();
            brush?.Dispose();
        }


        private static void RenderProps(Graphics graphics)
        {
            
        }


        private static void DisposeImage(Image img)
        {
            img.Dispose();
        }


        private static void DrawFilledPolygonObjective(Graphics graphics, SolidBrush brush, Pen pen, PointF[] vertices)
        {
            graphics.DrawPolygon(pen, vertices);
            graphics.FillPolygon(brush, vertices);
        }


        private static void SaveImage(Image img, string filepath)
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

            // only create the heatmaps if the files are not locked
            if (canSave)
            {
                img.Save(filepath, ImageFormat.Png);
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


        private static void FlipImage(Image img)
        {
            img.RotateFlip(RotateFlipType.RotateNoneFlipY);
        }


        private static void GenerateTxt()
        {
            overviewTxt.GetInExportableFormat(mapName);
        }
    }
}
