using ImageAlterer;
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
        private static readonly ImageProcessorExtender imageProcessorExtender = new ImageProcessorExtender();

        private static readonly string gameBinDirectoryPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\"));
        private static readonly string gameCsgoDirectoryPath = Path.GetFullPath(Path.Combine(Path.Combine(gameBinDirectoryPath, @"..\"), @"csgo\"));
        private static readonly string gameOverviewsDirectoryPath = Path.GetFullPath(Path.Combine(gameCsgoDirectoryPath, @"resource\overviews\"));

        private static string outputFilepathPrefix;
        private static string outputImageBackgroundLevelsFilepath;

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

            outputFilepathPrefix = string.Concat(gameOverviewsDirectoryPath, mapName);
            outputImageBackgroundLevelsFilepath = string.Concat(outputFilepathPrefix, "_background_levels.png");

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

            if (jercConfigValues.exportTxt)
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
            var brushesDoor = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.DoorTextureName);
            var brushesLadder = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.LadderTextureName);

            // displacements
            var displacementsRemove = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.RemoveTextureName);
            var displacementsPath = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.PathTextureName);
            var displacementsCover = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.CoverTextureName);
            var displacementsOverlap = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.OverlapTextureName);
            var displacementsDoor = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.DoorTextureName);
            var displacementsLadder = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.LadderTextureName);

            // entities (in game)
            var buyzoneBrushEntities = GetEntitiesByClassname(allEntities, Classnames.ClassnameBuyzone);
            var bombsiteBrushEntities = GetEntitiesByClassname(allEntities, Classnames.ClassnameBombsite);
            var rescueZoneBrushEntities = GetEntitiesByClassname(allEntities, Classnames.ClassnameRescueZone);
            var hostageEntities = GetEntitiesByClassname(allEntities, Classnames.ClassnameHostage);
            var ctSpawnEntities = GetEntitiesByClassname(allEntities, Classnames.ClassnameCTSpawn);
            var tSpawnEntities = GetEntitiesByClassname(allEntities, Classnames.ClassnameTSpawn);

            // entities (JERC)
            var jercConfigEntities = GetEntitiesByClassname(allEntities, Classnames.JercConfig);
            var jercDividerEntities = GetEntitiesByClassname(allEntities, Classnames.JercDivider);
            var jercFloorEntities = GetEntitiesByClassname(allEntities, Classnames.JercFloor);
            var jercCeilingEntities = GetEntitiesByClassname(allEntities, Classnames.JercCeiling);

            var allJercEntities = jercConfigEntities.Concat(jercDividerEntities).Concat(jercFloorEntities).Concat(jercCeilingEntities);

            jercConfigValues = new JercConfigValues(GetSettingsValuesFromJercEntities(allJercEntities), jercDividerEntities.Count());

            return new VmfRequiredData(
                brushesRemove, brushesPath, brushesCover, brushesOverlap, brushesDoor, brushesLadder,
                displacementsRemove, displacementsPath, displacementsCover, displacementsOverlap, displacementsDoor, displacementsLadder,
                buyzoneBrushEntities, bombsiteBrushEntities, rescueZoneBrushEntities, hostageEntities, ctSpawnEntities, tSpawnEntities,
                jercConfigEntities, jercDividerEntities, jercFloorEntities, jercCeilingEntities
            );
        }


        private static Dictionary<string, string> GetSettingsValuesFromJercEntities(IEnumerable<IVNode> jercEntities)
        {
            var jercEntitySettingsValues = new Dictionary<string, string>();

            // jerc_config
            var jercConfig = jercEntities.FirstOrDefault(x => x.Body.Any(y => y.Name == "classname" && y.Value == Classnames.JercConfig)).Body;

            jercEntitySettingsValues.Add("alternateOutputPath", jercConfig.FirstOrDefault(x => x.Name == "alternateOutputPath")?.Value ?? string.Empty);
            jercEntitySettingsValues.Add("onlyOutputToAlternatePath", jercConfig.FirstOrDefault(x => x.Name == "onlyOutputToAlternatePath")?.Value);
            jercEntitySettingsValues.Add("exportRadarAsSeparateLevels", jercConfig.FirstOrDefault(x => x.Name == "exportRadarAsSeparateLevels")?.Value);
            jercEntitySettingsValues.Add("useSeparateGradientEachLevel", jercConfig.FirstOrDefault(x => x.Name == "useSeparateGradientEachLevel")?.Value);
            jercEntitySettingsValues.Add("backgroundFilename", jercConfig.FirstOrDefault(x => x.Name == "backgroundFilename")?.Value ?? string.Empty);
            jercEntitySettingsValues.Add("pathColourHigh", jercConfig.FirstOrDefault(x => x.Name == "pathColourHigh")?.Value);
            jercEntitySettingsValues.Add("pathColourLow", jercConfig.FirstOrDefault(x => x.Name == "pathColourLow")?.Value);
            jercEntitySettingsValues.Add("coverColourHigh", jercConfig.FirstOrDefault(x => x.Name == "coverColourHigh")?.Value);
            jercEntitySettingsValues.Add("coverColourLow", jercConfig.FirstOrDefault(x => x.Name == "coverColourLow")?.Value);
            jercEntitySettingsValues.Add("overlapColourHigh", jercConfig.FirstOrDefault(x => x.Name == "overlapColourHigh")?.Value);
            jercEntitySettingsValues.Add("overlapColourLow", jercConfig.FirstOrDefault(x => x.Name == "overlapColourLow")?.Value);
            jercEntitySettingsValues.Add("doorColour", jercConfig.FirstOrDefault(x => x.Name == "doorColour")?.Value);
            jercEntitySettingsValues.Add("ladderColour", jercConfig.FirstOrDefault(x => x.Name == "ladderColour")?.Value);
            jercEntitySettingsValues.Add("strokeWidth", jercConfig.FirstOrDefault(x => x.Name == "strokeWidth")?.Value);
            jercEntitySettingsValues.Add("strokeColour", jercConfig.FirstOrDefault(x => x.Name == "strokeColour")?.Value);
            jercEntitySettingsValues.Add("strokeAroundLayoutMaterials", jercConfig.FirstOrDefault(x => x.Name == "strokeAroundLayoutMaterials")?.Value);
            jercEntitySettingsValues.Add("strokeAroundRemoveMaterials", jercConfig.FirstOrDefault(x => x.Name == "strokeAroundRemoveMaterials")?.Value);
            jercEntitySettingsValues.Add("strokeAroundEntities", jercConfig.FirstOrDefault(x => x.Name == "strokeAroundEntities")?.Value);
            jercEntitySettingsValues.Add("defaultLevelNum", jercConfig.FirstOrDefault(x => x.Name == "defaultLevelNum")?.Value);
            jercEntitySettingsValues.Add("levelBackgroundEnabled", jercConfig.FirstOrDefault(x => x.Name == "levelBackgroundEnabled")?.Value);
            jercEntitySettingsValues.Add("levelBackgroundDarkenAlpha", jercConfig.FirstOrDefault(x => x.Name == "levelBackgroundDarkenAlpha")?.Value);
            jercEntitySettingsValues.Add("levelBackgroundBlurAmount", jercConfig.FirstOrDefault(x => x.Name == "levelBackgroundBlurAmount")?.Value);
            jercEntitySettingsValues.Add("higherLevelOutputName", jercConfig.FirstOrDefault(x => x.Name == "higherLevelOutputName")?.Value);
            jercEntitySettingsValues.Add("lowerLevelOutputName", jercConfig.FirstOrDefault(x => x.Name == "lowerLevelOutputName")?.Value);
            jercEntitySettingsValues.Add("exportTxt", jercConfig.FirstOrDefault(x => x.Name == "exportTxt")?.Value);
            jercEntitySettingsValues.Add("exportDds", jercConfig.FirstOrDefault(x => x.Name == "exportDds")?.Value);
            jercEntitySettingsValues.Add("exportPng", jercConfig.FirstOrDefault(x => x.Name == "exportPng")?.Value);
            jercEntitySettingsValues.Add("exportBackgroundLevelsImage", jercConfig.FirstOrDefault(x => x.Name == "exportBackgroundLevelsImage")?.Value);


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
                .Concat(vmfRequiredData.brushesSidesDoor)
                .Concat(vmfRequiredData.brushesSidesLadder)
                .Concat(vmfRequiredData.displacementsSidesPath)
                .Concat(vmfRequiredData.displacementsSidesCover)
                .Concat(vmfRequiredData.displacementsSidesOverlap)
                .Concat(vmfRequiredData.displacementsSidesDoor)
                .Concat(vmfRequiredData.displacementsSidesLadder);
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
            /*if (jercDividerEntities.Count() == 0)
                return null;*/

            var numOfOverviewLevels = jercConfigValues.exportRadarAsSeparateLevels ? jercDividerEntities.Count() + 1 : 1;
            for (int i = 0; i < numOfOverviewLevels; i++)
            {
                var overviewLevelName = string.Empty;

                var valueDiff = numOfOverviewLevels == 1 ? 0 : (i - jercConfigValues.defaultLevelNum); // set to 0 if there are no dividers
                if (valueDiff == 0)
                    overviewLevelName = "default";
                else if (valueDiff < 0)
                    overviewLevelName = string.Concat(jercConfigValues.lowerLevelOutputName, Math.Abs(valueDiff));
                else if (valueDiff > 0)
                    overviewLevelName = string.Concat(jercConfigValues.higherLevelOutputName, Math.Abs(valueDiff));

                var zMinForTxt = i == 0 ? -(Sizes.MaxHammerGridSize / 2) : levelHeights.ElementAt(i - 1).zMaxForTxt;
                var zMaxForTxt = i == (numOfOverviewLevels - 1) ? (Sizes.MaxHammerGridSize / 2) : new Vertices(jercDividerEntities.ElementAt(i).origin).z;

                var zMinForRadar = i == 0 ? vmfRequiredData.GetLowestVerticesZ() : levelHeights.ElementAt(i - 1).zMaxForRadar;
                var zMaxForRadar = i == (numOfOverviewLevels - 1) ? vmfRequiredData.GetHighestVerticesZ() : new Vertices(jercDividerEntities.ElementAt(i).origin).z;

                var jercFloorEntitiesInsideLevel = jercConfigValues.exportRadarAsSeparateLevels && jercConfigValues.useSeparateGradientEachLevel
                    ? vmfRequiredData.jercFloorEntities.Where(x => new Vertices(x.origin).z >= zMinForRadar && new Vertices(x.origin).z < zMaxForRadar).ToList()
                    : vmfRequiredData.jercFloorEntities;
                var zMinForGradient = jercFloorEntitiesInsideLevel.Any() ? jercFloorEntitiesInsideLevel.OrderBy(x => new Vertices(x.origin).z).Select(x => new Vertices(x.origin).z).FirstOrDefault() : zMinForRadar; // takes the lowest (first) in the level if there are more than one

                var jercCeilingEntitiesInsideLevel = jercConfigValues.exportRadarAsSeparateLevels && jercConfigValues.useSeparateGradientEachLevel
                    ? vmfRequiredData.jercCeilingEntities.Where(x => new Vertices(x.origin).z >= zMinForRadar && new Vertices(x.origin).z < zMaxForRadar).ToList()
                    : vmfRequiredData.jercCeilingEntities;
                var zMaxForGradient = jercCeilingEntitiesInsideLevel.Any() ? jercCeilingEntitiesInsideLevel.OrderBy(x => new Vertices(x.origin).z).Select(x => new Vertices(x.origin).z).LastOrDefault() : zMaxForRadar; // takes the highest (last) in the level if there are more than one

                levelHeights.Add(new LevelHeight(levelHeights.Count(), overviewLevelName, zMinForTxt, zMaxForTxt, zMinForRadar, zMaxForRadar, zMinForGradient, zMaxForGradient));
            }

            return levelHeights;
        }


        private static void GenerateRadars(List<LevelHeight> levelHeights)
        {
            var radarLevels = new List<RadarLevel>();

            // get overview for each separate level if levelBackgroundEnabled == true
            if (levelHeights.Count() > 1)
            {
                if (jercConfigValues.exportRadarAsSeparateLevels)
                {
                    foreach (var levelHeight in levelHeights)
                    {
                        var radarLevel = GenerateRadarLevel(levelHeight);
                        radarLevels.Add(radarLevel);
                    }
                }
                else // more than one level height, but user has specified exporting as a single radar anyway
                {
                    var levelHeight = new LevelHeight(0, "default", levelHeights.Min(x => x.zMinForTxt), levelHeights.Max(x => x.zMaxForTxt), levelHeights.Min(x => x.zMinForRadar), levelHeights.Max(x => x.zMaxForRadar), levelHeights.Min(x => x.zMinForRadarGradient), levelHeights.Max(x => x.zMaxForRadarGradient));
                    var radarLevel = GenerateRadarLevel(levelHeight);
                    radarLevels.Add(radarLevel);
                }
            }
            else
            {
                var currentLevelHeight = levelHeights.FirstOrDefault();

                var levelHeight = new LevelHeight(0, "default", currentLevelHeight.zMinForTxt, currentLevelHeight.zMaxForTxt, currentLevelHeight.zMinForRadar, currentLevelHeight.zMaxForRadar, currentLevelHeight.zMinForRadarGradient, currentLevelHeight.zMaxForRadarGradient);
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

                    // apply blurred background levels to new image
                    newGraphics.CompositingMode = CompositingMode.SourceCopy;
                    newGraphics.DrawImage(backgroundBmp, 0, 0);
                    newGraphics.Save();
                    newGraphics.CompositingMode = CompositingMode.SourceOver;
                    newGraphics.DrawImage(radarLevel.bmp, 0, 0);
                    newGraphics.Save();

                    radarLevelsToSaveList.Add(new RadarLevel(newBmp, radarLevel.levelHeight));

                    // dispose
                    DisposeGraphics(newGraphics);
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
                levelHeight.zMinForRadar, levelHeight.zMaxForRadar,
                levelHeight.zMinForRadarGradient, levelHeight.zMaxForRadarGradient
            );

            //graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.SmoothingMode = SmoothingMode.HighSpeed;

            graphics.SetClip(Rectangle.FromLTRB(0, 0, overviewPositionValues.outputResolution, overviewPositionValues.outputResolution));

            // get all brush sides and displacement sides to draw (brush volumes)
            var brushRemoveList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.brushesRemove, JercTypes.Remove);
            var displacementRemoveList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.displacementsRemove, JercTypes.Remove);

            var brushCoverList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.brushesCover, JercTypes.Cover);
            var displacementCoverList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.displacementsCover, JercTypes.Cover);

            var brushDoorList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.brushesDoor, JercTypes.Door);
            var displacementDoorList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.displacementsDoor, JercTypes.Door);

            var brushLadderList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.brushesLadder, JercTypes.Ladder);
            var displacementLadderList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.displacementsLadder, JercTypes.Ladder);

            // get all brush sides and displacement sides to draw (brush sides)
            var brushPathSideList = GetBrushSideListWithinLevelHeight(levelHeight, vmfRequiredData.brushesSidesPath, JercTypes.Path);
            var displacementPathSideList = GetBrushSideListWithinLevelHeight(levelHeight, vmfRequiredData.displacementsSidesPath, JercTypes.Path);

            var brushOverlapSideList = GetBrushSideListWithinLevelHeight(levelHeight, vmfRequiredData.brushesSidesOverlap, JercTypes.Overlap);
            var displacementOverlapSideList = GetBrushSideListWithinLevelHeight(levelHeight, vmfRequiredData.displacementsSidesOverlap, JercTypes.Overlap);


            var brushesToDrawPath = GetBrushesToDraw(boundingBox, brushPathSideList);
            var displacementsToDrawPath = GetBrushesToDraw(boundingBox, displacementPathSideList);

            var brushesToDrawOverlap = GetBrushesToDraw(boundingBox, brushOverlapSideList);
            var displacementsToDrawOverlap = GetBrushesToDraw(boundingBox, displacementOverlapSideList);

            var brushesToDrawCover = GetBrushesToDraw(boundingBox, brushCoverList.SelectMany(x => x.brushSides).ToList());
            var displacementsToDrawCover = GetBrushesToDraw(boundingBox, displacementCoverList.SelectMany(x => x.brushSides).ToList());

            var brushesToDrawDoor = GetBrushesToDraw(boundingBox, brushDoorList.SelectMany(x => x.brushSides).ToList());
            var displacementsToDrawDoor = GetBrushesToDraw(boundingBox, displacementDoorList.SelectMany(x => x.brushSides).ToList());

            var brushesToDrawLadder = GetBrushesToDraw(boundingBox, brushLadderList.SelectMany(x => x.brushSides).ToList());
            var displacementsToDrawLadder = GetBrushesToDraw(boundingBox, displacementLadderList.SelectMany(x => x.brushSides).ToList());

            // get all entity sides to draw
            var entityBrushSideListById = GetEntityBrushSideListWithinLevelHeight(levelHeight);


            // add remove stuff first to set to graphics' clip
            AddRemoveRegion(bmp, graphics, brushRemoveList);
            AddRemoveRegion(bmp, graphics, displacementRemoveList);

            // path and overlap brush stuff (for stroke)
            if (jercConfigValues.strokeAroundLayoutMaterials)
            {
                foreach (var brushToRender in brushesToDrawPath.Concat(displacementsToDrawPath).Concat(brushesToDrawOverlap).Concat(displacementsToDrawOverlap).OrderBy(x => x.zAxisAverage))
                {
                    DrawStroke(graphics, brushToRender, Colours.ColourBrushesStroke(jercConfigValues.strokeColour));
                }
            }

            // path brush stuff
            foreach (var brushToRender in brushesToDrawPath.Concat(displacementsToDrawPath).OrderBy(x => x.zAxisAverage))
            {
                DrawFilledPolygonGradient(graphics, brushToRender, true);
            }

            // cover and overlap brush stuff
            foreach (var brushToRender in brushesToDrawOverlap.Concat(displacementsToDrawOverlap).Concat(brushesToDrawCover).Concat(displacementsToDrawCover).OrderBy(x => x.zAxisAverage))
            {
                DrawFilledPolygonGradient(graphics, brushToRender, false);
            }

            // door stuff
            foreach (var brushToRender in brushesToDrawDoor.Concat(displacementsToDrawDoor).OrderBy(x => x.zAxisAverage))
            {
                DrawFilledPolygonGradient(graphics, brushToRender, false);
            }

            // ladder stuff
            foreach (var brushToRender in brushesToDrawLadder.Concat(displacementsToDrawLadder).OrderBy(x => x.zAxisAverage))
            {
                DrawFilledPolygonGradient(graphics, brushToRender, false);
            }


            // reset the clip so that entity brushes can render anywhere
            ////graphics.ResetClip();


            // entities next
            var entitiesToDraw = GetEntitiesToDraw(overviewPositionValues, entityBrushSideListById);

            // stroke
            if (jercConfigValues.strokeAroundEntities)
            {
                foreach (var entityToRender in entitiesToDraw)
                {
                    Color colour = Color.White;

                    switch (entityToRender.entityType)
                    {
                        case EntityTypes.Buyzone:
                            colour = Colours.ColourBuyzonesStroke();
                            break;
                        case EntityTypes.Bombsite:
                            colour = Colours.ColourBombsitesStroke();
                            break;
                        case EntityTypes.RescueZone:
                            colour = Colours.ColourRescueZonesStroke();
                            break;
                    }

                    DrawStroke(graphics, entityToRender, colour);
                }
            }

            // normal
            foreach (var entityToRender in entitiesToDraw)
            {
                DrawFilledPolygonGradient(graphics, entityToRender, true);
            }

            graphics.Save();

            return new RadarLevel(bmp, levelHeight);
        }


        private static void DrawStroke(Graphics graphics, ObjectToDraw objectToDraw, Color colourStroke)
        {
            var strokeSolidBrush = new SolidBrush(Color.Transparent);
            var strokePen = new Pen(colourStroke);
            strokePen.Width *= jercConfigValues.strokeWidth;

            DrawFilledPolygonObjectBrushes(graphics, strokeSolidBrush, strokePen, objectToDraw.verticesToDraw.Select(x => x.vertices).ToArray());

            // dispose
            strokeSolidBrush?.Dispose();
            strokePen?.Dispose();
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

            if (!string.IsNullOrWhiteSpace(jercConfigValues.backgroundFilename))
                radarLevel.bmp = AddBackgroundImage(radarLevel);

            var radarLevelString = radarLevel.levelHeight.levelName.ToLower() == "default" ? string.Empty : string.Concat("_", radarLevel.levelHeight.levelName.ToLower());

            if (jercConfigValues.exportDds || jercConfigValues.exportPng)
            {
                if (!jercConfigValues.onlyOutputToAlternatePath)
                {
                    var outputImageFilepath = string.Concat(outputFilepathPrefix, radarLevelString, "_radar");
                    SaveImage(outputImageFilepath, radarLevel.bmp);
                }

                if (!string.IsNullOrWhiteSpace(jercConfigValues.alternateOutputPath) && Directory.Exists(jercConfigValues.alternateOutputPath))
                {
                    var outputImageFilepath = string.Concat(jercConfigValues.alternateOutputPath, mapName, radarLevelString, "_radar");
                    SaveImage(outputImageFilepath, radarLevel.bmp);
                }
            }
        }


        private static Bitmap AddBackgroundImage(RadarLevel radarLevel)
        {
            //var backgroundImageFilepath = string.Concat(gameCsgoDirectoryPath, "materials/jerc/backgrounds/", jercConfigValues.backgroundFilename, ".tga"); // TODO: uncomment before release!!!
            var backgroundImageFilepath = string.Concat("F:/Coding Stuff/GitHub Files/JERC/JERC/Resources/materials/jerc/backgrounds/", jercConfigValues.backgroundFilename, ".bmp"); // TODO: remove before release!!!

            if (!File.Exists(backgroundImageFilepath))
                return radarLevel.bmp;

            Bitmap newBmp = new Bitmap(radarLevel.bmp);
            Graphics newGraphics = Graphics.FromImage(newBmp);

            Bitmap backgroundBmp = new Bitmap(backgroundImageFilepath);
            backgroundBmp = new Bitmap(backgroundBmp, Sizes.FinalOutputImageResolution, Sizes.FinalOutputImageResolution);
            Graphics backgroundGraphics = Graphics.FromImage(backgroundBmp);

            newGraphics.CompositingMode = CompositingMode.SourceCopy;
            newGraphics.DrawImage(backgroundBmp, 0, 0);
            newGraphics.Save();
            newGraphics.CompositingMode = CompositingMode.SourceOver;
            newGraphics.DrawImage(radarLevel.bmp, 0, 0);
            newGraphics.Save();

            // dispose
            DisposeGraphics(backgroundGraphics);
            DisposeImage(backgroundBmp);

            DisposeGraphics(radarLevel.graphics);
            DisposeImage(radarLevel.bmp);

            return newBmp;
        }


        private static Dictionary<int, List<EntityBrushSide>> GetEntityVerticesListById()
        {
            var entityBrushSideListById = new Dictionary<int, List<EntityBrushSide>>();

            var entityBuyzoneVerticesListById = GetEntityBrushSideList(vmfRequiredData.entitiesSidesByEntityBuyzoneId, EntityTypes.Buyzone);
            var entityBombsiteVerticesListById = GetEntityBrushSideList(vmfRequiredData.entitiesSidesByEntityBombsiteId, EntityTypes.Bombsite);
            var entityRescueZoneVerticesListById = GetEntityBrushSideList(vmfRequiredData.entitiesSidesByEntityRescueZoneId, EntityTypes.RescueZone);

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


        private static List<BrushVolume> GetBrushVolumeListWithinLevelHeight(LevelHeight levelHeight, List<Models.Brush> brushList, JercTypes jercType)
        {
            return GetBrushVolumeList(brushList, jercType).Where(x => !(
                (x.brushSides.SelectMany(y => y.vertices).All(y => y.z < levelHeight.zMinForRadar) && x.brushSides.SelectMany(y => y.vertices).All(y => y.z < levelHeight.zMaxForRadar)) ||
                (x.brushSides.SelectMany(y => y.vertices).All(y => y.z >= levelHeight.zMinForRadar) && x.brushSides.SelectMany(y => y.vertices).All(y => y.z >= levelHeight.zMaxForRadar))
            )).ToList();
        }


        private static List<BrushSide> GetBrushSideListWithinLevelHeight(LevelHeight levelHeight, List<Side> sideList, JercTypes jercType)
        {
            return GetBrushSideList(sideList, jercType).Where(x => !(
                (x.vertices.All(y => y.z < levelHeight.zMinForRadar) && x.vertices.All(y => y.z < levelHeight.zMaxForRadar)) ||
                (x.vertices.All(y => y.z >= levelHeight.zMinForRadar) && x.vertices.All(y => y.z >= levelHeight.zMaxForRadar))
            )).ToList();
        }


        private static Dictionary<int, List<EntityBrushSide>> GetEntityBrushSideListWithinLevelHeight(LevelHeight levelHeight)
        {
            var entityBrushSideListById = new Dictionary<int, List<EntityBrushSide>>();
            var entityBrushSideListByIdUnfiltered = GetEntityVerticesListById();

            foreach (var entityBrushSideById in entityBrushSideListByIdUnfiltered)
            {
                if (entityBrushSideById.Value.Any(x => !((x.vertices.All(y => y.z < levelHeight.zMinForRadar) && x.vertices.All(y => y.z < levelHeight.zMaxForRadar)) || (x.vertices.All(y => y.z >= levelHeight.zMinForRadar) && x.vertices.All(y => y.z >= levelHeight.zMaxForRadar))))) // would this allow entities to be on more than 1 level if their brushes span across level dividers ??
                {
                    entityBrushSideListById.Add(entityBrushSideById.Key, entityBrushSideById.Value);
                }
            }

            return entityBrushSideListById;
        }


        private static List<BrushVolume> GetBrushVolumeList(List<Models.Brush> brushList, JercTypes jercType)
        {
            var brushVolumeList = new List<BrushVolume>();
            
            foreach (var brush in brushList)
            {
                var brushNew = new BrushVolume();
                foreach (var brushSide in brush.side.ToList())
                {
                    var brushSideNew = new BrushSide();
                    foreach (var vertices in brushSide.vertices_plus.ToList())
                    {
                        brushSideNew.vertices.Add(new Vertices(vertices.x / Sizes.SizeReductionMultiplier, vertices.y / Sizes.SizeReductionMultiplier, vertices.z));
                        brushSideNew.jercType = jercType;
                    }

                    brushNew.brushSides.Add(brushSideNew);
                }

                brushNew.jercType = jercType;

                brushVolumeList.Add(brushNew);
            }

            return brushVolumeList;
        }


        private static List<BrushSide> GetBrushSideList(List<Side> sideList, JercTypes jercType)
        {
            var brushSideList = new List<BrushSide>();

            foreach (var side in sideList)
            {
                var brushSide = new BrushSide();
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    brushSide.vertices.Add(new Vertices(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier, vert.z));
                    brushSide.jercType = jercType;
                }

                brushSideList.Add(brushSide);
            }

            return brushSideList;
        }


        private static Dictionary<int, List<EntityBrushSide>> GetEntityBrushSideList(Dictionary<int, List<Side>> sideListByEntityIdDictionary, EntityTypes entityType)
        {
            var entityBrushSideListById = new Dictionary<int, List<EntityBrushSide>>();

            foreach (var entitySides in sideListByEntityIdDictionary)
            {
                foreach (var side in entitySides.Value)
                {
                    var entityBrushSide = new EntityBrushSide();
                    for (int i = 0; i < side.vertices_plus.Count(); i++)
                    {
                        var vert = side.vertices_plus[i];

                        entityBrushSide.vertices.Add(new Vertices(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier, vert.z));
                        entityBrushSide.entityType = entityType;
                    }

                    if (entityBrushSideListById.ContainsKey(entitySides.Key))
                        entityBrushSideListById[entitySides.Key].Add(entityBrushSide);
                    else
                        entityBrushSideListById.Add(entitySides.Key, new List<EntityBrushSide>() { entityBrushSide });
                }
            }

            return entityBrushSideListById;
        }


        private static Vertices GetCorrectedVerticesPositionInWorld(Vertices vertices)
        {
            vertices.x = (vertices.x - overviewPositionValues.brushVerticesPosMinX + overviewPositionValues.brushVerticesOffsetX) / Sizes.SizeReductionMultiplier;
            vertices.y = (vertices.y - overviewPositionValues.brushVerticesPosMinY + overviewPositionValues.brushVerticesOffsetY) / Sizes.SizeReductionMultiplier;

            return vertices;
        }


        private static void AddRemoveRegion(Bitmap bmp, Graphics graphics, List<BrushVolume> brushList)
        {
            foreach (var brush in brushList)
            {
                if (brush.jercType != JercTypes.Remove)
                    continue;

                // corrects the verts by taking into account the movement from space in world to the space in the image (which starts at (0,0))
                foreach (var side in brush.brushSides)
                {
                    var verticesOffset = side.vertices;
                    for (var i = 0; i < verticesOffset.Count(); i++)
                    {
                        verticesOffset[i] = GetCorrectedVerticesPositionInWorld(verticesOffset[i]);
                    }

                    AddRemoveRegion(bmp, graphics, side);
                }
            }
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
                var strokePen = new Pen(Colours.ColourRemoveStroke(jercConfigValues.strokeColour), jercConfigValues.strokeWidth);
                DrawFilledPolygonObjectBrushes(graphics, strokeSolidBrush, strokePen, graphicsPath.PathPoints.Select(x => new Point((int)x.X, (int)x.Y)).ToArray());
            }

            var region = new Region(graphicsPath);
            graphics.ExcludeClip(region);

            graphicsPath.CloseFigure();
        }


        private static List<ObjectToDraw> GetBrushesToDraw(BoundingBox boundingBox, List<BrushSide> brushSidesList)
        {
            var brushesToDraw = new List<ObjectToDraw>();

            foreach (var brushSide in brushSidesList)
            {
                var verticesOffsetsToUse = new List<VerticesToDraw>();

                foreach (var vertices in brushSide.vertices)
                {
                    var heightAboveMin = vertices.z - boundingBox.minZGradient;

                    float percentageAboveMin = 0;
                    if (heightAboveMin == 0)
                    {
                        if (boundingBox.minZGradient == boundingBox.maxZGradient)
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
                        percentageAboveMin = (float)((Math.Ceiling(Convert.ToDouble(heightAboveMin)) / (boundingBox.maxZGradient - boundingBox.minZGradient)));
                    }

                    // corrects the verts by taking into account the movement from space in world to the space in the image (which starts at (0,0))
                    var verticesOffset = GetCorrectedVerticesPositionInWorld(vertices);

                    Color colour = brushSide.jercType switch
                    {
                        //JercTypes.Remove => Colours.ColourRemove(percentageAboveMin),
                        JercTypes.Path => Colours.ColourBrush(jercConfigValues.pathColourLow, jercConfigValues.pathColourHigh, percentageAboveMin),
                        JercTypes.Cover => Colours.ColourBrush(jercConfigValues.coverColourLow, jercConfigValues.coverColourHigh, percentageAboveMin),
                        JercTypes.Overlap => Colours.ColourBrush(jercConfigValues.overlapColourLow, jercConfigValues.overlapColourHigh, percentageAboveMin),
                        JercTypes.Door => jercConfigValues.doorColour,
                        JercTypes.Ladder => jercConfigValues.ladderColour
                    };

                    verticesOffsetsToUse = verticesOffsetsToUse.Distinct().ToList(); // TODO: doesn't seem to work

                    verticesOffsetsToUse.Add(new VerticesToDraw(new Point((int)verticesOffset.x, (int)verticesOffset.y), (int)verticesOffset.z, colour));
                }

                brushesToDraw.Add(new ObjectToDraw(verticesOffsetsToUse, (int)verticesOffsetsToUse.Select(x => x.zAxis).Average(x => x), brushSide.jercType));
            }

            return brushesToDraw;
        }


        private static List<ObjectToDraw> GetEntitiesToDraw(OverviewPositionValues overviewPositionValues, Dictionary<int, List<EntityBrushSide>> entityBrushSideListById)
        {
            var entitiesToDraw = new List<ObjectToDraw>();

            foreach (var entityBrushSideByBrush in entityBrushSideListById.Values)
            {
                foreach (var entityBrushSide in entityBrushSideByBrush)
                {
                    var verticesOffsetsToUse = new List<VerticesToDraw>();

                    foreach (var vertices in entityBrushSide.vertices)
                    {
                        // corrects the verts by taking into account the movement from space in world to the space in the image (which starts at (0,0))
                        var verticesOffset = GetCorrectedVerticesPositionInWorld(vertices);

                        Color colour = entityBrushSide.entityType switch
                        {
                            EntityTypes.Buyzone => Colours.ColourBuyzones(),
                            EntityTypes.Bombsite => Colours.ColourBombsites(),
                            EntityTypes.RescueZone => Colours.ColourRescueZones(),
                        };

                        verticesOffsetsToUse.Add(new VerticesToDraw(new Point((int)verticesOffset.x, (int)verticesOffset.y), (int)verticesOffset.z, colour));
                    }

                    verticesOffsetsToUse = verticesOffsetsToUse.Distinct().ToList(); // TODO: doesn't seem to work

                    entitiesToDraw.Add(new ObjectToDraw(verticesOffsetsToUse, (int)verticesOffsetsToUse.Select(x => x.zAxis).Average(x => x), entityBrushSide.entityType));
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


        private static void DrawFilledPolygonGradient(Graphics graphics, ObjectToDraw objectToDraw, bool drawAroundEdge)
        {
            // Make the points for a polygon.
            var vertices = objectToDraw.verticesToDraw.Select(x => x.vertices).ToList();

            // remove duplicate point positions (this can be caused by vertical brush sides, where their X and Y values are the same (Z is not taken into account here))
            vertices = vertices.Distinct().ToList();

            // check there are still more than 2 points
            if (vertices.Count() < 3)
                return;

            // check there are more than 1 value on each axis
            if (vertices.Select(x => x.X).Distinct().Count() < 2 || vertices.Select(x => x.Y).Distinct().Count() < 2)
                return;

            // draw polygon
            var verticesArray = vertices.ToArray();

            using (PathGradientBrush pathBrush = new PathGradientBrush(verticesArray))
            {
                var colours = new List<Color>();
                for (int i = 0; i < verticesArray.Length; i++)
                {
                    colours.Add(objectToDraw.verticesToDraw[i].colour);
                }

                // get average colour for center
                int averageColourA = 0, averageColourR = 0, averageColourG = 0, averageColourB = 0;
                foreach (var colour in colours)
                {
                    averageColourA += colour.A;
                    averageColourR += colour.R;
                    averageColourG += colour.G;
                    averageColourB += colour.B;
                }

                averageColourA /= colours.Count();
                averageColourR /= colours.Count();
                averageColourG /= colours.Count();
                averageColourB /= colours.Count();

                var averageColour = Color.FromArgb(averageColourA, averageColourR, averageColourG, averageColourB);

                // Define the center and surround colors.
                pathBrush.CenterColor = averageColour;
                pathBrush.SurroundColors = colours.ToArray();

                // Fill the hexagon
                graphics.FillPolygon(pathBrush, verticesArray);

                // Draw border of the hexagon
                if (drawAroundEdge)
                {
                    var verticesToDrawList = objectToDraw.verticesToDraw;
                    for (int i = 0; i < verticesToDrawList.Count(); i++)
                    {
                        var verticesToDraw1 = verticesToDrawList[i];
                        var verticesToDraw2 = (i == verticesToDrawList.Count() - 1) ? verticesToDrawList[0] : verticesToDrawList[i + 1];

                        if (verticesToDraw1.vertices == verticesToDraw2.vertices)
                            continue;

                        using (LinearGradientBrush linearBrush = new LinearGradientBrush(verticesToDraw1.vertices, verticesToDraw2.vertices, verticesToDraw1.colour, verticesToDraw2.colour))
                        {
                            Pen pen = new Pen(linearBrush);

                            graphics.DrawLine(pen, verticesToDraw1.vertices, verticesToDraw2.vertices);

                            pen?.Dispose();
                        }
                    }
                }
            }
        }


        private static void DrawFilledPolygonObjectBrushes(Graphics graphics, SolidBrush solidBrush, Pen pen, Point[] vertices)
        {
            graphics.DrawPolygon(pen, vertices);
            graphics.FillPolygon(solidBrush, vertices);

            pen?.Dispose();
            solidBrush?.Dispose();
        }


        /*private static void DrawFilledPolygonObjectDisplacements(Graphics graphics, SolidBrush solidBrush, Pen pen, Point[] vertices)
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
        }*/


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
                if (jercConfigValues.exportDds)
                    bmp.Save(filepath + ".dds");

                if (jercConfigValues.exportPng)
                    bmp.Save(filepath + ".png", ImageFormat.Png);
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

            if (!jercConfigValues.onlyOutputToAlternatePath)
            {
                var outputTxtFilepath = string.Concat(outputFilepathPrefix, ".txt");
                SaveOutputTxtFile(outputTxtFilepath, lines);
            }

            if (!string.IsNullOrWhiteSpace(jercConfigValues.alternateOutputPath) && Directory.Exists(jercConfigValues.alternateOutputPath))
            {
                var outputTxtFilepath = string.Concat(jercConfigValues.alternateOutputPath, mapName, ".txt");
                SaveOutputTxtFile(outputTxtFilepath, lines);
            }
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
            if (!filepath.Contains(".txt"))
                filepath += ".txt";

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
