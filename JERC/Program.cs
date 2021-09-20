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
        private static readonly int[] TEMPORARYrgbColourPath = new int[3] { 127, 127, 127 };
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

            if (overviewPositionValues == null)
            {
                Console.WriteLine("---- No brushes or displacements found, exiting. ----");
                return;
            }

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

            // used for both world brushes and displacements
            var allWorldBrushesInVisgroup = from x in allWorldBrushes
                                            from y in x.Body
                                            where y.Name == "editor"
                                            from z in y.Body
                                            where z.Name == "visgroupid"
                                            where z.Value == visgroupId
                                            select x;

            var brushesRemove = GetBrushesRemove(allWorldBrushesInVisgroup);
            var brushesPath = GetBrushesPath(allWorldBrushesInVisgroup);
            var brushesCover = GetBrushesCover(allWorldBrushesInVisgroup);
            var brushesOverlap = GetBrushesOverlap(allWorldBrushesInVisgroup);

            var displacementsRemove = GetDisplacementsRemove(allWorldBrushesInVisgroup);
            var displacementsPath = GetDisplacementsPath(allWorldBrushesInVisgroup);
            var displacementsCover = GetDisplacementsCover(allWorldBrushesInVisgroup);
            var displacementsOverlap = GetDisplacementsOverlap(allWorldBrushesInVisgroup);

            var buyzoneBrushEntities = GetBuyzoneBrushEntities(allEntities);
            var bombsiteBrushEntities = GetBombsiteBrushEntities(allEntities);
            var rescueZoneBrushEntities = GetRescueZoneBrushEntities(allEntities);
            var hostageEntities = GetHostageEntities(allEntities);

            return new VmfRequiredData(
                brushesRemove, brushesPath, brushesCover, brushesOverlap,
                displacementsRemove, displacementsPath, displacementsCover, displacementsOverlap,
                buyzoneBrushEntities, bombsiteBrushEntities, rescueZoneBrushEntities, hostageEntities
            );
        }


        private static IEnumerable<IVNode> GetBrushesRemove(IEnumerable<IVNode> allWorldBrushesInVisgroup)
        {
            return from x in allWorldBrushesInVisgroup
                   from y in x.Body
                   where y.Name == "side"
                   where !y.Body.Any(z => z.Name == "dispinfo")
                   from z in y.Body
                   where z.Name == "material"
                   where z.Value.ToLower() == TextureNames.RemoveTextureName.ToLower()
                   select x;
        }


        private static IEnumerable<IVNode> GetBrushesPath(IEnumerable<IVNode> allWorldBrushesInVisgroup)
        {
            return from x in allWorldBrushesInVisgroup
                   from y in x.Body
                   where y.Name == "side"
                   where !y.Body.Any(z => z.Name == "dispinfo")
                   from z in y.Body
                   where z.Name == "material"
                   where z.Value.ToLower() == TextureNames.PathTextureName.ToLower()
                   select x;
        }


        private static IEnumerable<IVNode> GetBrushesCover(IEnumerable<IVNode> allWorldBrushesInVisgroup)
        {
            return from x in allWorldBrushesInVisgroup
                   from y in x.Body
                   where y.Name == "side"
                   where !y.Body.Any(z => z.Name == "dispinfo")
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
                   where !y.Body.Any(z => z.Name == "dispinfo")
                   from z in y.Body
                   where z.Name == "material"
                   where z.Value.ToLower() == TextureNames.OverlapTextureName.ToLower()
                   select x;
        }


        private static IEnumerable<IVNode> GetDisplacementsRemove(IEnumerable<IVNode> allWorldBrushesInVisgroup)
        {
            return from x in allWorldBrushesInVisgroup
                   from y in x.Body
                   where y.Name == "side"
                   where y.Body.Any(z => z.Name == "dispinfo")
                   from z in y.Body
                   where z.Name == "material"
                   where z.Value.ToLower() == TextureNames.RemoveTextureName.ToLower()
                   select x;
        }


        private static IEnumerable<IVNode> GetDisplacementsPath(IEnumerable<IVNode> allWorldBrushesInVisgroup)
        {
            return from x in allWorldBrushesInVisgroup
                   from y in x.Body
                   where y.Name == "side"
                   where y.Body.Any(z => z.Name == "dispinfo")
                   from z in y.Body
                   where z.Name == "material"
                   where z.Value.ToLower() == TextureNames.PathTextureName.ToLower()
                   select x;
        }


        private static IEnumerable<IVNode> GetDisplacementsCover(IEnumerable<IVNode> allWorldBrushesInVisgroup)
        {
            return from x in allWorldBrushesInVisgroup
                   from y in x.Body
                   where y.Name == "side"
                   where y.Body.Any(z => z.Name == "dispinfo")
                   from z in y.Body
                   where z.Name == "material"
                   where z.Value.ToLower() == TextureNames.CoverTextureName.ToLower()
                   select x;
        }


        private static IEnumerable<IVNode> GetDisplacementsOverlap(IEnumerable<IVNode> allWorldBrushesInVisgroup)
        {
            return from x in allWorldBrushesInVisgroup
                   from y in x.Body
                   where y.Name == "side"
                   where y.Body.Any(z => z.Name == "dispinfo")
                   from z in y.Body
                   where z.Name == "material"
                   where z.Value.ToLower() == TextureNames.OverlapTextureName.ToLower()
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
            var allWorldBrushesAndDisplacementsExceptRemove = vmfRequiredData.brushesSidesPath
                .Concat(vmfRequiredData.brushesSidesCover)
                .Concat(vmfRequiredData.brushesSidesOverlap)
                .Concat(vmfRequiredData.displacementsSidesPath)
                .Concat(vmfRequiredData.displacementsSidesCover)
                .Concat(vmfRequiredData.displacementsSidesOverlap);
            //var allWorldBrushes = vmfRequiredData.brushesSidesRemove.Concat(vmfRequiredData.displacementsSidesRemove).Concat(allWorldBrushesAndDisplacementsExceptRemove);

            if (allWorldBrushesAndDisplacementsExceptRemove == null || allWorldBrushesAndDisplacementsExceptRemove.Count() == 0)
                return null;

            var minX = allWorldBrushesAndDisplacementsExceptRemove.Min(x => x.vertices_plus.Min(y => y.x));
            var maxX = allWorldBrushesAndDisplacementsExceptRemove.Max(x => x.vertices_plus.Max(y => y.x));
            var minY = allWorldBrushesAndDisplacementsExceptRemove.Min(x => x.vertices_plus.Min(y => y.y));
            var maxY = allWorldBrushesAndDisplacementsExceptRemove.Max(x => x.vertices_plus.Max(y => y.y));

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
                var boundingBox = new BoundingBox();

                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                graphics.SetClip(Rectangle.FromLTRB(0, 0, overviewPositionValues.outputResolution, overviewPositionValues.outputResolution));

                // get all brushes, displacements and entities to draw
                var brushRemoveSideList = GetBrushRemoveOnlyVerticesList(boundingBox);
                var displacementRemoveSideList = GetDisplacementRemoveOnlyVerticesList(boundingBox);
                var brushExceptRemoveSideList = GetBrushExceptRemoveOnlyVerticesList(boundingBox);
                var displacementExceptRemoveSideList = GetDisplacementExceptRemoveOnlyVerticesList(boundingBox);
                var entityBrushSideListById = GetEntityVerticesListById();

                // add remove stuff first to set to graphics' clip
                GetBrushesToDrawOrAddRemoveRegion(bmp, graphics, boundingBox, overviewPositionValues, brushRemoveSideList);
                GetDisplacementsToDrawOrAddRemoveRegion(bmp, graphics, boundingBox, overviewPositionValues, displacementRemoveSideList);

                // non-remove stuff (for stroke)
                var brushesToDraw = GetBrushesToDrawOrAddRemoveRegion(bmp, graphics, boundingBox, overviewPositionValues, brushExceptRemoveSideList);
                foreach (var brushToRender in brushesToDraw)
                {
                    var strokeSolidBrush = new SolidBrush(Color.Transparent);
                    var strokePen = (Pen)brushToRender.pen.Clone();
                    strokePen.Color = Color.White;
                    strokePen.Width *= Sizes.StrokeWidthMultiplier;

                    DrawFilledPolygonObjectBrushes(graphics, strokeSolidBrush, strokePen, brushToRender.vertices);
                }

                var displacementsToDraw = GetDisplacementsToDrawOrAddRemoveRegion(bmp, graphics, boundingBox, overviewPositionValues, displacementExceptRemoveSideList);
                foreach (var displacementToRender in displacementsToDraw)
                {
                    var strokeSolidBrush = new SolidBrush(Color.Transparent);
                    var strokePen = (Pen)displacementToRender.pen.Clone();
                    strokePen.Color = Color.White;
                    strokePen.Width *= Sizes.StrokeWidthMultiplier;

                    DrawFilledPolygonObjectDisplacements(graphics, strokeSolidBrush, strokePen, displacementToRender.vertices);
                }

                // non-remove stuff next
                foreach (var brushToRender in brushesToDraw)
                {
                    DrawFilledPolygonObjectBrushes(graphics, brushToRender.solidBrush, brushToRender.pen, brushToRender.vertices);
                }

                foreach (var displacementToRender in displacementsToDraw)
                {
                    DrawFilledPolygonObjectDisplacements(graphics, displacementToRender.solidBrush, displacementToRender.pen, displacementToRender.vertices);
                }

                // reset the clip so that entity brushes can render anywhere
                graphics.ResetClip();

                // entities next
                var entitiesToDraw = GetEntitiesToDraw(bmp, graphics, boundingBox, overviewPositionValues, entityBrushSideListById);
                foreach (var entityToDraw in entitiesToDraw)
                {
                    DrawFilledPolygonObjectEntities(graphics, entityToDraw.solidBrush, entityToDraw.pen, entityToDraw.vertices);
                }

                graphics.Save();

                FlipImage(bmp);

                Bitmap bmpNew = new Bitmap(bmp, 1024, 1024);

                SaveImage(outputImageFilepath, bmpNew);

                DisposeImage(bmpNew);
            }

            DisposeImage(bmp);
        }


        private static List<BrushSide> GetBrushRemoveOnlyVerticesList(BoundingBox boundingBox)
        {
            var brushSideList = GetBrushRemoveVerticesList();

            if (brushSideList.Count() == 0)
                return brushSideList;

            boundingBox.minX = brushSideList.SelectMany(x => x.vertices.Select(y => y.X)).Min();
            boundingBox.maxX = brushSideList.SelectMany(x => x.vertices.Select(y => y.X)).Max();
            boundingBox.minY = brushSideList.SelectMany(x => x.vertices.Select(y => y.Y)).Min();
            boundingBox.maxY = brushSideList.SelectMany(x => x.vertices.Select(y => y.Y)).Max();

            boundingBox.minZ = brushSideList.Select(x => x.worldHeight).Min();
            boundingBox.maxZ = brushSideList.Select(x => x.worldHeight).Max();

            return brushSideList;
        }


        private static List<BrushSide> GetDisplacementRemoveOnlyVerticesList(BoundingBox boundingBox)
        {
            var displacementSideList = GetDisplacementRemoveVerticesList();

            if (displacementSideList.Count() == 0)
                return displacementSideList;

            boundingBox.minX = displacementSideList.SelectMany(x => x.vertices.Select(y => y.X)).Min();
            boundingBox.maxX = displacementSideList.SelectMany(x => x.vertices.Select(y => y.X)).Max();
            boundingBox.minY = displacementSideList.SelectMany(x => x.vertices.Select(y => y.Y)).Min();
            boundingBox.maxY = displacementSideList.SelectMany(x => x.vertices.Select(y => y.Y)).Max();

            boundingBox.minZ = displacementSideList.Select(x => x.worldHeight).Min();
            boundingBox.maxZ = displacementSideList.Select(x => x.worldHeight).Max();

            return displacementSideList;
        }


        private static List<BrushSide> GetBrushExceptRemoveOnlyVerticesList(BoundingBox boundingBox)
        {
            var brushSideList = new List<BrushSide>();

            brushSideList.AddRange(GetBrushPathVerticesList());
            brushSideList.AddRange(GetBrushCoverVerticesList());
            brushSideList.AddRange(GetBrushOverlapVerticesList());

            if (brushSideList.Count() == 0)
                return brushSideList;

            boundingBox.minX = brushSideList.SelectMany(x => x.vertices.Select(y => y.X)).Min();
            boundingBox.maxX = brushSideList.SelectMany(x => x.vertices.Select(y => y.X)).Max();
            boundingBox.minY = brushSideList.SelectMany(x => x.vertices.Select(y => y.Y)).Min();
            boundingBox.maxY = brushSideList.SelectMany(x => x.vertices.Select(y => y.Y)).Max();

            boundingBox.minZ = brushSideList.Select(x => x.worldHeight).Min();
            boundingBox.maxZ = brushSideList.Select(x => x.worldHeight).Max();

            return brushSideList;
        }


        private static List<BrushSide> GetDisplacementExceptRemoveOnlyVerticesList(BoundingBox boundingBox)
        {
            var displacementSideList = new List<BrushSide>();

            displacementSideList.AddRange(GetDisplacementPathVerticesList());
            displacementSideList.AddRange(GetDisplacementCoverVerticesList());
            displacementSideList.AddRange(GetDisplacementOverlapVerticesList());

            if (displacementSideList.Count() == 0)
                return displacementSideList;

            boundingBox.minX = displacementSideList.SelectMany(x => x.vertices.Select(y => y.X)).Min();
            boundingBox.maxX = displacementSideList.SelectMany(x => x.vertices.Select(y => y.X)).Max();
            boundingBox.minY = displacementSideList.SelectMany(x => x.vertices.Select(y => y.Y)).Min();
            boundingBox.maxY = displacementSideList.SelectMany(x => x.vertices.Select(y => y.Y)).Max();

            boundingBox.minZ = displacementSideList.Select(x => x.worldHeight).Min();
            boundingBox.maxZ = displacementSideList.Select(x => x.worldHeight).Max();

            return displacementSideList;
        }


        private static Dictionary<int, List<EntityBrushSide>> GetEntityVerticesListById()
        {
            var entityBrushSideListById = new Dictionary<int, List<EntityBrushSide>>();

            var entityBuyzoneVerticesListById = GetEntityBuyzoneVerticesList();
            var entityBombsiteVerticesListById = GetEntityBombsiteVerticesList();
            var entityRescueZoneVerticesListById = GetEntityRescueZoneVerticesList();

            if (entityBuyzoneVerticesListById != null && entityBuyzoneVerticesListById.Any())
            {
                foreach (var list in entityBuyzoneVerticesListById)
                {
                    entityBrushSideListById.Add(list.Key, list.Value);
                }
            }
            if (entityBombsiteVerticesListById != null && entityBombsiteVerticesListById.Any())
            {
                foreach (var list in entityBombsiteVerticesListById)
                {
                    entityBrushSideListById.Add(list.Key, list.Value);
                }
            }
            if (entityRescueZoneVerticesListById != null && entityRescueZoneVerticesListById.Any())
            {
                foreach (var list in entityRescueZoneVerticesListById)
                {
                    entityBrushSideListById.Add(list.Key, list.Value);
                }
            }

            return entityBrushSideListById;
        }


        private static List<BrushSide> GetBrushRemoveVerticesList()
        {
            var brushSideList = new List<BrushSide>();

            foreach (var side in vmfRequiredData.brushesSidesRemove)
            {
                var brushSide = new BrushSide(side.vertices_plus.Count());
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    brushSide.vertices[i] = new PointF(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier);
                    brushSide.worldHeight = vert.z;
                    brushSide.jercType = JercTypes.Remove;
                }

                brushSideList.Add(brushSide);
            }

            return brushSideList;
        }


        private static List<BrushSide> GetBrushPathVerticesList()
        {
            var brushSideList = new List<BrushSide>();

            foreach (var side in vmfRequiredData.brushesSidesPath)
            {
                var brushSide = new BrushSide(side.vertices_plus.Count());
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    brushSide.vertices[i] = new PointF(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier);
                    brushSide.worldHeight = vert.z;
                    brushSide.jercType = JercTypes.Path;
                }

                brushSideList.Add(brushSide);
            }

            return brushSideList;
        }


        private static List<BrushSide> GetBrushCoverVerticesList()
        {
            var brushSideList = new List<BrushSide>();

            foreach (var side in vmfRequiredData.brushesSidesCover)
            {
                var brushSide = new BrushSide(side.vertices_plus.Count());
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    brushSide.vertices[i] = new PointF(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier);
                    brushSide.worldHeight = vert.z;
                    brushSide.jercType = JercTypes.Cover;
                }

                brushSideList.Add(brushSide);
            }

            return brushSideList;
        }


        private static List<BrushSide> GetBrushOverlapVerticesList()
        {
            var brushSideList = new List<BrushSide>();

            foreach (var side in vmfRequiredData.brushesSidesOverlap)
            {
                var brushSide = new BrushSide(side.vertices_plus.Count());
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    brushSide.vertices[i] = new PointF(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier);
                    brushSide.worldHeight = vert.z;
                    brushSide.jercType = JercTypes.Overlap;
                }

                brushSideList.Add(brushSide);
            }

            return brushSideList;
        }


        private static List<BrushSide> GetDisplacementRemoveVerticesList()
        {
            var displacementSideList = new List<BrushSide>();

            foreach (var side in vmfRequiredData.displacementsSidesRemove)
            {
                var displacementSide = new BrushSide(side.vertices_plus.Count());
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    displacementSide.vertices[i] = new PointF(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier);
                    displacementSide.worldHeight = vert.z;
                    displacementSide.jercType = JercTypes.Remove;
                }

                displacementSideList.Add(displacementSide);
            }

            return displacementSideList;
        }


        private static List<BrushSide> GetDisplacementPathVerticesList()
        {
            var displacementSideList = new List<BrushSide>();

            foreach (var side in vmfRequiredData.displacementsSidesPath)
            {
                var displacementSide = new BrushSide(side.vertices_plus.Count());
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    displacementSide.vertices[i] = new PointF(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier);
                    displacementSide.worldHeight = vert.z;
                    displacementSide.jercType = JercTypes.Path;
                }

                displacementSideList.Add(displacementSide);
            }

            return displacementSideList;
        }


        private static List<BrushSide> GetDisplacementCoverVerticesList()
        {
            var displacementSideList = new List<BrushSide>();

            foreach (var side in vmfRequiredData.displacementsSidesCover)
            {
                var displacementSide = new BrushSide(side.vertices_plus.Count());
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    displacementSide.vertices[i] = new PointF(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier);
                    displacementSide.worldHeight = vert.z;
                    displacementSide.jercType = JercTypes.Cover;
                }

                displacementSideList.Add(displacementSide);
            }

            return displacementSideList;
        }


        private static List<BrushSide> GetDisplacementOverlapVerticesList()
        {
            var displacementSideList = new List<BrushSide>();

            foreach (var side in vmfRequiredData.displacementsSidesOverlap)
            {
                var displacementSide = new BrushSide(side.vertices_plus.Count());
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    displacementSide.vertices[i] = new PointF(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier);
                    displacementSide.worldHeight = vert.z;
                    displacementSide.jercType = JercTypes.Overlap;
                }

                displacementSideList.Add(displacementSide);
            }

            return displacementSideList;
        }


        private static Dictionary<int, List<EntityBrushSide>> GetEntityBuyzoneVerticesList()
        {
            var entityBrushSideListById = new Dictionary<int, List<EntityBrushSide>>();

            foreach (var entitySides in vmfRequiredData.entitiesSidesByEntityBuyzoneId)
            {
                foreach (var side in entitySides.Value)
                {
                    var entityBrushSide = new EntityBrushSide(side.vertices_plus.Count());
                    for (int i = 0; i < side.vertices_plus.Count(); i++)
                    {
                        var vert = side.vertices_plus[i];

                        entityBrushSide.vertices[i] = new PointF(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier);
                        entityBrushSide.worldHeight = vert.z;
                        entityBrushSide.entityType = EntityTypes.Buyzone;
                    }

                    if (entityBrushSideListById.ContainsKey(entitySides.Key))
                        entityBrushSideListById[entitySides.Key].Add(entityBrushSide);
                    else
                        entityBrushSideListById.Add(entitySides.Key, new List<EntityBrushSide>() { entityBrushSide });
                }
            }

            return entityBrushSideListById;
        }


        private static Dictionary<int, List<EntityBrushSide>> GetEntityBombsiteVerticesList()
        {
            var entityBrushSideListById = new Dictionary<int, List<EntityBrushSide>>();

            foreach (var entitySides in vmfRequiredData.entitiesSidesByEntityBombsiteId)
            {
                foreach (var side in entitySides.Value)
                {
                    var entityBrushSide = new EntityBrushSide(side.vertices_plus.Count());
                    for (int i = 0; i < side.vertices_plus.Count(); i++)
                    {
                        var vert = side.vertices_plus[i];

                        entityBrushSide.vertices[i] = new PointF(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier);
                        entityBrushSide.worldHeight = vert.z;
                        entityBrushSide.entityType = EntityTypes.Bombsite;
                    }

                    if (entityBrushSideListById.ContainsKey(entitySides.Key))
                        entityBrushSideListById[entitySides.Key].Add(entityBrushSide);
                    else
                        entityBrushSideListById.Add(entitySides.Key, new List<EntityBrushSide>() { entityBrushSide });
                }
            }

            return entityBrushSideListById;
        }


        private static Dictionary<int, List<EntityBrushSide>> GetEntityRescueZoneVerticesList()
        {
            var entityBrushSideListById = new Dictionary<int, List<EntityBrushSide>>();

            foreach (var entitySides in vmfRequiredData.entitiesSidesByEntityRescueZoneId)
            {
                foreach (var side in entitySides.Value)
                {
                    var entityBrushSide = new EntityBrushSide(side.vertices_plus.Count());
                    for (int i = 0; i < side.vertices_plus.Count(); i++)
                    {
                        var vert = side.vertices_plus[i];

                        entityBrushSide.vertices[i] = new PointF(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier);
                        entityBrushSide.worldHeight = vert.z;
                        entityBrushSide.entityType = EntityTypes.RescueZone;
                    }

                    if (entityBrushSideListById.ContainsKey(entitySides.Key))
                        entityBrushSideListById[entitySides.Key].Add(entityBrushSide);
                    else
                        entityBrushSideListById.Add(entitySides.Key, new List<EntityBrushSide>() { entityBrushSide });
                }
            }

            return entityBrushSideListById;
        }


        private static List<ObjectToDraw> GetBrushesToDrawOrAddRemoveRegion(Bitmap bmp, Graphics graphics, BoundingBox boundingBox, OverviewPositionValues overviewPositionValues, List<BrushSide> brushSideList)
        {
            var brushesToDraw = new List<ObjectToDraw>();

            foreach (var brushSide in brushSideList)
            {
                var heightAboveMin = brushSide.worldHeight - boundingBox.minZ;

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

                // corrects the verts to tax into account the movement from space in world to the space in the image (which starts at (0,0))
                var verticesOffset = brushSide.vertices;
                for (var i = 0; i < verticesOffset.Count(); i++)
                {
                    verticesOffset[i].X = verticesOffset[i].X - overviewPositionValues.brushVerticesPosMinX + overviewPositionValues.brushVerticesOffsetX;
                    verticesOffset[i].Y = verticesOffset[i].Y - overviewPositionValues.brushVerticesPosMinY + overviewPositionValues.brushVerticesOffsetY;
                }

                if (brushSide.jercType == JercTypes.Remove) // shouldn't be used as long as SetBrushesToDraw() is not called with remove brushes in brushSideList
                {
                    AddRemoveRegion(bmp, graphics, brushSide);
                }
                else
                {
                    Pen pen = brushSide.jercType switch
                    {
                        //JercTypes.Remove => PenColours.PenRemove(gradientValue),
                        JercTypes.Path => PenColours.PenPath(TEMPORARYrgbColourPath, gradientValue),
                        JercTypes.Cover => PenColours.PenCover(TEMPORARYrgbColourCover, gradientValue),
                        JercTypes.Overlap => PenColours.PenOverlap(TEMPORARYrgbColourOverlap, gradientValue),
                        _ => null,
                    };

                    SolidBrush solidBrush = brushSide.jercType switch
                    {
                        //JercTypes.Remove => BrushColours.SolidBrushRemove(gradientValue),
                        JercTypes.Path => SolidBrushColours.SolidBrushPath(TEMPORARYrgbColourPath, gradientValue),
                        JercTypes.Cover => SolidBrushColours.SolidBrushCover(TEMPORARYrgbColourCover, gradientValue),
                        JercTypes.Overlap => SolidBrushColours.SolidBrushOverlap(TEMPORARYrgbColourOverlap, gradientValue),
                        _ => null,
                    };


                    brushesToDraw.Add(new ObjectToDraw(verticesOffset, pen, solidBrush));
                }
            }

            return brushesToDraw;
        }


        private static List<ObjectToDraw> GetDisplacementsToDrawOrAddRemoveRegion(Bitmap bmp, Graphics graphics, BoundingBox boundingBox, OverviewPositionValues overviewPositionValues, List<BrushSide> displacementSideList)
        {
            var displacementsToDraw = new List<ObjectToDraw>();

            foreach (var displacementSide in displacementSideList)
            {
                var heightAboveMin = displacementSide.worldHeight - boundingBox.minZ;

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

                // corrects the verts to tax into account the movement from space in world to the space in the image (which starts at (0,0))
                var verticesOffset = displacementSide.vertices;
                for (var i = 0; i < verticesOffset.Count(); i++)
                {
                    verticesOffset[i].X = verticesOffset[i].X - overviewPositionValues.brushVerticesPosMinX + overviewPositionValues.brushVerticesOffsetX;
                    verticesOffset[i].Y = verticesOffset[i].Y - overviewPositionValues.brushVerticesPosMinY + overviewPositionValues.brushVerticesOffsetY;
                }

                if (displacementSide.jercType == JercTypes.Remove) // shouldn't be used as long as SetDisplacementsToDraw() is not called with remove displacements in brushSideList
                {
                    AddRemoveRegion(bmp, graphics, displacementSide);
                }
                else
                {
                    Pen pen = displacementSide.jercType switch
                    {
                        //JercTypes.Remove => PenColours.PenRemove(gradientValue),
                        JercTypes.Path => PenColours.PenPath(TEMPORARYrgbColourPath, gradientValue),
                        JercTypes.Cover => PenColours.PenCover(TEMPORARYrgbColourCover, gradientValue),
                        JercTypes.Overlap => PenColours.PenOverlap(TEMPORARYrgbColourOverlap, gradientValue),
                        _ => null,
                    };

                    SolidBrush solidBrush = displacementSide.jercType switch
                    {
                        //JercTypes.Remove => BrushColours.SolidBrushRemove(gradientValue),
                        JercTypes.Path => SolidBrushColours.SolidBrushPath(TEMPORARYrgbColourPath, gradientValue),
                        JercTypes.Cover => SolidBrushColours.SolidBrushCover(TEMPORARYrgbColourCover, gradientValue),
                        JercTypes.Overlap => SolidBrushColours.SolidBrushOverlap(TEMPORARYrgbColourOverlap, gradientValue),
                        _ => null,
                    };


                    displacementsToDraw.Add(new ObjectToDraw(verticesOffset, pen, solidBrush));
                }
            }

            return displacementsToDraw;
        }


        private static List<ObjectToDraw> GetEntitiesToDraw(Bitmap bmp, Graphics graphics, BoundingBox boundingBox, OverviewPositionValues overviewPositionValues, Dictionary<int, List<EntityBrushSide>> entityBrushSideListById)
        {
            var entitiesToDraw = new List<ObjectToDraw>();

            foreach (var entityBrushSideByBrush in entityBrushSideListById.Values)
            {
                foreach (var entityBrushSide in entityBrushSideByBrush)
                {
                    // corrects the verts to tax into account the movement from space in world to the space in the image (which starts at (0,0))
                    var verticesOffset = entityBrushSide.vertices;
                    for (var i = 0; i < verticesOffset.Count(); i++)
                    {
                        verticesOffset[i].X = verticesOffset[i].X - overviewPositionValues.brushVerticesPosMinX + overviewPositionValues.brushVerticesOffsetX;
                        verticesOffset[i].Y = verticesOffset[i].Y - overviewPositionValues.brushVerticesPosMinY + overviewPositionValues.brushVerticesOffsetY;
                    }

                    Pen pen = entityBrushSide.entityType switch
                    {
                        EntityTypes.Buyzone => PenColours.PenBuyzones(),
                        EntityTypes.Bombsite => PenColours.PenBombsites(),
                        EntityTypes.RescueZone => PenColours.PenRescueZones(),
                        _ => null,
                    };

                    SolidBrush solidBrush = entityBrushSide.entityType switch
                    {
                        EntityTypes.Buyzone => SolidBrushColours.SolidBrushBuyzones(),
                        EntityTypes.Bombsite => SolidBrushColours.SolidBrushBombsites(),
                        EntityTypes.RescueZone => SolidBrushColours.SolidBrushRescueZones(),
                        _ => null,
                    };


                    entitiesToDraw.Add(new ObjectToDraw(verticesOffset, pen, solidBrush));
                }
            }

            return entitiesToDraw;
        }


        private static void DisposeImage(Bitmap bmp)
        {
            bmp.Dispose();
        }


        private static void AddRemoveRegion(Bitmap bmp, Graphics graphics, BrushSide brushSide)
        {
            var verticesToUse = brushSide.vertices;
            if (verticesToUse.Length < 3)
            {
                return;
            }

            var graphicsPath = new GraphicsPath();
            graphicsPath.AddPolygon(verticesToUse);

            var region = new Region(graphicsPath);
            graphics.ExcludeClip(region);

            graphicsPath.CloseFigure();
        }


        private static void DrawFilledPolygonObjectBrushes(Graphics graphics, SolidBrush solidBrush, Pen pen, Point[] vertices)
        {
            graphics.DrawPolygon(pen, vertices);
            graphics.FillPolygon(solidBrush, vertices);

            pen?.Dispose();
            solidBrush?.Dispose();
        }


        private static void DrawFilledPolygonObjectDisplacements(Graphics graphics, SolidBrush solidBrush, Pen pen, Point[] vertices)
        {
            graphics.DrawPolygon(pen, vertices);
            graphics.FillPolygon(solidBrush, vertices);

            pen?.Dispose();
            solidBrush?.Dispose();
        }


        private static void DrawFilledPolygonObjectEntities(Graphics graphics, SolidBrush solidBrush, Pen pen, Point[] vertices)
        {
            //graphics.DrawPolygon(pen, vertices);
            graphics.FillPolygon(solidBrush, vertices);

            pen?.Dispose();
            solidBrush?.Dispose();
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
