﻿using ImageAlterer;
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




        private static readonly ImageProcessorExtender imageProcessorExtender = new ImageProcessorExtender();

        private static readonly string gameBinDirectoryPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\"));
        private static readonly string gameCsgoDirectoryPath = Path.GetFullPath(Path.Combine(Path.Combine(gameBinDirectoryPath, @"..\"), @"csgo\"));
        private static readonly string gameOverviewsDirectoryPath = Path.GetFullPath(Path.Combine(gameCsgoDirectoryPath, @"resource\overviews\"));

        private static string outputImageFilepathPart1;
        private static string outputImageFilepathPart2;
        private static string outputImageBackgroundLevelsFilepath;
        private static string outputTxtFilepath;

        private static readonly string visgroupName = "JERC";

        private static GameConfigurationValues gameConfigurationValues;

        private static JercConfigValues jercConfigValues;

        private static string visgroupId;

        private static string mapName;

        private static VMF vmf;
        private static VmfRequiredData vmfRequiredData;
        private static OverviewPositionValues overviewPositionValues;


        static void Main(string[] args)
        {
            gameConfigurationValues = new GameConfigurationValues(args);

            if (gameConfigurationValues == null || !gameConfigurationValues.VerifyAllValuesSet())
            {
                Console.WriteLine("Game configuration files missing. Check the compile configuration's parameters.");
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

            var lines = File.ReadAllLines(gameConfigurationValues.vmfFilepath);

            mapName = Path.GetFileNameWithoutExtension(gameConfigurationValues.vmfFilepath);


            // TODO: uncomment for release
            /*
            outputImageFilepathPart1 = string.Concat(gameOverviewsDirectoryPath, mapName);
            outputImageFilepathPart2 = "_radar";
            outputImageBackgroundLevelsFilepath = string.Concat(outputImageFilepathPart1, "_background_levels.png");
            outputTxtFilepath = string.Concat(gameOverviewsDirectoryPath, mapName, ".txt");
            */
            outputImageFilepathPart1 = @"F:\Coding Stuff\GitHub Files\JERC\jerc_test_map";
            outputImageFilepathPart2 = "_radar";
            outputImageBackgroundLevelsFilepath = @"F:\Coding Stuff\GitHub Files\JERC\jerc_test_map_background_levels.png";
            outputTxtFilepath = @"F:\Coding Stuff\GitHub Files\JERC\jerc_test_map.txt";


            vmf = new VMF(lines);

            SetVisgroupId();

            vmfRequiredData = GetVmfRequiredData();

            SortScaleStuff();

            if (overviewPositionValues == null)
            {
                Console.WriteLine("---- No brushes or displacements found, exiting. ----");
                return;
            }

            var levelHeights = GetLevelHeights();

            GenerateRadars(levelHeights);

            GenerateTxt(levelHeights);
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

            // brushes
            var brushesRemove = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.RemoveTextureName);
            var brushesPath = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.PathTextureName);
            var brushesCover = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.CoverTextureName);
            var brushesOverlap = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.OverlapTextureName);

            // displacements
            var displacementsRemove = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.RemoveTextureName);
            var displacementsPath = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.PathTextureName);
            var displacementsCover = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.CoverTextureName);
            var displacementsOverlap = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.OverlapTextureName);

            // entities (in game)
            var buyzoneBrushEntities = GetEntitiesByClassname(allEntities, Classnames.ClassnameBuyzone);
            var bombsiteBrushEntities = GetEntitiesByClassname(allEntities, Classnames.ClassnameBombsite);
            var rescueZoneBrushEntities = GetEntitiesByClassname(allEntities, Classnames.ClassnameRescueZone);
            var hostageEntities = GetEntitiesByClassname(allEntities, Classnames.ClassnameHostage);
            var ctSpawnEntities = GetEntitiesByClassname(allEntities, Classnames.ClassnameCTSpawn);
            var tSpawnEntities = GetEntitiesByClassname(allEntities, Classnames.ClassnameTSpawn);

            // entities (JERC)
            var jercConfigureEntities = GetEntitiesByClassname(allEntities, Classnames.JercConfigure);
            var jercDividerEntities = GetEntitiesByClassname(allEntities, Classnames.JercDivider);

            var allJercEntities = jercConfigureEntities.Concat(jercDividerEntities);

            jercConfigValues = new JercConfigValues(GetSettingsValuesFromJercEntities(allJercEntities), jercDividerEntities.Count());

            return new VmfRequiredData(
                brushesRemove, brushesPath, brushesCover, brushesOverlap,
                displacementsRemove, displacementsPath, displacementsCover, displacementsOverlap,
                buyzoneBrushEntities, bombsiteBrushEntities, rescueZoneBrushEntities, hostageEntities, ctSpawnEntities, tSpawnEntities,
                jercConfigureEntities, jercDividerEntities
            );
        }


        private static Dictionary<string, string> GetSettingsValuesFromJercEntities(IEnumerable<IVNode> jercEntities)
        {
            var jercEntitySettingsValues = new Dictionary<string, string>();

            // jerc_configure
            var jercConfigure = jercEntities.FirstOrDefault(x => x.Body.Any(y => y.Name == "classname" && y.Value == Classnames.JercConfigure)).Body;

            jercEntitySettingsValues.Add("alternateOutputPath", jercConfigure.FirstOrDefault(x => x.Name == "alternateOutputPath")?.Value ?? string.Empty);
            jercEntitySettingsValues.Add("onlyOutputToAlternatePath", jercConfigure.FirstOrDefault(x => x.Name == "onlyOutputToAlternatePath")?.Value);
            jercEntitySettingsValues.Add("backgroundFilename", jercConfigure.FirstOrDefault(x => x.Name == "backgroundFilename")?.Value ?? string.Empty);
            jercEntitySettingsValues.Add("pathColourHigh", jercConfigure.FirstOrDefault(x => x.Name == "pathColourHigh")?.Value);
            jercEntitySettingsValues.Add("pathColourLow", jercConfigure.FirstOrDefault(x => x.Name == "pathColourLow")?.Value);
            jercEntitySettingsValues.Add("coverColourHigh", jercConfigure.FirstOrDefault(x => x.Name == "coverColourHigh")?.Value);
            jercEntitySettingsValues.Add("coverColourLow", jercConfigure.FirstOrDefault(x => x.Name == "coverColourLow")?.Value);
            jercEntitySettingsValues.Add("overlapColourHigh", jercConfigure.FirstOrDefault(x => x.Name == "overlapColourHigh")?.Value);
            jercEntitySettingsValues.Add("overlapColourLow", jercConfigure.FirstOrDefault(x => x.Name == "overlapColourLow")?.Value);
            jercEntitySettingsValues.Add("strokeWidth", jercConfigure.FirstOrDefault(x => x.Name == "strokeWidth")?.Value);
            jercEntitySettingsValues.Add("strokeColour", jercConfigure.FirstOrDefault(x => x.Name == "strokeColour")?.Value);
            jercEntitySettingsValues.Add("strokeAroundMainMaterials", jercConfigure.FirstOrDefault(x => x.Name == "strokeAroundMainMaterials")?.Value);
            jercEntitySettingsValues.Add("strokeAroundRemoveMaterials", jercConfigure.FirstOrDefault(x => x.Name == "strokeAroundRemoveMaterials")?.Value);
            jercEntitySettingsValues.Add("defaultLevelNum", jercConfigure.FirstOrDefault(x => x.Name == "defaultLevelNum")?.Value);
            jercEntitySettingsValues.Add("levelBackgroundEnabled", jercConfigure.FirstOrDefault(x => x.Name == "levelBackgroundEnabled")?.Value);
            jercEntitySettingsValues.Add("levelBackgroundDarkenAlpha", jercConfigure.FirstOrDefault(x => x.Name == "levelBackgroundDarkenAlpha")?.Value);
            jercEntitySettingsValues.Add("levelBackgroundBlurAmount", jercConfigure.FirstOrDefault(x => x.Name == "levelBackgroundBlurAmount")?.Value);
            jercEntitySettingsValues.Add("higherLevelOutputName", jercConfigure.FirstOrDefault(x => x.Name == "higherLevelOutputName")?.Value);
            jercEntitySettingsValues.Add("lowerLevelOutputName", jercConfigure.FirstOrDefault(x => x.Name == "lowerLevelOutputName")?.Value);
            jercEntitySettingsValues.Add("exportTxt", jercConfigure.FirstOrDefault(x => x.Name == "exportTxt")?.Value);
            jercEntitySettingsValues.Add("exportDds", jercConfigure.FirstOrDefault(x => x.Name == "exportDds")?.Value);
            jercEntitySettingsValues.Add("exportPng", jercConfigure.FirstOrDefault(x => x.Name == "exportPng")?.Value);
            jercEntitySettingsValues.Add("exportRadarAsSeparateLevels", jercConfigure.FirstOrDefault(x => x.Name == "exportRadarAsSeparateLevels")?.Value);
            jercEntitySettingsValues.Add("exportBackgroundLevelsImage", jercConfigure.FirstOrDefault(x => x.Name == "exportBackgroundLevelsImage")?.Value);


            // 



            return jercEntitySettingsValues;
        }


        private static IEnumerable<IVNode> GetBrushesByTextureName(IEnumerable<IVNode> allWorldBrushesInVisgroup, string textureName)
        {
            return (from x in allWorldBrushesInVisgroup
                   from y in x.Body
                   where y.Name == "side"
                   where !y.Body.Any(z => z.Name == "dispinfo")
                   from z in y.Body
                   where z.Name == "material"
                   where z.Value.ToLower() == textureName.ToLower()
                   select x).Distinct();
        }


        private static IEnumerable<IVNode> GetDisplacementsByTextureName(IEnumerable<IVNode> allWorldBrushesInVisgroup, string textureName)
        {
            return (from x in allWorldBrushesInVisgroup
                   from y in x.Body
                   where y.Name == "side"
                   where y.Body.Any(z => z.Name == "dispinfo")
                   from z in y.Body
                   where z.Name == "material"
                   where z.Value.ToLower() == textureName.ToLower()
                   select x).Distinct();
        }


        private static IEnumerable<IVNode> GetEntitiesByClassname(IEnumerable<IVNode> allEntities, string classname)
        {
            return (from x in allEntities
                   from y in x.Body
                   where y.Name == "classname"
                   where y.Value.ToLower() == classname.ToLower()
                   select x).Distinct();
        }


        private static void SortScaleStuff()
        {
            var allWorldBrushesAndDisplacementsExceptRemove = vmfRequiredData.brushesSidesPath
                .Concat(vmfRequiredData.brushesSidesCover)
                .Concat(vmfRequiredData.brushesSidesOverlap)
                .Concat(vmfRequiredData.displacementsSidesPath)
                .Concat(vmfRequiredData.displacementsSidesCover)
                .Concat(vmfRequiredData.displacementsSidesOverlap);
            //var allWorldBrushes = vmfRequiredData.brushesSidesRemove.Concat(vmfRequiredData.displacementsSidesRemove).Concat(allWorldBrushesAndDisplacementsExceptRemove);

            if (allWorldBrushesAndDisplacementsExceptRemove == null || allWorldBrushesAndDisplacementsExceptRemove.Count() == 0)
                return;

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

            overviewPositionValues = new OverviewPositionValues(jercConfigValues, minX, maxX, minY, maxY, scale);

            var pixelsPerUnitX = overviewPositionValues.outputResolution / sizeX;
            var pixelsPerUnitY = overviewPositionValues.outputResolution / sizeY;

            var unitsPerPixelX = sizeX / overviewPositionValues.outputResolution;
            var unitsPerPixelY = sizeY / overviewPositionValues.outputResolution;
        }


        private static List<LevelHeight> GetLevelHeights()
        {
            var levelHeights = new List<LevelHeight>();

            var jercDividerEntities = vmfRequiredData.jercDividerEntities.ToList();
            if (jercDividerEntities.Count() == 0)
                return null;

            var numOfOverviewLevels = jercDividerEntities.Count() + 1;
            for (int i = 0; i < numOfOverviewLevels; i++)
            {
                var overviewLevelName = string.Empty;

                var valueDiff = i - jercConfigValues.defaultLevelNum;
                if (valueDiff == 0)
                    overviewLevelName = "default";
                else if (valueDiff < 0)
                    overviewLevelName = string.Concat(jercConfigValues.lowerLevelOutputName, Math.Abs(valueDiff));
                else if (valueDiff > 0)
                    overviewLevelName = string.Concat(jercConfigValues.higherLevelOutputName, Math.Abs(valueDiff));

                var zMin = i == 0 ? -(Sizes.MaxHammerGridSize / 2) : levelHeights.ElementAt(i - 1).zMax;
                var zMax = i == (numOfOverviewLevels - 1) ? (Sizes.MaxHammerGridSize / 2) : new Vertices(jercDividerEntities.ElementAt(i).origin).z;

                levelHeights.Add(new LevelHeight(levelHeights.Count(), overviewLevelName, zMin, zMax));
            }

            return levelHeights;
        }


        private static void GenerateRadars(List<LevelHeight> levelHeights)
        {
            var radarLevels = new List<RadarLevel>();

            // get overview for each separate level if levelBackgroundEnabled == true
            if (jercConfigValues.exportRadarAsSeparateLevels && levelHeights != null && levelHeights.Count() > 1)
            {
                foreach (var levelHeight in levelHeights)
                {
                    var radarLevel = GenerateRadarLevel(levelHeight);
                    radarLevels.Add(radarLevel);
                }
            }
            else
            {
                var levelHeight = new LevelHeight(0, "default", -Sizes.MaxHammerGridSize, Sizes.MaxHammerGridSize);
                var radarLevel = GenerateRadarLevel(levelHeight);
                radarLevels.Add(radarLevel);
            }

            var radarLevelsToSaveList = new List<RadarLevel>();

            // add darkened background for all overview levels if levelBackgroundEnabled == true
            if (jercConfigValues.exportRadarAsSeparateLevels && levelHeights != null && levelHeights.Count() > 1 && jercConfigValues.levelBackgroundEnabled)
            {
                var backgroundBmp = GetBackgroundToRadarLevels(radarLevels);

                foreach (var radarLevel in radarLevels)
                {
                    Bitmap newBmp = new Bitmap(radarLevels.FirstOrDefault().bmp);
                    Graphics newGraphics = Graphics.FromImage(newBmp);
                    //newGraphics.ResetClip();
                    //newGraphics.Clear(Color.Transparent);

                    // apply blurred background levels to new image
                    newGraphics.CompositingMode = CompositingMode.SourceCopy;
                    newGraphics.DrawImage(backgroundBmp, 0, 0);
                    newGraphics.Save();
                    newGraphics.CompositingMode = CompositingMode.SourceOver;
                    newGraphics.DrawImage(radarLevel.bmp, 0, 0);
                    newGraphics.Save();

                    radarLevelsToSaveList.Add(new RadarLevel(newBmp, radarLevel.levelHeight));
                }

                // dispose
                DisposeGraphics(Graphics.FromImage(backgroundBmp));
                DisposeImage(backgroundBmp);
            }
            else
            {
                radarLevelsToSaveList = radarLevels;
            }

            // save overview levels
            foreach (var radarLevel in radarLevelsToSaveList)
            {
                FlipImage(radarLevel.bmp);

                SaveRadarLevel(radarLevel);
            }

            // dispose
            foreach (var radarLevel in radarLevels.Concat(radarLevelsToSaveList))
            {
                DisposeGraphics(radarLevel.graphics);
                DisposeImage(radarLevel.bmp);
            }
        }


        private static RadarLevel GenerateRadarLevel(LevelHeight levelHeight)
        {
            Bitmap bmp = new Bitmap(overviewPositionValues.outputResolution, overviewPositionValues.outputResolution);

            var graphics = Graphics.FromImage(bmp);

            var boundingBox = new BoundingBox(
                overviewPositionValues.brushVerticesPosMinX, overviewPositionValues.brushVerticesPosMaxX,
                overviewPositionValues.brushVerticesPosMinY, overviewPositionValues.brushVerticesPosMaxY,
                levelHeight.zMin, levelHeight.zMax
            );

            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            graphics.SetClip(Rectangle.FromLTRB(0, 0, overviewPositionValues.outputResolution, overviewPositionValues.outputResolution));

            // get all brush sides and displacement sides to draw
            var brushRemoveList = GetBrushRemoveVerticesList().Where(x => !(
                (x.brushSides.SelectMany(y => y.vertices).All(y => y.z < levelHeight.zMin) && x.brushSides.SelectMany(y => y.vertices).All(y => y.z < levelHeight.zMax)) ||
                (x.brushSides.SelectMany(y => y.vertices).All(y => y.z >= levelHeight.zMin) && x.brushSides.SelectMany(y => y.vertices).All(y => y.z >= levelHeight.zMax))
            )).ToList();
            var displacementRemoveList = GetDisplacementRemoveVerticesList().Where(x => !(
                (x.brushSides.SelectMany(y => y.vertices).All(y => y.z < levelHeight.zMin) && x.brushSides.SelectMany(y => y.vertices).All(y => y.z < levelHeight.zMax)) ||
                (x.brushSides.SelectMany(y => y.vertices).All(y => y.z >= levelHeight.zMin) && x.brushSides.SelectMany(y => y.vertices).All(y => y.z >= levelHeight.zMax))
            )).ToList();

            var brushCoverList = GetBrushCoverVerticesList().Where(x => !(
                (x.brushSides.SelectMany(y => y.vertices).All(y => y.z < levelHeight.zMin) && x.brushSides.SelectMany(y => y.vertices).All(y => y.z < levelHeight.zMax)) ||
                (x.brushSides.SelectMany(y => y.vertices).All(y => y.z >= levelHeight.zMin) && x.brushSides.SelectMany(y => y.vertices).All(y => y.z >= levelHeight.zMax))
            )).ToList();
            var displacementCoverList = GetDisplacementCoverVerticesList().Where(x => !(
                (x.brushSides.SelectMany(y => y.vertices).All(y => y.z < levelHeight.zMin) && x.brushSides.SelectMany(y => y.vertices).All(y => y.z < levelHeight.zMax)) ||
                (x.brushSides.SelectMany(y => y.vertices).All(y => y.z >= levelHeight.zMin) && x.brushSides.SelectMany(y => y.vertices).All(y => y.z >= levelHeight.zMax))
            )).ToList();

            var brushPathAndOverlapSideList = GetBrushPathAndOverlapSideOnlyVerticesList().Where(x => !(
                (x.vertices.All(y => y.z < levelHeight.zMin) && x.vertices.All(y => y.z < levelHeight.zMax)) ||
                (x.vertices.All(y => y.z >= levelHeight.zMin) && x.vertices.All(y => y.z >= levelHeight.zMax))
            )).ToList();
            var displacementPathAndOverlapSideList = GetDisplacementPathAndOverlapSideOnlyVerticesList().Where(x => !(
                (x.vertices.All(y => y.z < levelHeight.zMin) && x.vertices.All(y => y.z < levelHeight.zMax)) ||
                (x.vertices.All(y => y.z >= levelHeight.zMin) && x.vertices.All(y => y.z >= levelHeight.zMax))
            )).ToList();


            var allBrushSidesExceptRemove = brushPathAndOverlapSideList.Concat(brushCoverList.SelectMany(x => x.brushSides)).ToList();
            var allDisplacementSidesExceptRemove = displacementPathAndOverlapSideList.Concat(brushCoverList.SelectMany(x => x.brushSides)).ToList();


            // get all entity sides to draw
            var entityBrushSideListByIdUnfiltered = GetEntityVerticesListById();
            var entityBrushSideListById = new Dictionary<int, List<EntityBrushSide>>();
            foreach (var entityBrushSideById in entityBrushSideListByIdUnfiltered)
            {
                if (entityBrushSideById.Value.Any(x => !((x.vertices.All(y => y.z < levelHeight.zMin) && x.vertices.All(y => y.z < levelHeight.zMax)) || (x.vertices.All(y => y.z >= levelHeight.zMin) && x.vertices.All(y => y.z >= levelHeight.zMax))))) // would this allow entities to be on more than 1 level if their brushes span across level dividers ??
                {
                    entityBrushSideListById.Add(entityBrushSideById.Key, entityBrushSideById.Value);
                }
            }


            // add remove stuff first to set to graphics' clip
            AddRemoveRegion(bmp, graphics, brushRemoveList);
            AddRemoveRegion(bmp, graphics, displacementRemoveList);

            // non-remove stuff (for stroke)
            var brushesToDraw = GetBrushesToDraw(bmp, graphics, boundingBox, allBrushSidesExceptRemove);

            if (jercConfigValues.strokeAroundMainMaterials)
            {
                foreach (var brushToRender in brushesToDraw)
                {
                    var strokeSolidBrush = new SolidBrush(Color.Transparent);
                    var strokePen = (Pen)brushToRender.pen.Clone();
                    strokePen.Color = Color.White;
                    strokePen.Width *= jercConfigValues.strokeWidth;

                    DrawFilledPolygonObjectBrushes(graphics, strokeSolidBrush, strokePen, brushToRender.vertices);
                }
            }

            var displacementsToDraw = GetBrushesToDraw(bmp, graphics, boundingBox, allDisplacementSidesExceptRemove);

            if (jercConfigValues.strokeAroundMainMaterials)
            {
                foreach (var displacementToRender in displacementsToDraw)
                {
                    var strokeSolidBrush = new SolidBrush(Color.Transparent);
                    var strokePen = (Pen)displacementToRender.pen.Clone();
                    strokePen.Color = Color.White;
                    strokePen.Width *= jercConfigValues.strokeWidth;

                    DrawFilledPolygonObjectDisplacements(graphics, strokeSolidBrush, strokePen, displacementToRender.vertices);
                }
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
            ///////graphics.ResetClip();

            // entities next
            var entitiesToDraw = GetEntitiesToDraw(bmp, graphics, boundingBox, overviewPositionValues, entityBrushSideListById);

            foreach (var entityToDraw in entitiesToDraw)
            {
                DrawFilledPolygonObjectEntities(graphics, entityToDraw.solidBrush, entityToDraw.pen, entityToDraw.vertices);
            }

            graphics.Save();

            //FlipImage(bmp);

            return new RadarLevel(bmp, levelHeight);
        }


        private static Bitmap GetBackgroundToRadarLevels(List<RadarLevel> radarLevels)
        {
            var allGraphics = radarLevels.Select(x => x.graphics);

            Bitmap backgroundBmp = new Bitmap(radarLevels.FirstOrDefault().bmp);
            Graphics backgroundGraphics = Graphics.FromImage(backgroundBmp);
            backgroundGraphics.ResetClip();
            backgroundGraphics.Clear(Color.Transparent);

            // draw all levels on top of one another, bottom first
            foreach (var radarLevel in radarLevels)
            {
                //radarLevel.graphics.ResetClip();
                backgroundGraphics.CompositingMode = CompositingMode.SourceOver;
                backgroundGraphics.DrawImage(radarLevel.bmp, 0, 0);
            }

            // get transparent pixels
            var transparentPixelLocations = new List<Vertices>();
            for (int x = 0; x < backgroundBmp.Width; x++)
            {
                for (int y = 0; y < backgroundBmp.Height; y++)
                {
                    if (backgroundBmp.GetPixel(x, y).A == 0)
                    {
                        transparentPixelLocations.Add(new Vertices(x, y));
                    }
                }
            }

            // darken backgroundGraphics
            var rectangle = Rectangle.FromLTRB(0, 0, overviewPositionValues.outputResolution, overviewPositionValues.outputResolution);
            GraphicsPath path = new GraphicsPath();
            path.AddRectangle(rectangle);
            Region r1 = new Region(path);
            backgroundGraphics.FillRectangle(new SolidBrush(Color.FromArgb(jercConfigValues.levelBackgroundDarkenAlpha, 0, 0, 0)), rectangle);

            // reapply the transparent pixels
            foreach (var pixel in transparentPixelLocations)
            {
                backgroundBmp.SetPixel((int)pixel.x, (int)pixel.y, Color.Transparent);
            }

            // save graphics
            backgroundGraphics.Save();

            // blur and save background image
            backgroundBmp = new Bitmap(backgroundBmp, Sizes.FinalOutputImageResolution, Sizes.FinalOutputImageResolution);
            var imageFactoryBlurred = imageProcessorExtender.GetBlurredImage(backgroundBmp, jercConfigValues.levelBackgroundBlurAmount); //blur the image
            backgroundBmp = new Bitmap(backgroundBmp, overviewPositionValues.outputResolution, overviewPositionValues.outputResolution);

            if (jercConfigValues.exportBackgroundLevelsImage)
                imageFactoryBlurred.Save(outputImageBackgroundLevelsFilepath);

            imageFactoryBlurred.Dispose();

            return backgroundBmp;
        }


        private static void SaveRadarLevel(RadarLevel radarLevel)
        {
            radarLevel.bmp = new Bitmap(radarLevel.bmp, Sizes.FinalOutputImageResolution, Sizes.FinalOutputImageResolution);

            var radarLevelString = radarLevel.levelHeight.levelName.ToLower() == "default" ? string.Empty : string.Concat("_", radarLevel.levelHeight.levelName.ToLower());
            var outputImageFilepath = string.Concat(outputImageFilepathPart1, radarLevelString, outputImageFilepathPart2);

            SaveImage(outputImageFilepath, radarLevel.bmp);
        }


        // returns brush sides
        private static List<BrushSide> GetBrushPathAndOverlapSideOnlyVerticesList()
        {
            var brushSideList = new List<BrushSide>();

            brushSideList.AddRange(GetBrushPathSidesVerticesList());
            brushSideList.AddRange(GetBrushOverlapSidesVerticesList());

            return brushSideList;
        }


        // returns brush sides
        private static List<BrushSide> GetDisplacementPathAndOverlapSideOnlyVerticesList()
        {
            var displacementSideList = new List<BrushSide>();

            displacementSideList.AddRange(GetDisplacementPathSidesVerticesList());
            displacementSideList.AddRange(GetDisplacementOverlapSidesVerticesList());

            return displacementSideList;
        }


        private static Dictionary<int, List<EntityBrushSide>> GetEntityVerticesListById()
        {
            var entityBrushSideListById = new Dictionary<int, List<EntityBrushSide>>();

            var entityBuyzoneVerticesListById = GetEntityBuyzoneSidesVerticesList();
            var entityBombsiteVerticesListById = GetEntityBombsiteSidesVerticesList();
            var entityRescueZoneVerticesListById = GetEntityRescueZoneSidesVerticesList();

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


        private static List<BrushVolume> GetBrushRemoveVerticesList()
        {
            var brushList = new List<BrushVolume>();
            
            foreach (var brush in vmfRequiredData.brushesRemove)
            {
                var brushNew = new BrushVolume();
                foreach (var brushSide in brush.side.ToList())
                {
                    var brushSideNew = new BrushSide();
                    foreach (var vertices in brushSide.vertices_plus.ToList())
                    {
                        brushSideNew.vertices.Add(new Vertices(vertices.x / Sizes.SizeReductionMultiplier, vertices.y / Sizes.SizeReductionMultiplier, vertices.z));
                        brushSideNew.jercType = JercTypes.Remove;
                    }

                    brushNew.brushSides.Add(brushSideNew);
                }

                brushNew.jercType = JercTypes.Remove;

                brushList.Add(brushNew);
            }

            return brushList;
        }


        /*private static List<BrushSide> GetBrushRemoveSidesVerticesList()
        {
            var brushSideList = new List<BrushSide>();

            foreach (var side in vmfRequiredData.brushesSidesRemove)
            {
                var brushSide = new BrushSide();
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    brushSide.vertices.Add(new Vertices(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier, vert.z));
                    brushSide.jercType = JercTypes.Remove;
                }

                brushSideList.Add(brushSide);
            }

            return brushSideList;
        }*/


        private static List<BrushSide> GetBrushPathSidesVerticesList()
        {
            var brushSideList = new List<BrushSide>();

            foreach (var side in vmfRequiredData.brushesSidesPath)
            {
                var brushSide = new BrushSide();
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    brushSide.vertices.Add(new Vertices(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier, vert.z));
                    brushSide.jercType = JercTypes.Path;
                }

                brushSideList.Add(brushSide);
            }

            return brushSideList;
        }


        private static List<BrushVolume> GetBrushCoverVerticesList()
        {
            var brushList = new List<BrushVolume>();

            foreach (var brush in vmfRequiredData.brushesCover)
            {
                var brushNew = new BrushVolume();
                foreach (var brushSide in brush.side.ToList())
                {
                    var brushSideNew = new BrushSide();
                    foreach (var vertices in brushSide.vertices_plus.ToList())
                    {
                        brushSideNew.vertices.Add(new Vertices(vertices.x / Sizes.SizeReductionMultiplier, vertices.y / Sizes.SizeReductionMultiplier, vertices.z));
                        brushSideNew.jercType = JercTypes.Cover;
                    }

                    brushNew.brushSides.Add(brushSideNew);
                }

                brushNew.jercType = JercTypes.Cover;

                brushList.Add(brushNew);
            }

            return brushList;
        }


        /*private static List<BrushSide> GetBrushCoverSidesVerticesList()
        {
            var brushSideList = new List<BrushSide>();

            foreach (var side in vmfRequiredData.brushesSidesCover)
            {
                var brushSide = new BrushSide();
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    brushSide.vertices.Add(new Vertices(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier, vert.z));
                    brushSide.jercType = JercTypes.Cover;
                }

                brushSideList.Add(brushSide);
            }

            return brushSideList;
        }*/


        private static List<BrushSide> GetBrushOverlapSidesVerticesList()
        {
            var brushSideList = new List<BrushSide>();

            foreach (var side in vmfRequiredData.brushesSidesOverlap)
            {
                var brushSide = new BrushSide();
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    brushSide.vertices.Add(new Vertices(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier, vert.z));
                    brushSide.jercType = JercTypes.Overlap;
                }

                brushSideList.Add(brushSide);
            }

            return brushSideList;
        }


        private static List<BrushVolume> GetDisplacementRemoveVerticesList()
        {
            var displacementList = new List<BrushVolume>();

            foreach (var displacement in vmfRequiredData.displacementsRemove)
            {
                var displacementNew = new BrushVolume();
                foreach (var displacementSide in displacement.side.ToList())
                {
                    var displacementSideNew = new BrushSide();
                    foreach (var vertices in displacementSide.vertices_plus.ToList())
                    {
                        displacementSideNew.vertices.Add(new Vertices(vertices.x / Sizes.SizeReductionMultiplier, vertices.y / Sizes.SizeReductionMultiplier, vertices.z));
                        displacementSideNew.jercType = JercTypes.Remove;
                    }

                    displacementNew.brushSides.Add(displacementSideNew);
                }

                displacementNew.jercType = JercTypes.Remove;

                displacementList.Add(displacementNew);
            }

            return displacementList;
        }


        /*private static List<BrushSide> GetDisplacementRemoveSidesVerticesList()
        {
            var displacementSideList = new List<BrushSide>();

            foreach (var side in vmfRequiredData.displacementsSidesRemove)
            {
                var displacementSide = new BrushSide();
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    displacementSide.vertices.Add(new Vertices(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier, vert.z));
                    displacementSide.jercType = JercTypes.Remove;
                }

                displacementSideList.Add(displacementSide);
            }

            return displacementSideList;
        }*/


        private static List<BrushSide> GetDisplacementPathSidesVerticesList()
        {
            var displacementSideList = new List<BrushSide>();

            foreach (var side in vmfRequiredData.displacementsSidesPath)
            {
                var displacementSide = new BrushSide();
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    displacementSide.vertices.Add(new Vertices(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier, vert.z));
                    displacementSide.jercType = JercTypes.Path;
                }

                displacementSideList.Add(displacementSide);
            }

            return displacementSideList;
        }


        private static List<BrushVolume> GetDisplacementCoverVerticesList()
        {
            var displacementList = new List<BrushVolume>();

            foreach (var displacement in vmfRequiredData.displacementsCover)
            {
                var displacementNew = new BrushVolume();
                foreach (var displacementSide in displacement.side.ToList())
                {
                    var displacementSideNew = new BrushSide();
                    foreach (var vertices in displacementSide.vertices_plus.ToList())
                    {
                        displacementSideNew.vertices.Add(new Vertices(vertices.x / Sizes.SizeReductionMultiplier, vertices.y / Sizes.SizeReductionMultiplier, vertices.z));
                        displacementSideNew.jercType = JercTypes.Cover;
                    }

                    displacementNew.brushSides.Add(displacementSideNew);
                }

                displacementNew.jercType = JercTypes.Cover;

                displacementList.Add(displacementNew);
            }

            return displacementList;
        }


        /*private static List<BrushSide> GetDisplacementCoverSidesVerticesList()
        {
            var displacementSideList = new List<BrushSide>();

            foreach (var side in vmfRequiredData.displacementsSidesCover)
            {
                var displacementSide = new BrushSide();
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    displacementSide.vertices.Add(new Vertices(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier, vert.z));
                    displacementSide.jercType = JercTypes.Cover;
                }

                displacementSideList.Add(displacementSide);
            }

            return displacementSideList;
        }*/


        private static List<BrushSide> GetDisplacementOverlapSidesVerticesList()
        {
            var displacementSideList = new List<BrushSide>();

            foreach (var side in vmfRequiredData.displacementsSidesOverlap)
            {
                var displacementSide = new BrushSide();
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    displacementSide.vertices.Add(new Vertices(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier, vert.z));
                    displacementSide.jercType = JercTypes.Overlap;
                }

                displacementSideList.Add(displacementSide);
            }

            return displacementSideList;
        }


        private static Dictionary<int, List<EntityBrushSide>> GetEntityBuyzoneSidesVerticesList()
        {
            var entityBrushSideListById = new Dictionary<int, List<EntityBrushSide>>();

            foreach (var entitySides in vmfRequiredData.entitiesSidesByEntityBuyzoneId)
            {
                foreach (var side in entitySides.Value)
                {
                    var entityBrushSide = new EntityBrushSide();
                    for (int i = 0; i < side.vertices_plus.Count(); i++)
                    {
                        var vert = side.vertices_plus[i];

                        entityBrushSide.vertices.Add(new Vertices(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier, vert.z));
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


        private static Dictionary<int, List<EntityBrushSide>> GetEntityBombsiteSidesVerticesList()
        {
            var entityBrushSideListById = new Dictionary<int, List<EntityBrushSide>>();

            foreach (var entitySides in vmfRequiredData.entitiesSidesByEntityBombsiteId)
            {
                foreach (var side in entitySides.Value)
                {
                    var entityBrushSide = new EntityBrushSide();
                    for (int i = 0; i < side.vertices_plus.Count(); i++)
                    {
                        var vert = side.vertices_plus[i];

                        entityBrushSide.vertices.Add(new Vertices(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier, vert.z));
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


        private static Dictionary<int, List<EntityBrushSide>> GetEntityRescueZoneSidesVerticesList()
        {
            var entityBrushSideListById = new Dictionary<int, List<EntityBrushSide>>();

            foreach (var entitySides in vmfRequiredData.entitiesSidesByEntityRescueZoneId)
            {
                foreach (var side in entitySides.Value)
                {
                    var entityBrushSide = new EntityBrushSide();
                    for (int i = 0; i < side.vertices_plus.Count(); i++)
                    {
                        var vert = side.vertices_plus[i];

                        entityBrushSide.vertices.Add(new Vertices(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier, vert.z));
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


        private static void AddRemoveRegion(Bitmap bmp, Graphics graphics, List<BrushVolume> brushList)
        {
            foreach (var brush in brushList)
            {
                if (brush.jercType != JercTypes.Remove)
                    continue;

                // corrects the verts to tax into account the movement from space in world to the space in the image (which starts at (0,0))
                foreach (var side in brush.brushSides)
                {
                    var verticesOffset = side.vertices;
                    for (var i = 0; i < verticesOffset.Count(); i++)
                    {
                        verticesOffset[i].x = verticesOffset[i].x - overviewPositionValues.brushVerticesPosMinX + overviewPositionValues.brushVerticesOffsetX;
                        verticesOffset[i].y = verticesOffset[i].y - overviewPositionValues.brushVerticesPosMinY + overviewPositionValues.brushVerticesOffsetY;
                    }

                    AddRemoveRegion(bmp, graphics, side);
                }
            }
        }


        private static List<ObjectToDraw> GetBrushesToDraw(Bitmap bmp, Graphics graphics, BoundingBox boundingBox, List<BrushSide> brushSidesList)
        {
            var brushesToDraw = new List<ObjectToDraw>();

            foreach (var brushSide in brushSidesList)
            {
                var heightAboveMin = brushSide.vertices.Max(x => x.z) - boundingBox.minZ; // TODO: is this a place where it should do some logic for blending colours for different heights? Should Max() be removed?

                float? percentageAboveMin;
                if (heightAboveMin == 0)
                {
                    if (boundingBox.minZ == boundingBox.maxZ)
                    {
                        percentageAboveMin = 1.00f;
                    }
                    else
                    {
                        percentageAboveMin = 0.01f;
                    }
                }
                else
                {
                    percentageAboveMin = heightAboveMin / (boundingBox.maxZ - boundingBox.minZ);
                }

                var gradientValue = (int)Math.Round((float)percentageAboveMin * 255, 0);

                if (gradientValue < 1)
                    gradientValue = 1;
                else if (gradientValue > 255)
                    gradientValue = 255;

                // corrects the verts to tax into account the movement from space in world to the space in the image (which starts at (0,0))
                var verticesOffset = brushSide.vertices;
                for (var i = 0; i < verticesOffset.Count(); i++)
                {
                    verticesOffset[i].x = verticesOffset[i].x - overviewPositionValues.brushVerticesPosMinX + overviewPositionValues.brushVerticesOffsetX;
                    verticesOffset[i].y = verticesOffset[i].y - overviewPositionValues.brushVerticesPosMinY + overviewPositionValues.brushVerticesOffsetY;
                }

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


                var verticesOffsetToUse = verticesOffset.Select(x => new PointF(x.x, x.y)).ToArray();

                brushesToDraw.Add(new ObjectToDraw(verticesOffsetToUse, pen, solidBrush));
            }

            return brushesToDraw;
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
                        verticesOffset[i].x = verticesOffset[i].x - overviewPositionValues.brushVerticesPosMinX + overviewPositionValues.brushVerticesOffsetX;
                        verticesOffset[i].y = verticesOffset[i].y - overviewPositionValues.brushVerticesPosMinY + overviewPositionValues.brushVerticesOffsetY;
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


                    var verticesOffsetToUse = verticesOffset.Select(x => new PointF(x.x, x.y)).ToArray();

                    entitiesToDraw.Add(new ObjectToDraw(verticesOffsetToUse, pen, solidBrush));
                }
            }

            return entitiesToDraw;
        }


        private static void DisposeGraphics(Graphics graphics)
        {
            graphics.Dispose();
        }


        private static void DisposeImage(Bitmap bmp)
        {
            bmp.Dispose();
        }


        private static void AddRemoveRegion(Bitmap bmp, Graphics graphics, BrushSide brushSide)
        {
            var verticesToUse = brushSide.vertices.Select(x => new PointF(x.x, x.y)).ToArray();
            if (verticesToUse.Length < 3)
            {
                return;
            }

            var graphicsPath = new GraphicsPath();
            graphicsPath.AddPolygon(verticesToUse);

            // add stroke
            if (jercConfigValues.strokeAroundRemoveMaterials)
            {
                var strokeSolidBrush = new SolidBrush(Color.Transparent);
                var strokePen = new Pen(Color.White, jercConfigValues.strokeWidth);
                DrawFilledPolygonObjectBrushes(graphics, strokeSolidBrush, strokePen, graphicsPath.PathPoints.Select(x => new Point((int)x.X, (int)x.Y)).ToArray());
            }

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


        private static void GenerateTxt(List<LevelHeight> levelHeights)
        {
            var overviewTxt = GetOverviewTxt(overviewPositionValues);

            var lines = overviewTxt.GetInExportableFormat(jercConfigValues, levelHeights, mapName);

            SaveOutputTxtFile(outputTxtFilepath, lines);
        }


        private static OverviewTxt GetOverviewTxt(OverviewPositionValues overviewPositionValues)
        {
            string scale = overviewPositionValues.scale.ToString();
            string pos_x = overviewPositionValues.posX.ToString();
            string pos_y = overviewPositionValues.posY.ToString();
            string rotate = null;
            string zoom = null;

            string inset_left = null, inset_top = null, inset_right = null, inset_bottom = null;

            string CTSpawn_x = null, CTSpawn_y = null, TSpawn_x = null, TSpawn_y = null;

            string bombA_x = null, bombA_y = null, bombB_x = null, bombB_y = null;

            string Hostage1_x = null, Hostage1_y = null, Hostage2_x = null, Hostage2_y = null, Hostage3_x = null, Hostage3_y = null, Hostage4_x = null, Hostage4_y = null;
            string Hostage5_x = null, Hostage5_y = null, Hostage6_x = null, Hostage6_y = null, Hostage7_x = null, Hostage7_y = null, Hostage8_x = null, Hostage8_y = null;


            var paddingPercentageEachSideX = overviewPositionValues.paddingPercentageX == 0 ? 0 : (overviewPositionValues.paddingPercentageX / 2);
            var paddingPercentageEachSideY = overviewPositionValues.paddingPercentageY == 0 ? 0 : (overviewPositionValues.paddingPercentageY / 2);

            if (vmfRequiredData.ctSpawnEntities.Any())
            {
                var origins = vmfRequiredData.ctSpawnEntities.Select(x => new Vertices(x.origin));
                var xPercent = Math.Abs((Math.Abs(Math.Abs(origins.Average(x => x.x)) - Math.Abs(overviewPositionValues.brushVerticesPosMinX))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                var yPercent = Math.Abs((Math.Abs(Math.Abs(origins.Average(x => x.y)) - Math.Abs(overviewPositionValues.brushVerticesPosMinY))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                CTSpawn_x = FlipOverviewTxtValues(xPercent, true);
                CTSpawn_y = FlipOverviewTxtValues(yPercent, false);
            }
            
            if (vmfRequiredData.tSpawnEntities.Any())
            {
                var origins = vmfRequiredData.tSpawnEntities.Select(x => new Vertices(x.origin));
                var xPercent = Math.Abs((Math.Abs(Math.Abs(origins.Average(x => x.x)) - Math.Abs(overviewPositionValues.brushVerticesPosMinX))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                var yPercent = Math.Abs((Math.Abs(Math.Abs(origins.Average(x => x.y)) - Math.Abs(overviewPositionValues.brushVerticesPosMinY))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                TSpawn_x = FlipOverviewTxtValues(xPercent, true);
                TSpawn_y = FlipOverviewTxtValues(yPercent, false);
            }

            if (vmfRequiredData.bombsiteBrushEntities.Any())
            {
                var bombsites = vmfRequiredData.bombsiteBrushEntities;

                if (vmfRequiredData.bombsiteBrushEntities.LastOrDefault().targetname.ToLower().Contains("bombsite_a"))
                {
                    bombsites.Reverse();
                }

                var xAllValues1 = bombsites.FirstOrDefault().brushes.SelectMany(x => x.side.SelectMany(y => y.vertices_plus.Select(x => x.x)));
                var yAllValues1 = bombsites.FirstOrDefault().brushes.SelectMany(x => x.side.SelectMany(y => y.vertices_plus.Select(x => x.y)));
                var xAverage1 = xAllValues1.Average();
                var yAverage1 = yAllValues1.Average();
                //var xPercent1 = Math.Abs((xAverage1 - (overviewPositionValues.brushVerticesPosMinX + overviewPositionValues.paddingSizeX)) / overviewPositionValues.outputResolution);
                var xPercent1 = Math.Abs((Math.Abs(Math.Abs(xAverage1) - Math.Abs(overviewPositionValues.brushVerticesPosMinX))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                var yPercent1 = Math.Abs((Math.Abs(Math.Abs(yAverage1) - Math.Abs(overviewPositionValues.brushVerticesPosMinY))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                bombA_x = FlipOverviewTxtValues(xPercent1, true);
                bombA_y = FlipOverviewTxtValues(yPercent1, false);

                if (vmfRequiredData.bombsiteBrushEntities.Count() > 1)
                {
                    var xAllValues2 = bombsites.Skip(1).FirstOrDefault().brushes.SelectMany(x => x.side.SelectMany(y => y.vertices_plus.Select(x => x.x)));
                    var yAllValues2 = bombsites.Skip(1).FirstOrDefault().brushes.SelectMany(x => x.side.SelectMany(y => y.vertices_plus.Select(x => x.y)));
                    var xAverage2 = xAllValues2.Average();
                    var yAverage2 = yAllValues2.Average();
                    var xPercent2 = Math.Abs((Math.Abs(Math.Abs(xAverage2) - Math.Abs(overviewPositionValues.brushVerticesPosMinX))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                    var yPercent2 = Math.Abs((Math.Abs(Math.Abs(yAverage2) - Math.Abs(overviewPositionValues.brushVerticesPosMinY))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                    bombB_x = FlipOverviewTxtValues(xPercent2, true);
                    bombB_y = FlipOverviewTxtValues(yPercent2, false);
                }
            }

            if (vmfRequiredData.hostageEntities.Any())
            {
                var origin1 = new Vertices(vmfRequiredData.hostageEntities.FirstOrDefault().origin);
                var xPercent1 = Math.Abs((Math.Abs(Math.Abs(origin1.x) - Math.Abs(overviewPositionValues.brushVerticesPosMinX))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                var yPercent1 = Math.Abs((Math.Abs(Math.Abs(origin1.y) - Math.Abs(overviewPositionValues.brushVerticesPosMinY))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                Hostage1_x = FlipOverviewTxtValues(xPercent1, true);
                Hostage1_y = FlipOverviewTxtValues(yPercent1, false);

                if (vmfRequiredData.hostageEntities.Count() > 1)
                {
                    var origin2 = new Vertices(vmfRequiredData.hostageEntities.Skip(1).FirstOrDefault().origin);
                    var xPercent2 = Math.Abs((Math.Abs(Math.Abs(origin2.x) - Math.Abs(overviewPositionValues.brushVerticesPosMinX))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                    var yPercent2 = Math.Abs((Math.Abs(Math.Abs(origin2.y) - Math.Abs(overviewPositionValues.brushVerticesPosMinY))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                    Hostage2_x = FlipOverviewTxtValues(xPercent2, true);
                    Hostage2_y = FlipOverviewTxtValues(yPercent2, false);

                    if (vmfRequiredData.hostageEntities.Count() > 2)
                    {
                        var origin3 = new Vertices(vmfRequiredData.hostageEntities.Skip(2).FirstOrDefault().origin);
                        var xPercent3 = Math.Abs((Math.Abs(Math.Abs(origin3.x) - Math.Abs(overviewPositionValues.brushVerticesPosMinX))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                        var yPercent3 = Math.Abs((Math.Abs(Math.Abs(origin3.y) - Math.Abs(overviewPositionValues.brushVerticesPosMinY))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                        Hostage3_x = FlipOverviewTxtValues(xPercent3, true);
                        Hostage3_y = FlipOverviewTxtValues(yPercent3, false);

                        if (vmfRequiredData.hostageEntities.Count() > 3)
                        {
                            var origin4 = new Vertices(vmfRequiredData.hostageEntities.Skip(3).FirstOrDefault().origin);
                            var xPercent4 = Math.Abs((Math.Abs(Math.Abs(origin4.x) - Math.Abs(overviewPositionValues.brushVerticesPosMinX))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                            var yPercent4 = Math.Abs((Math.Abs(Math.Abs(origin4.y) - Math.Abs(overviewPositionValues.brushVerticesPosMinY))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                            Hostage4_x = FlipOverviewTxtValues(xPercent4, true);
                            Hostage4_y = FlipOverviewTxtValues(yPercent4, false);

                            if (vmfRequiredData.hostageEntities.Count() > 4)
                            {
                                var origin5 = new Vertices(vmfRequiredData.hostageEntities.Skip(3).FirstOrDefault().origin);
                                var xPercent5 = Math.Abs((Math.Abs(Math.Abs(origin5.x) - Math.Abs(overviewPositionValues.brushVerticesPosMinX))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                                var yPercent5 = Math.Abs((Math.Abs(Math.Abs(origin5.y) - Math.Abs(overviewPositionValues.brushVerticesPosMinY))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                                Hostage5_x = FlipOverviewTxtValues(xPercent5, true);
                                Hostage5_y = FlipOverviewTxtValues(yPercent5, false);

                                if (vmfRequiredData.hostageEntities.Count() > 5)
                                {
                                    var origin6 = new Vertices(vmfRequiredData.hostageEntities.Skip(3).FirstOrDefault().origin);
                                    var xPercent6 = Math.Abs((Math.Abs(Math.Abs(origin6.x) - Math.Abs(overviewPositionValues.brushVerticesPosMinX))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                                    var yPercent6 = Math.Abs((Math.Abs(Math.Abs(origin6.y) - Math.Abs(overviewPositionValues.brushVerticesPosMinY))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                                    Hostage6_x = FlipOverviewTxtValues(xPercent6, true);
                                    Hostage6_y = FlipOverviewTxtValues(yPercent6, false);

                                    if (vmfRequiredData.hostageEntities.Count() > 6)
                                    {
                                        var origin7 = new Vertices(vmfRequiredData.hostageEntities.Skip(3).FirstOrDefault().origin);
                                        var xPercent7 = Math.Abs((Math.Abs(Math.Abs(origin7.x) - Math.Abs(overviewPositionValues.brushVerticesPosMinX))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                                        var yPercent7 = Math.Abs((Math.Abs(Math.Abs(origin7.y) - Math.Abs(overviewPositionValues.brushVerticesPosMinY))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                                        Hostage7_x = FlipOverviewTxtValues(xPercent7, true);
                                        Hostage7_y = FlipOverviewTxtValues(yPercent7, false);

                                        if (vmfRequiredData.hostageEntities.Count() > 7)
                                        {
                                            var origin8 = new Vertices(vmfRequiredData.hostageEntities.Skip(3).FirstOrDefault().origin);
                                            var xPercent8 = Math.Abs((Math.Abs(Math.Abs(origin8.x) - Math.Abs(overviewPositionValues.brushVerticesPosMinX))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                                            var yPercent8 = Math.Abs((Math.Abs(Math.Abs(origin8.y) - Math.Abs(overviewPositionValues.brushVerticesPosMinY))) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                                            Hostage8_x = FlipOverviewTxtValues(xPercent8, true);
                                            Hostage8_y = FlipOverviewTxtValues(yPercent8, false);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }


            return new OverviewTxt(
                mapName, pos_x, pos_y, scale, rotate, zoom,
                inset_left, inset_top, inset_right, inset_bottom,
                CTSpawn_x, CTSpawn_y, TSpawn_x, TSpawn_y,
                bombA_x, bombA_y, bombB_x, bombB_y,
                Hostage1_x, Hostage1_y, Hostage2_x, Hostage2_y, Hostage3_x, Hostage3_y, Hostage4_x, Hostage4_y,
                Hostage5_x, Hostage5_y, Hostage6_x, Hostage6_y, Hostage7_x, Hostage7_y, Hostage8_x, Hostage8_y
            );
        }


        private static string FlipOverviewTxtValues(float value, bool isAxisX)
        {
            var newValue = value;

            if (!isAxisX)
            {
                newValue -= 1;
                newValue = -newValue;
            }

            return newValue.ToString();
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
