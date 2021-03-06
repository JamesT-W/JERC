using ImageAlterer;
using JERC.Constants;
using JERC.Enums;
using JERC.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using VMFParser;

namespace JERC
{
    class Program
    {
        private static readonly bool debugging = false;
        private static readonly string debuggingJercPath = @"F:\Coding Stuff\GitHub Files\JERC\";

        private static readonly ImageProcessorExtender imageProcessorExtender = new ImageProcessorExtender();

        private static string backgroundImagesDirectory;
        private static string overviewsOutputFilepathPrefix;
        private static string dzTabletOutputFilepathPrefix;
        private static string dzTabletWorkshopOutputFilepathPrefix;
        private static string dzSpawnselectOutputFilepathPrefix;
        private static string dzSpawnselectWorkshopOutputFilepathPrefix;
        private static string outputImageBackgroundLevelsFilepath;

        private static string extrasOutputFilepathPrefix;
        private static string extrasOutputFilepathPrefixOverview;
        private static string extrasOutputFilepathPrefixTablet;
        private static string extrasOutputFilepathPrefixSpawnSelect;

        private static string alternateExtrasOutputFilepathPrefix;
        private static string alternateExtrasOutputFilepathPrefixOverview;
        private static string alternateExtrasOutputFilepathPrefixTablet;
        private static string alternateExtrasOutputFilepathPrefixSpawnSelect;

        private static string vmfcmdFilepath;

        private static ConfigurationValues configurationValues;

        private static VisgroupIdsInVmf visgroupIdsInMainVmf;
        private static readonly Dictionary<int, VisgroupIdsInVmf> visgroupIdsInInstanceEntityIds = new Dictionary<int, VisgroupIdsInVmf>();

        private static string mapName;

        private static VMF vmf;
        private static readonly Dictionary<VMF, int> instanceEntityIdsByVmf = new Dictionary<VMF, int>();
        private static VmfRequiredData vmfRequiredData;
        private static OverviewPositionValues overviewPositionValues;


        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Logger.LogError("No argument and value given");
                return;
            }

            // add quotes to param values if necessary
            for (int i = 1; i < args.Length; i++)
            {
                if (!args[i].StartsWith("\"") && GameConfigurationValues.allArgumentNames.Any(x => x == args[i - 1].ToLower()))
                {
                    args[i] = "\"" + args[i];
                }

                if (!args[i].EndsWith("\"") &&
                        (i == args.Length - 1 ||
                        i < args.Length - 1 && GameConfigurationValues.allArgumentNames.Any(x => x == args[i + 1].ToLower())))
                {
                    args[i] += "\"";
                }
            }

            // ensure the args are split by spaces (but not when spaces are within quotes)
            var argsJoined = string.Join(" ", args);
            args = argsJoined.Split('"')
                     .Select((element, index) => index % 2 == 0  // If even index
                                           ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)  // Split the item
                                           : new string[] { element })  // Keep the entire item
                     .SelectMany(element => element).ToArray();
            //


            GameConfigurationValues.SetArgs(args);


            if (!debugging && !GameConfigurationValues.VerifyAllValuesSet())
            {
                Logger.LogError("Game configuration filepaths missing. Potentially too many parameters given. Check the compile configuration's parameters.");
                return;
            }

            if (!debugging && (GameConfigurationValues.binFolderPath.Split(@"\").Reverse().Skip(1).FirstOrDefault() != "bin" || GameConfigurationValues.binFolderPath.Replace("/", @"\").Replace(@"\\", @"\").Contains(@"\csgo\bin")))
            {
                Logger.LogError(@"JERC's folder should be placed in ...\Counter-Strike Global Offensive\bin");
                return;
            }

            try
            {
                var lines = File.ReadAllLines(GameConfigurationValues.vmfFilepath);
                vmf = new VMF(lines);
            }
            catch (Exception e)
            {
                Logger.LogError("Could not find or read main vmf, check the filepath is correct and if the file is potentially locked due to saving, aborting");
            }

            if (vmf == null)
            {
                Logger.LogError("Error parsing VMF, aborting");
                return;
            }

            Logger.LogNewLine();
            Logger.LogMessage("VMF parsed sucessfully");
            Logger.LogNewLine();


            // main vmf contents
            var allWorldBrushes = vmf.World.Body.Where(x => x.Name == "solid");
            var allEntities = vmf.Body.Where(x => x.Name == "entity");

            // if software was not defined, work out which is being used automatically
            if (GameConfigurationValues.isVanillaHammer == null)
            {
                // check world brushes and entity brushes for "vertices_plus" to know if it has been saved with hammer++ or vanilla hammer
                if (allWorldBrushes.Any(x => x.Body.Where(y => y.Name == "side").Any(y => y.Body.Any(z => z.Name == "vertices_plus"))) ||
                    allEntities.Any(x => x.Body.Where(y => y.Name == "solid").Where(y => y.Body != null).Any(y => y.Body.Where(z => z.Name == "side").Any(z => z.Body.Any(a => a.Name == "vertices_plus"))))
                )
                {
                    GameConfigurationValues.isVanillaHammer = false;
                    Logger.LogMessage("VMF saved with Hammer++ (auto)");
                }
                else
                {
                    GameConfigurationValues.isVanillaHammer = true;
                    Logger.LogMessage("VMF saved with Vanilla Hammer (auto)");
                }

                Logger.LogNewLine();
            }

            var jercConfigEntities = GetEntitiesByClassname(allEntities, Classnames.JercConfig);

            if (jercConfigEntities == null || !jercConfigEntities.Any())
            {
                Logger.LogError("No jerc_config entity found in main vmf, aborting");
                return;
            }

            if (jercConfigEntities.Count() > 1)
            {
                Logger.LogError("More than one jerc_config entity found, aborting");
                return;
            }

            configurationValues = new ConfigurationValues(GetSettingsValuesFromJercConfig(jercConfigEntities.First()));

            // add hidden stuff if applicable except when instance is hidden
            if (configurationValues.includeEvenWhenHidden)
            {
                Logger.LogMessage("Looking through hidden brushes and entities...");
                AddHiddenStuffToDifferentLocationInVmf(vmf);
                Logger.LogMessage("Finished looking through hidden brushes and entities");

                allWorldBrushes = vmf.World.Body.Where(x => x.Name == "solid");
                allEntities = vmf.Body.Where(x => x.Name == "entity");
            }


            // correct entity origins and angles
            Logger.LogMessage("Correcting overlay origins and angles...");
            CorrectOverlayOriginsAndAngles(vmf.Body.Where(x => x.Name == "entity"));
            Logger.LogMessage("Finished correcting overlay origins and angles");


            Logger.LogMessage("Parsing instances...");
            var successfullyParsedInstances = SortInstances(vmf);
            if (!successfullyParsedInstances)
            {
                return;
            }
            Logger.LogMessage("Finished parsing instances");

            SetVisgroupIdsInMainVmf();
            SetVisgroupIdsInInstancesByEntityId();

            vmfRequiredData = GetVmfRequiredData(allWorldBrushes, allEntities, jercConfigEntities);

            if (vmfRequiredData == null)
            {
                Logger.LogError("More than one jerc_disp_rotation entity found in main vmf, aborting");
                return;
            }


            if (!configurationValues.onlyOutputToAlternatePath)
            {
                GameConfigurationValues.CreateAnyGameDirectoriesThatDontExist();
            }

            if (!debugging && !configurationValues.onlyOutputToAlternatePath && !GameConfigurationValues.VerifyAllDirectoriesAndFilesExist())
            {
                Logger.LogError("Game configuration directories missing. These directories are created automatically, make sure nothing is being locked.");
                return;
            }


            if (configurationValues.onlyOutputToAlternatePath && string.IsNullOrWhiteSpace(configurationValues.alternateOutputPath))
            {
                Logger.LogError("Set to only output to alternate path, however no alternate path is provided.");
                return;
            }

            mapName = Path.GetFileNameWithoutExtension(GameConfigurationValues.vmfFilepath);
            if (debugging)
            {
                Logger.LogDebugInfo("Setting backgroundImagesDirectory to empty string");
                Logger.LogDebugInfo("Setting overviewsOutputFilepathPrefix to empty string");
                Logger.LogDebugInfo("Setting dzTabletOutputFilepathPrefix to empty string");
                Logger.LogDebugInfo("Setting dzTabletWorkshopOutputFilepathPrefix to empty string");
                Logger.LogDebugInfo("Setting dzSpawnselectOutputFilepathPrefix to empty string");
                Logger.LogDebugInfo("Setting dzSpawnselectWorkshopOutputFilepathPrefix to empty string");

                backgroundImagesDirectory = string.Concat(debuggingJercPath, @"JERC\Resources\materials\jerc\backgrounds\");
                overviewsOutputFilepathPrefix = string.Concat(debuggingJercPath, mapName);
                dzTabletOutputFilepathPrefix = string.Concat(debuggingJercPath, "tablet_radar_", mapName);
                dzTabletWorkshopOutputFilepathPrefix = string.Concat(debuggingJercPath, @"tablet_radar_workshop\", configurationValues.workshopId, @"\", mapName);
                dzSpawnselectOutputFilepathPrefix = string.Concat(debuggingJercPath, "map_", mapName);
                dzSpawnselectWorkshopOutputFilepathPrefix = string.Concat(debuggingJercPath, @"map_workshop\", configurationValues.workshopId, @"\", mapName);

                extrasOutputFilepathPrefix = string.Concat(debuggingJercPath, mapName);
                extrasOutputFilepathPrefixOverview = string.Concat(debuggingJercPath, @"overview\", mapName);
                extrasOutputFilepathPrefixTablet = string.Concat(debuggingJercPath, @"tablet\", mapName);
                extrasOutputFilepathPrefixSpawnSelect = string.Concat(debuggingJercPath, @"spawnselect\", mapName);

                alternateExtrasOutputFilepathPrefix = string.Concat(debuggingJercPath, @"jerc_extra\", mapName);
                alternateExtrasOutputFilepathPrefixOverview = string.Concat(debuggingJercPath, @"jerc_extra\overview\", mapName);
                alternateExtrasOutputFilepathPrefixTablet = string.Concat(debuggingJercPath, @"jerc_extra\tablet\", mapName);
                alternateExtrasOutputFilepathPrefixSpawnSelect = string.Concat(debuggingJercPath, @"jerc_extra\spawnselect\", mapName);

                vmfcmdFilepath = string.Concat(debuggingJercPath, @"JERC\Resources\VTFCmd\VTFCmd.exe");
            }
            else
            {
                backgroundImagesDirectory = string.Concat(GameConfigurationValues.csgoFolderPath, @"materials\jerc\backgrounds\");
                overviewsOutputFilepathPrefix = string.Concat(GameConfigurationValues.overviewsFolderPath, mapName);
                dzTabletOutputFilepathPrefix = string.Concat(GameConfigurationValues.dzTabletFolderPath, "tablet_radar_", mapName);
                dzTabletWorkshopOutputFilepathPrefix = string.Concat(GameConfigurationValues.dzTabletFolderPath, @"tablet_radar_workshop\", configurationValues.workshopId, @"\", mapName);
                dzSpawnselectOutputFilepathPrefix = string.Concat(GameConfigurationValues.dzSpawnselectFolderPath, "map_", mapName);
                dzSpawnselectWorkshopOutputFilepathPrefix = string.Concat(GameConfigurationValues.dzSpawnselectFolderPath, @"map_workshop\", configurationValues.workshopId, @"\", mapName);

                extrasOutputFilepathPrefix = string.Concat(GameConfigurationValues.extrasFolderPath, mapName);
                extrasOutputFilepathPrefixOverview = string.Concat(GameConfigurationValues.extrasFolderPath, @"overview\", mapName);
                extrasOutputFilepathPrefixTablet = string.Concat(GameConfigurationValues.extrasFolderPath, @"tablet\", mapName);
                extrasOutputFilepathPrefixSpawnSelect = string.Concat(GameConfigurationValues.extrasFolderPath, @"spawnselect\", mapName);

                alternateExtrasOutputFilepathPrefix = string.Concat(configurationValues.alternateOutputPath, @"jerc_extras\", mapName);
                alternateExtrasOutputFilepathPrefixOverview = string.Concat(configurationValues.alternateOutputPath, @"jerc_extras\overview\", mapName);
                alternateExtrasOutputFilepathPrefixTablet = string.Concat(configurationValues.alternateOutputPath, @"jerc_extras\tablet\", mapName);
                alternateExtrasOutputFilepathPrefixSpawnSelect = string.Concat(configurationValues.alternateOutputPath, @"jerc_extras\spawnselect\", mapName);

                vmfcmdFilepath = string.Concat(GameConfigurationValues.binFolderPath, @"JERC\VTFCmd\VTFCmd.exe");
            }

            outputImageBackgroundLevelsFilepath = string.Concat(extrasOutputFilepathPrefix, "_background_levels.png");

            if (debugging)
            {
                Logger.LogDebugInfo("Setting alternateOutputPath to empty string");

                configurationValues.alternateOutputPath = string.Empty;
            }

            Logger.LogNewLine();

            SortScaleStuff();

            if (overviewPositionValues == null)
            {
                Logger.LogError("No brushes or displacements found, exiting");
                return;
            }

            var levelHeights = GetLevelHeights();

            if (levelHeights == null || !levelHeights.Any())
                Logger.LogImportantWarning("Number of level heights found: 0");
            else
                Logger.LogMessage(string.Concat("Number of level heights found: ", levelHeights.Count()));

            Logger.LogNewLine();

            GenerateRadars(levelHeights);

            Logger.LogNewLine();

            if (configurationValues.exportTxt)
                GenerateTxt(levelHeights);

            Logger.LogNewLine();

            var allDisplayedBrushSides = vmfRequiredData.GetAllDisplayedBrushSides();
            foreach (var brushSideId in configurationValues.displacementRotationSideIds90.Where(x => !allDisplayedBrushSides.Any(y => y.id == x)))
            {
                Logger.LogImportantWarning($"Could not find brush side {brushSideId} to rotate 90 degrees clockwise");
            }
            foreach (var brushSideId in configurationValues.displacementRotationSideIds180.Where(x => !allDisplayedBrushSides.Any(y => y.id == x)))
            {
                Logger.LogImportantWarning($"Could not find brush side {brushSideId} to rotate 180 degrees");
            }
            foreach (var brushSideId in configurationValues.displacementRotationSideIds270.Where(x => !allDisplayedBrushSides.Any(y => y.id == x)))
            {
                Logger.LogImportantWarning($"Could not find brush side {brushSideId} to rotate 90 degrees anti-clockwise");
            }
        }


        private static void CorrectOverlayOriginsAndAngles(IEnumerable<IVNode> entityList)
        {
            foreach (var entity in entityList)
            {
                // overlays (before the fake brush is created, the vertices need rotating)
                if (entity.Body.FirstOrDefault(x => x.Name == "classname")?.Value == "info_overlay" || entity.Body.FirstOrDefault(x => x.Name == "classname")?.Value == "jerc_info_overlay")
                {
                    var originIVNode = entity.Body.FirstOrDefault(x => x.Name == "origin");
                    var anglesIVNode = entity.Body.FirstOrDefault(x => x.Name == "angles");

                    var uv0 = entity.Body.FirstOrDefault(x => x.Name == "uv0");
                    var uv1 = entity.Body.FirstOrDefault(x => x.Name == "uv1");
                    var uv2 = entity.Body.FirstOrDefault(x => x.Name == "uv2");
                    var uv3 = entity.Body.FirstOrDefault(x => x.Name == "uv3");

                    if (uv0 != null && uv1 != null && uv2 != null && uv3 != null)
                    {
                        var allVerticesOffsetsInOverlay = new List<IVNode>() { uv0, uv1, uv2, uv3 };
                        for (int i = 0; i < allVerticesOffsetsInOverlay.Count(); i++) // allVerticesOffsetsInOverlay.Count() should be 4
                        {
                            var overlayVerticesOffset = allVerticesOffsetsInOverlay[i];

                            var origin = new Vertices(originIVNode.Value);
                            var angles = anglesIVNode?.Value == null ? new Angle(0, 0, 0) : new Angle(anglesIVNode.Value);

                            var verticesString = MergeTwoVerticesAsString(originIVNode.Value, overlayVerticesOffset.Value); // removes the offset
                            verticesString = GetRotatedVerticesNewPositionAsString(new Vertices(verticesString), origin, angles.yaw); // removes the rotation

                            AddSingleJimVertices(entity, i, verticesString);
                        }
                    }
                }
            }
        }


        private static bool SortInstances(VMF vmf)
        {
            var instanceEntities = vmf.Body.Where(x => x.Name == "entity").Where(x => x.Body.Any(y => y.Name == "classname" && y.Value == Classnames.FuncInstance)).ToList();

            Logger.LogMessage(string.Concat(instanceEntities.Count(), " instance", instanceEntities.Count() == 1 ? string.Empty : "s", " found"));

            // create FuncInstances from the entity key values
            var funcInstances = new List<FuncInstance>();
            foreach (var instanceEntity in instanceEntities)
            {
                var newInstance = new FuncInstance(instanceEntity);

                if (newInstance != null && !string.IsNullOrWhiteSpace(newInstance.file) && newInstance.angles != null && newInstance.origin != null)
                {
                    funcInstances.Add(newInstance);
                }
            }

            // Parse the instance VMFs
            foreach (var instance in funcInstances)
            {
                var filepath = string.Concat(GameConfigurationValues.vmfFilepathDirectory, instance.file);

                if (!File.Exists(filepath))
                {
                    Logger.LogImportantWarning("Instance filepath does not exist, skipping: " + filepath);
                    continue;
                }

                VMF newVmf = null;

                try
                {
                    var lines = File.ReadAllLines(filepath);
                    newVmf = new VMF(lines);
                }
                catch (Exception e)
                {
                    Logger.LogError($"Could not read instance vmf: {instance.file}, it is potentially locked due to saving, aborting");
                }

                if (newVmf == null)
                {
                    Logger.LogImportantWarning("Instance vmf data was null, skipping. Entity ID: " + instance.id);
                    continue;
                }


                // add hidden stuff if applicable except when instance is hidden
                if (configurationValues.includeEvenWhenHidden)
                {
                    AddHiddenStuffToDifferentLocationInVmf(newVmf);
                }
                

                // correct entity origins and angles
                foreach (var entity in newVmf.Body.Where(x => x.Name == "entity"))
                {
                    // entity id is not changed
                    // brush rotation is not changed

                    string originalOriginValue = null;

                    var originIVNode = entity.Body.FirstOrDefault(x => x.Name == "origin");
                    if (originIVNode != null)
                    {
                        originalOriginValue = originIVNode.Value;
                        MoveAndRotateVerticesInInstance(instance, originIVNode);
                    }

                    var allBrushSidesInEntity = entity.Body.Where(x => x.Name == "solid" && x.Body != null)?.SelectMany(x => x.Body.Where(y => y.Name == "side" && y.Body != null)?.Select(y => y.Body))?.ToList();
                    MoveAndRotateAllBrushSidesInInstance(instance, allBrushSidesInEntity);

                    // overlays (before the fake brush is created, the vertices need rotating)
                    if (!string.IsNullOrWhiteSpace(originalOriginValue) && (entity.Body.FirstOrDefault(x => x.Name == "classname")?.Value == "info_overlay" || entity.Body.FirstOrDefault(x => x.Name == "classname")?.Value == "jerc_info_overlay"))
                    {
                        var uv0 = entity.Body.FirstOrDefault(x => x.Name == "uv0");
                        var uv1 = entity.Body.FirstOrDefault(x => x.Name == "uv1");
                        var uv2 = entity.Body.FirstOrDefault(x => x.Name == "uv2");
                        var uv3 = entity.Body.FirstOrDefault(x => x.Name == "uv3");

                        if (uv0 != null && uv1 != null && uv2 != null && uv3 != null)
                        {
                            var allVerticesOffsetsInOverlay = new List<IVNode>() { uv0, uv1, uv2, uv3 };
                            RotateOverlayVerticesInInstanceAndSetJimVertices(instance, entity, allVerticesOffsetsInOverlay, originalOriginValue);
                        }
                    }
                }

                var allWorldBrushSides = newVmf.World.Body.Where(x => x.Name == "solid").SelectMany(x => x.Body.Where(y => y.Name == "side").Select(y => y.Body)).ToList();
                MoveAndRotateAllBrushSidesInInstance(instance, allWorldBrushSides);

                instanceEntityIdsByVmf.Add(newVmf, instance.id);
            }

            Logger.LogMessage(string.Concat(instanceEntityIdsByVmf.Count(), " instance", instanceEntityIdsByVmf.Count() == 1 ? string.Empty : "s", " successfully parsed."));

            var numOfInstancesUnsuccessfullyParsed = instanceEntities.Count() - instanceEntityIdsByVmf.Count();
            if (numOfInstancesUnsuccessfullyParsed < 0)
            {
                Logger.LogError("Parsed more instances successfully than there are instance entities ?");
            }
            else if (numOfInstancesUnsuccessfullyParsed > 0)
            {
                var unsuccessfulInstanceEntityIds = funcInstances.Where(x => (instanceEntityIdsByVmf.Values.All(y => y != x.id))).ToList();

                Logger.LogImportantWarning(string.Concat(numOfInstancesUnsuccessfullyParsed, " instance", numOfInstancesUnsuccessfullyParsed == 1 ? string.Empty : "s", " unsuccessfully parsed:"));

                foreach (var funcInstance in unsuccessfulInstanceEntityIds)
                {
                    Logger.LogImportantWarning(string.Concat("Entity ID: ", funcInstance.id));
                }
            }

            return true;
        }


        private static void AddHiddenStuffToDifferentLocationInVmf(VMF vmf)
        {
            var hiddenIVNodesWorldBrushes = from x in vmf.World.Body
                                            where x.Name == "hidden"
                                            from y in x.Body
                                            where y.Name == "solid"
                                            select y;
            vmf.World.Body = vmf.World.Body.Concat(hiddenIVNodesWorldBrushes).ToList();

            // ignores hidden instances
            var hiddenIVNodesEntities = from x in vmf.Body
                                        where x.Name == "hidden"
                                        from y in x.Body
                                        where y.Name == "entity"
                                        from z in y.Body
                                        where z.Name == "classname"
                                        where z.Value != Classnames.FuncInstance
                                        select y;
            for (int i = 0; i < hiddenIVNodesEntities.Count(); i++)
            {
                var hiddenIVNodes = hiddenIVNodesEntities.ElementAt(i).Body.Where(x => x.Name == "hidden"); // could there be multiple of these ???
                if (hiddenIVNodes == null || !hiddenIVNodes.Any())
                    continue;

                for (int j = 0; j < hiddenIVNodes.Count(); j++)
                {
                    var solidIVNodes = hiddenIVNodes.ElementAt(j).Body.Where(x => x.Name == "solid"); // could there be multiple of these ???
                    if (solidIVNodes == null || !solidIVNodes.Any())
                        continue;

                    for (int k = 0; k < solidIVNodes.Count(); k++)
                    {
                        hiddenIVNodesEntities.ElementAt(i).Body.Add(solidIVNodes.ElementAt(k));
                    }
                }
            }
            vmf.Body = vmf.Body.Concat(hiddenIVNodesEntities).ToList();
        }


        private static void MoveAndRotateAllBrushSidesInInstance(FuncInstance instance, List<IList<IVNode>> brushSideIVNodeList)
        {
            foreach (var brushSide in brushSideIVNodeList)
            {
                // brush id is not changed
                // rotation is not changed

                // start position
                var startPosition = brushSide.Where(x => x.Name == "dispinfo").Select(x => x.Body.Where(y => y.Name == "startposition").FirstOrDefault()).FirstOrDefault();
                if (startPosition != null && !string.IsNullOrWhiteSpace(startPosition.Value))
                {
                    MoveAndRotateStartPositionInInstance(instance, brushSide.Where(x => x.Name == "dispinfo").Select(x => x.Body.Where(y => y.Name == "startposition").FirstOrDefault()).FirstOrDefault());
                }

                // vertices
                foreach (var verticesPlusIVNode in brushSide.Where(x => x.Name == "vertices_plus").SelectMany(x => x.Body))
                {
                    MoveAndRotateVerticesInInstance(instance, verticesPlusIVNode);
                }
            }
        }


        private static void MoveAndRotateStartPositionInInstance(FuncInstance instance, IVNode ivNode)
        {
            ivNode.Value = MergeVerticesToString(instance.origin, ivNode.Value.Replace("[", string.Empty).Replace("]", string.Empty)); // removes the offset that being in an instances causes
            ivNode.Value = GetRotatedVerticesNewPositionAsString(new Vertices(ivNode.Value), instance.origin, instance.angles.yaw); // removes the rotation that being in an instances causes
            ivNode.Value = "[" + ivNode.Value + "]";
        }


        private static void MoveAndRotateVerticesInInstance(FuncInstance instance, IVNode ivNode)
        {
            ivNode.Value = MergeVerticesToString(instance.origin, ivNode.Value); // removes the offset that being in an instances causes
            ivNode.Value = GetRotatedVerticesNewPositionAsString(new Vertices(ivNode.Value), instance.origin, instance.angles.yaw); // removes the rotation that being in an instances causes
        }


        private static void RotateOverlayVerticesInInstanceAndSetJimVertices(FuncInstance instance, IVNode entity, List<IVNode> allVerticesOffsetsInOverlay, string originalOriginValue)
        {
            for (int i = 0; i < allVerticesOffsetsInOverlay.Count(); i++) // allVerticesOffsetsInOverlay.Count() should be 4
            {
                var overlayVerticesOffset = allVerticesOffsetsInOverlay[i];

                var verticesString = MergeTwoVerticesAsString(MergeVerticesToString(instance.origin, overlayVerticesOffset.Value), originalOriginValue); // removes the offset that being in an instances causes
                verticesString = GetRotatedVerticesNewPositionAsString(new Vertices(verticesString), instance.origin, instance.angles.yaw); // removes the rotation that being in an instances causes

                AddSingleJimVertices(entity, i, verticesString);
            }
        }


        private static void AddSingleJimVertices(IVNode entity, int verticesIndex, string verticesString)
        {
            entity.Body.Add(new VProperty($"jim_vertices{verticesIndex}", verticesString));
        }


        private static void MoveAndRotateObjectToDrawAroundOrigin(ObjectToDraw objectToDraw, float degreesClockwise)
        {
            while (degreesClockwise < 0)
                degreesClockwise += 360;
            while (degreesClockwise > 359)
                degreesClockwise -= 360;

            if (degreesClockwise == 0)
                return;

            for (int i = 0; i < objectToDraw.verticesToDraw.Count(); i++)
            {
                var vertices = objectToDraw.verticesToDraw[i].vertices;
                var colour = objectToDraw.verticesToDraw[i].colour;

                objectToDraw.verticesToDraw[i] = new VerticesToDraw(GetRotatedVertices(vertices, objectToDraw.center, degreesClockwise), colour);
            }

            // change the order of the vertices to match BL,BR,TR,TL
            if (degreesClockwise >= 45 && degreesClockwise < 135) ////// this might be wrong for the edges (eg. 45) ??
            {
                var verticesToDrawNew = new List<VerticesToDraw>()
                {
                    objectToDraw.verticesToDraw[1],
                    objectToDraw.verticesToDraw[2],
                    objectToDraw.verticesToDraw[3],
                    objectToDraw.verticesToDraw[0]
                };

                objectToDraw.verticesToDraw = verticesToDrawNew;
            }
            else if (degreesClockwise >= 135 && degreesClockwise < 225)
            {
                var verticesToDrawNew = new List<VerticesToDraw>()
                {
                    objectToDraw.verticesToDraw[2],
                    objectToDraw.verticesToDraw[3],
                    objectToDraw.verticesToDraw[0],
                    objectToDraw.verticesToDraw[1]
                };

                objectToDraw.verticesToDraw = verticesToDrawNew;
            }
            else if (degreesClockwise >= 225 && degreesClockwise < 315)
            {
                var verticesToDrawNew = new List<VerticesToDraw>()
                {
                    objectToDraw.verticesToDraw[3],
                    objectToDraw.verticesToDraw[0],
                    objectToDraw.verticesToDraw[1],
                    objectToDraw.verticesToDraw[2]
                };

                objectToDraw.verticesToDraw = verticesToDrawNew;
            }
            /*else if (degreesClockwise >= 315 && degreesClockwise < 45) //// don't change the order
            { }*/
        }


        private static Vertices GetRotatedVertices(Vertices verticesPlus, Vertices origin, float degreesClockwise)
        {
            var newVertices = GetRotatedVerticesNewPositionAsVertices(verticesPlus, origin, degreesClockwise); // gets brush's vertices as rotated value

            return newVertices;
        }


        public static string MergeVerticesToString(Vertices vertices, string verticesString)
        {
            var verticesNew = GetVerticesFromString(verticesString);

            var xNew = vertices.x + verticesNew.x;
            var yNew = vertices.y + verticesNew.y;
            var zNew = vertices.z + verticesNew.z;

            return string.Concat(xNew, " ", yNew, " ", zNew);
        }


        public static string MergeTwoVerticesAsString(string verticesString1, string verticesString2)
        {
            var vertices1New = GetVerticesFromString(verticesString1);
            var vertices2New = GetVerticesFromString(verticesString2);

            var xNew = vertices1New.x + vertices2New.x;
            var yNew = vertices1New.y + vertices2New.y;
            var zNew = vertices1New.z + vertices2New.z;

            return string.Concat(xNew, " ", yNew, " ", zNew);
        }


        public static string MergeAnglesToString(Angle angle, string anglesString)
        {
            var anglesNew = GetAnglesFromString(anglesString);

            var pitchNew = angle.pitch + anglesNew.pitch;
            var yawNew = angle.yaw + anglesNew.yaw;
            var rollNew = angle.roll + anglesNew.roll;

            return string.Concat(pitchNew, " ", yawNew, " ", rollNew);
        }


        private static Vertices GetVerticesFromString(string verticesString)
        {
            var verticesStringSplit = verticesString.Split(" ");

            if (verticesStringSplit.Count() != 3)
                return null;

            float.TryParse(verticesStringSplit[0], Globalization.Style, Globalization.Culture, out var xCasted);
            float.TryParse(verticesStringSplit[1], Globalization.Style, Globalization.Culture, out var yCasted);
            float.TryParse(verticesStringSplit[2], Globalization.Style, Globalization.Culture, out var zCasted);

            return new Vertices(xCasted, yCasted, zCasted);
        }


        private static Angle GetAnglesFromString(string anglesString)
        {
            var anglesStringSplit = anglesString.Split(" ");

            if (anglesStringSplit.Count() != 3)
                return null;

            float.TryParse(anglesStringSplit[0], Globalization.Style, Globalization.Culture, out var pitchCasted);
            float.TryParse(anglesStringSplit[1], Globalization.Style, Globalization.Culture, out var yawCasted);
            float.TryParse(anglesStringSplit[2], Globalization.Style, Globalization.Culture, out var rollCasted);

            return new Angle(pitchCasted, yawCasted, rollCasted);
        }


        private static string GetRotatedVerticesNewPositionAsString(Vertices verticesToRotate, Vertices centerVertices, float angleInDegrees)
        {
            double angleInRadians = angleInDegrees * (Math.PI / 180);
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);

            var newVertices = new Vertices(
                (int)(cosTheta * (verticesToRotate.x - centerVertices.x) - sinTheta * (verticesToRotate.y - centerVertices.y) + centerVertices.x),
                (int)(sinTheta * (verticesToRotate.x - centerVertices.x) + cosTheta * (verticesToRotate.y - centerVertices.y) + centerVertices.y),
                (float)verticesToRotate.z
            );

            return newVertices.x + " " + newVertices.y + " " + newVertices.z;
        }


        private static Vertices GetRotatedVerticesNewPositionAsVertices(Vertices verticesToRotate, Vertices centerVertices, float angleInDegrees)
        {
            double angleInRadians = angleInDegrees * (Math.PI / 180);
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);

            var newVertices = new Vertices(
                (int)(cosTheta * (verticesToRotate.x - centerVertices.x) - sinTheta * (verticesToRotate.y - centerVertices.y) + centerVertices.x),
                (int)(sinTheta * (verticesToRotate.x - centerVertices.x) + cosTheta * (verticesToRotate.y - centerVertices.y) + centerVertices.y),
                (float)verticesToRotate.z
            );

            return newVertices;
        }


        private static void SetVisgroupIdsInMainVmf()
        {
            visgroupIdsInMainVmf = GetJercVisgroupIdsInVmf(vmf, VisgroupNames.Jerc);
        }


        private static void SetVisgroupIdsInInstancesByEntityId()
        {
            if (instanceEntityIdsByVmf != null && instanceEntityIdsByVmf.Any())
            {
                foreach (var instance in instanceEntityIdsByVmf)
                {
                    var id = GetJercVisgroupIdsInVmf(instance.Key, VisgroupNames.Jerc);

                    visgroupIdsInInstanceEntityIds.Add(instance.Value, id);
                }
            }
        }


        private static VisgroupIdsInVmf GetJercVisgroupIdsInVmf(VMF vmf, string visgroupName)
        {
            var visgroupIdsInVmf = new VisgroupIdsInVmf()
            {
                Jerc = GetJercVisgroupIdInVmf(vmf, VisgroupNames.Jerc),
                JercRemove = GetJercVisgroupIdInVmf(vmf, VisgroupNames.JercRemove),
                JercPath = GetJercVisgroupIdInVmf(vmf, VisgroupNames.JercPath),
                JercCover = GetJercVisgroupIdInVmf(vmf, VisgroupNames.JercCover),
                JercOverlap = GetJercVisgroupIdInVmf(vmf, VisgroupNames.JercOverlap),
                JercDoor = GetJercVisgroupIdInVmf(vmf, VisgroupNames.JercDoor),
                JercLadder = GetJercVisgroupIdInVmf(vmf, VisgroupNames.JercLadder),
                JercDanger = GetJercVisgroupIdInVmf(vmf, VisgroupNames.JercDanger),
            };

            return visgroupIdsInVmf;
        }


        private static int GetJercVisgroupIdInVmf(VMF vmf, string visgroupName)
        {
            int.TryParse((from x in vmf.VisGroups.Body
                          from y in x.Body
                          where y.Name == "name"
                          where y.Value.ToLower() == visgroupName.ToLower()
                          select x.Body.FirstOrDefault(y => y.Name == "visgroupid").Value)
            .FirstOrDefault(),
            Globalization.Style, Globalization.Culture, out var id);

            return id;
        }


        private static VmfRequiredData GetVmfRequiredData(IEnumerable<IVNode> allWorldBrushes, IEnumerable<IVNode> allEntities, IEnumerable<IVNode> jercConfigEntities)
        {
            Logger.LogNewLine();
            Logger.LogMessage("Getting required data from the main vmf and instances...");

            // instances contents
            if (instanceEntityIdsByVmf != null && instanceEntityIdsByVmf.Any())
            {
                foreach (var instanceEntityIdByVmf in instanceEntityIdsByVmf)
                {
                    var instanceEntityId = instanceEntityIdByVmf.Value;
                    var instance = instanceEntityIdByVmf.Key;

                    allWorldBrushes = allWorldBrushes.Concat(instance.World.Body.Where(x => x.Name == "solid"));
                    allEntities = allEntities.Concat(instance.Body.Where(x => x.Name == "entity"));
                }
            }

            // used for both world brushes and displacements
            var allWorldBrushesInJercVisgroup = from x in allWorldBrushes
                                                from y in x.Body
                                                where y.Name == "editor"
                                                from z in y.Body
                                                where z.Name == "visgroupid"
                                                where int.Parse(z.Value, Globalization.Style, Globalization.Culture) == visgroupIdsInMainVmf.Jerc ||
                                                    visgroupIdsInInstanceEntityIds.Values.Any(a => a.Jerc == int.Parse(z.Value, Globalization.Style, Globalization.Culture))
                                                select x;

            // separated visgroups mode
            var allWorldBrushesInJercRemoveVisgroup = from x in allWorldBrushes
                                                      from y in x.Body
                                                      where y.Name == "editor"
                                                      from z in y.Body
                                                      where z.Name == "visgroupid"
                                                      where int.Parse(z.Value, Globalization.Style, Globalization.Culture) == visgroupIdsInMainVmf.JercRemove ||
                                                          visgroupIdsInInstanceEntityIds.Values.Any(a => a.JercRemove == int.Parse(z.Value, Globalization.Style, Globalization.Culture))
                                                      select x;

            var allWorldBrushesInJercPathVisgroup = from x in allWorldBrushes
                                                      from y in x.Body
                                                      where y.Name == "editor"
                                                      from z in y.Body
                                                      where z.Name == "visgroupid"
                                                      where int.Parse(z.Value, Globalization.Style, Globalization.Culture) == visgroupIdsInMainVmf.JercPath ||
                                                          visgroupIdsInInstanceEntityIds.Values.Any(a => a.JercPath == int.Parse(z.Value, Globalization.Style, Globalization.Culture))
                                                      select x;

            var allWorldBrushesInJercCoverVisgroup = from x in allWorldBrushes
                                                      from y in x.Body
                                                      where y.Name == "editor"
                                                      from z in y.Body
                                                      where z.Name == "visgroupid"
                                                      where int.Parse(z.Value, Globalization.Style, Globalization.Culture) == visgroupIdsInMainVmf.JercCover ||
                                                          visgroupIdsInInstanceEntityIds.Values.Any(a => a.JercCover == int.Parse(z.Value, Globalization.Style, Globalization.Culture))
                                                      select x;

            var allWorldBrushesInJercOverlapVisgroup = from x in allWorldBrushes
                                                      from y in x.Body
                                                      where y.Name == "editor"
                                                      from z in y.Body
                                                      where z.Name == "visgroupid"
                                                      where int.Parse(z.Value, Globalization.Style, Globalization.Culture) == visgroupIdsInMainVmf.JercOverlap ||
                                                          visgroupIdsInInstanceEntityIds.Values.Any(a => a.JercOverlap == int.Parse(z.Value, Globalization.Style, Globalization.Culture))
                                                      select x;

            var allWorldBrushesInJercDoorVisgroup = from x in allWorldBrushes
                                                      from y in x.Body
                                                      where y.Name == "editor"
                                                      from z in y.Body
                                                      where z.Name == "visgroupid"
                                                      where int.Parse(z.Value, Globalization.Style, Globalization.Culture) == visgroupIdsInMainVmf.JercDoor ||
                                                          visgroupIdsInInstanceEntityIds.Values.Any(a => a.JercDoor == int.Parse(z.Value, Globalization.Style, Globalization.Culture))
                                                      select x;

            var allWorldBrushesInJercLadderVisgroup = from x in allWorldBrushes
                                                      from y in x.Body
                                                      where y.Name == "editor"
                                                      from z in y.Body
                                                      where z.Name == "visgroupid"
                                                      where int.Parse(z.Value, Globalization.Style, Globalization.Culture) == visgroupIdsInMainVmf.JercLadder ||
                                                          visgroupIdsInInstanceEntityIds.Values.Any(a => a.JercLadder == int.Parse(z.Value, Globalization.Style, Globalization.Culture))
                                                      select x;

            var allWorldBrushesInJercDangerVisgroup = from x in allWorldBrushes
                                                      from y in x.Body
                                                      where y.Name == "editor"
                                                      from z in y.Body
                                                      where z.Name == "visgroupid"
                                                      where int.Parse(z.Value, Globalization.Style, Globalization.Culture) == visgroupIdsInMainVmf.JercDanger ||
                                                          visgroupIdsInInstanceEntityIds.Values.Any(a => a.JercDanger == int.Parse(z.Value, Globalization.Style, Globalization.Culture))
                                                      select x;


            // brushes
            var brushesIgnore = GetBrushesByTextureName(allWorldBrushesInJercVisgroup, TextureNames.IgnoreTextureName);
            var brushesRemove = GetBrushesByTextureName(allWorldBrushesInJercVisgroup, TextureNames.RemoveTextureName).Concat(allWorldBrushesInJercRemoveVisgroup); // concats the brushes in separated visgroups mode
            var brushesPath = GetBrushesByTextureName(allWorldBrushesInJercVisgroup, TextureNames.PathTextureName).Concat(allWorldBrushesInJercPathVisgroup);
            var brushesCover = GetBrushesByTextureName(allWorldBrushesInJercVisgroup, TextureNames.CoverTextureName).Concat(allWorldBrushesInJercCoverVisgroup);
            var brushesOverlap = GetBrushesByTextureName(allWorldBrushesInJercVisgroup, TextureNames.OverlapTextureName).Concat(allWorldBrushesInJercOverlapVisgroup);
            var brushesDoor = GetBrushesByTextureName(allWorldBrushesInJercVisgroup, TextureNames.DoorTextureName).Concat(allWorldBrushesInJercDoorVisgroup);
            var brushesLadder = new List<IVNode>();
            foreach (var ladderTextureName in TextureNames.LadderTextureNames)
            {
                brushesLadder.AddRange(GetBrushesByTextureName(allWorldBrushesInJercVisgroup, ladderTextureName));
            }
            brushesLadder.AddRange(allWorldBrushesInJercLadderVisgroup);
            var brushesDanger = GetBrushesByTextureName(allWorldBrushesInJercVisgroup, TextureNames.DangerTextureName).Concat(allWorldBrushesInJercDangerVisgroup);

            var brushesBuyzone = GetBrushesByTextureName(allWorldBrushesInJercVisgroup, TextureNames.BuyzoneTextureName);
            var brushesBombsiteA = GetBrushesByTextureName(allWorldBrushesInJercVisgroup, TextureNames.BombsiteATextureName);
            var brushesBombsiteB = GetBrushesByTextureName(allWorldBrushesInJercVisgroup, TextureNames.BombsiteBTextureName);
            var brushesRescueZone = GetBrushesByTextureName(allWorldBrushesInJercVisgroup, TextureNames.RescueZoneTextureName);
            var brushesHostage = GetBrushesByTextureName(allWorldBrushesInJercVisgroup, TextureNames.HostageTextureName);
            var brushesTSpawn = GetBrushesByTextureName(allWorldBrushesInJercVisgroup, TextureNames.TSpawnTextureName);
            var brushesCTSpawn = GetBrushesByTextureName(allWorldBrushesInJercVisgroup, TextureNames.CTSpawnTextureName);


            // displacements
            var displacementsIgnore = GetDisplacementsByTextureName(allWorldBrushesInJercVisgroup, TextureNames.IgnoreTextureName);
            var displacementsRemove = GetDisplacementsByTextureName(allWorldBrushesInJercVisgroup, TextureNames.RemoveTextureName);
            var displacementsPath = GetDisplacementsByTextureName(allWorldBrushesInJercVisgroup, TextureNames.PathTextureName);
            var displacementsCover = GetDisplacementsByTextureName(allWorldBrushesInJercVisgroup, TextureNames.CoverTextureName);
            var displacementsOverlap = GetDisplacementsByTextureName(allWorldBrushesInJercVisgroup, TextureNames.OverlapTextureName);
            var displacementsDoor = GetDisplacementsByTextureName(allWorldBrushesInJercVisgroup, TextureNames.DoorTextureName);
            var displacementsLadder = new List<IVNode>();
            foreach (var ladderTextureName in TextureNames.LadderTextureNames)
            {
                displacementsLadder.AddRange(GetDisplacementsByTextureName(allWorldBrushesInJercVisgroup, ladderTextureName));
            }
            var displacementsDanger = GetDisplacementsByTextureName(allWorldBrushesInJercVisgroup, TextureNames.DangerTextureName);

            var displacementsBuyzone = GetDisplacementsByTextureName(allWorldBrushesInJercVisgroup, TextureNames.BuyzoneTextureName);
            var displacementsBombsiteA = GetDisplacementsByTextureName(allWorldBrushesInJercVisgroup, TextureNames.BombsiteATextureName);
            var displacementsBombsiteB = GetDisplacementsByTextureName(allWorldBrushesInJercVisgroup, TextureNames.BombsiteBTextureName);
            var displacementsRescueZone = GetDisplacementsByTextureName(allWorldBrushesInJercVisgroup, TextureNames.RescueZoneTextureName);
            var displacementsHostage = GetDisplacementsByTextureName(allWorldBrushesInJercVisgroup, TextureNames.HostageTextureName);
            var displacementsTSpawn = GetDisplacementsByTextureName(allWorldBrushesInJercVisgroup, TextureNames.TSpawnTextureName);
            var displacementsCTSpawn = GetDisplacementsByTextureName(allWorldBrushesInJercVisgroup, TextureNames.CTSpawnTextureName);


            // brush entities
            var buyzoneBrushEntities = GetEntitiesByClassnameInJercVisgroup(allEntities, Classnames.Buyzone);
            var bombsiteBrushEntities = GetEntitiesByClassnameInJercVisgroup(allEntities, Classnames.Bombsite);
            var rescueZoneBrushEntities = GetEntitiesByClassnameInJercVisgroup(allEntities, Classnames.RescueZone);

            var funcBrushBrushEntities = GetEntitiesByClassnameInJercVisgroup(allEntities, Classnames.FuncBrush);
            var funcDetailBrushEntities = GetEntitiesByClassnameInJercVisgroup(allEntities, Classnames.FuncDetail);
            var funcDoorBrushEntities = GetEntitiesByClassnameInJercVisgroup(allEntities, Classnames.FuncDoor);
            var funcDoorBrushRotatingEntities = GetEntitiesByClassnameInJercVisgroup(allEntities, Classnames.FuncDoorRotating);
            var funcLadderBrushEntities = GetEntitiesByClassnameInJercVisgroup(allEntities, Classnames.FuncLadder);
            var triggerHurtBrushEntities = GetEntitiesByClassnameInJercVisgroup(allEntities, Classnames.TriggerHurt);

            var allBrushesBrushEntitiesInJercVisgroup = buyzoneBrushEntities
                .Concat(bombsiteBrushEntities)
                .Concat(rescueZoneBrushEntities)
                .Concat(funcBrushBrushEntities)
                .Concat(funcDetailBrushEntities)
                .Concat(funcDoorBrushEntities)
                .Concat(funcDoorBrushRotatingEntities)
                .Concat(funcLadderBrushEntities)
                .Concat(triggerHurtBrushEntities);

            var brushesIgnoreBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndTriggerHurts(allBrushesBrushEntitiesInJercVisgroup, TextureNames.IgnoreTextureName);
            var brushesRemoveBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndTriggerHurts(allBrushesBrushEntitiesInJercVisgroup, TextureNames.RemoveTextureName).Concat(GetBrushEntityBrushes(GetEntitiesByClassnameInAnyJercVisgroup(allEntities, VisgroupNames.JercRemove))); // concats the brush entity brushes in separated visgroups mode
            var brushesPathBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndTriggerHurts(allBrushesBrushEntitiesInJercVisgroup, TextureNames.PathTextureName).Concat(GetBrushEntityBrushes(GetEntitiesByClassnameInAnyJercVisgroup(allEntities, VisgroupNames.JercPath)));
            var brushesCoverBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndTriggerHurts(allBrushesBrushEntitiesInJercVisgroup, TextureNames.CoverTextureName).Concat(GetBrushEntityBrushes(GetEntitiesByClassnameInAnyJercVisgroup(allEntities, VisgroupNames.JercCover)));
            var brushesOverlapBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndTriggerHurts(allBrushesBrushEntitiesInJercVisgroup, TextureNames.OverlapTextureName).Concat(GetBrushEntityBrushes(GetEntitiesByClassnameInAnyJercVisgroup(allEntities, VisgroupNames.JercOverlap)));
            var brushesDoorBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndTriggerHurts(allBrushesBrushEntitiesInJercVisgroup, TextureNames.DoorTextureName)
                .Concat(GetBrushEntityBrushesByClassname(allBrushesBrushEntitiesInJercVisgroup, Classnames.FuncDoor))
                .Concat(GetBrushEntityBrushesByClassname(allBrushesBrushEntitiesInJercVisgroup, Classnames.FuncDoorRotating))
                .Concat(GetBrushEntityBrushes(GetEntitiesByClassnameInAnyJercVisgroup(allEntities, VisgroupNames.JercDoor)));
            var brushesLadderBrushEntities = GetBrushEntityBrushesByClassname(allBrushesBrushEntitiesInJercVisgroup, Classnames.FuncLadder).Concat(GetBrushEntityBrushes(GetEntitiesByClassnameInAnyJercVisgroup(allEntities, VisgroupNames.JercLadder))).ToList();
            foreach (var ladderTextureName in TextureNames.LadderTextureNames)
            {
                brushesLadderBrushEntities.AddRange(GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndTriggerHurts(allBrushesBrushEntitiesInJercVisgroup, ladderTextureName));
            }
            var brushesDangerBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndTriggerHurts(allBrushesBrushEntitiesInJercVisgroup, TextureNames.DangerTextureName)
                .Concat(GetBrushEntityBrushesByClassname(allBrushesBrushEntitiesInJercVisgroup, Classnames.TriggerHurt))
                .Concat(GetBrushEntityBrushes(GetEntitiesByClassnameInAnyJercVisgroup(allEntities, VisgroupNames.JercDanger)));
            var brushesBuyzoneBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndTriggerHurts(allBrushesBrushEntitiesInJercVisgroup, TextureNames.BuyzoneTextureName);
            var brushesBombsiteABrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndTriggerHurts(allBrushesBrushEntitiesInJercVisgroup, TextureNames.BombsiteATextureName);
            var brushesBombsiteBBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndTriggerHurts(allBrushesBrushEntitiesInJercVisgroup, TextureNames.BombsiteBTextureName);
            var brushesRescueZoneBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndTriggerHurts(allBrushesBrushEntitiesInJercVisgroup, TextureNames.RescueZoneTextureName);
            var brushesHostageBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndTriggerHurts(allBrushesBrushEntitiesInJercVisgroup, TextureNames.HostageTextureName);
            var brushesTSpawnBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndTriggerHurts(allBrushesBrushEntitiesInJercVisgroup, TextureNames.TSpawnTextureName);
            var brushesCTSpawnBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndTriggerHurts(allBrushesBrushEntitiesInJercVisgroup, TextureNames.CTSpawnTextureName);


            // brush entities (JERC)
            var jercBoxBrushEntities = GetEntitiesByClassnameInJercVisgroup(allEntities, Classnames.JercBox);


            // point entities
            var hostageEntities = GetEntitiesByClassnameInJercVisgroup(allEntities, Classnames.Hostage);
            var tSpawnEntities = GetEntitiesByClassnameInJercVisgroup(allEntities, Classnames.TSpawn);
            var ctSpawnEntities = GetEntitiesByClassnameInJercVisgroup(allEntities, Classnames.CTSpawn);

            var infoOverlayEntities = GetEntitiesByClassnameInJercVisgroup(allEntities, Classnames.InfoOverlay);
            var jercInfoOverlayEntities = GetEntitiesByClassnameInJercVisgroup(allEntities, Classnames.JercInfoOverlay);


            // point entities (JERC)
            var jercDividerEntities = GetEntitiesByClassname(allEntities, Classnames.JercDivider);
            var jercFloorEntities = GetEntitiesByClassname(allEntities, Classnames.JercFloor);
            var jercCeilingEntities = GetEntitiesByClassname(allEntities, Classnames.JercCeiling);
            var jercDispRotationEntities = GetEntitiesByClassname(allEntities, Classnames.JercDispRotation);

            if (jercDispRotationEntities != null && jercDispRotationEntities.Count() > 1)
                return null;

            var allJercEntitiesExceptJercConfig = jercDividerEntities.Concat(jercFloorEntities).Concat(jercCeilingEntities).Concat(jercDispRotationEntities);

            configurationValues.SetAllOtherSettingsValues(GetSettingsValuesFromJercEntitiesExceptJercConfig(allJercEntitiesExceptJercConfig), jercDividerEntities.Count(), jercDispRotationEntities.Any());

            Logger.LogMessage("Finished getting required data from the main vmf and instances");

            return new VmfRequiredData(
                configurationValues,
                brushesIgnore, brushesRemove, brushesPath, brushesCover, brushesOverlap, brushesDoor, brushesLadder, brushesDanger,
                brushesBuyzone, brushesBombsiteA, brushesBombsiteB, brushesRescueZone, brushesHostage, brushesTSpawn, brushesCTSpawn,
                displacementsIgnore, displacementsRemove, displacementsPath, displacementsCover, displacementsOverlap, displacementsDoor, displacementsLadder, displacementsDanger,
                displacementsBuyzone, displacementsBombsiteA, displacementsBombsiteB, displacementsRescueZone, displacementsHostage, displacementsTSpawn, displacementsCTSpawn,
                buyzoneBrushEntities, bombsiteBrushEntities, rescueZoneBrushEntities,
                brushesIgnoreBrushEntities, brushesRemoveBrushEntities, brushesPathBrushEntities, brushesCoverBrushEntities, brushesOverlapBrushEntities, brushesDoorBrushEntities, brushesLadderBrushEntities, brushesDangerBrushEntities,
                brushesBuyzoneBrushEntities, brushesBombsiteABrushEntities, brushesBombsiteBBrushEntities, brushesRescueZoneBrushEntities, brushesHostageBrushEntities, brushesTSpawnBrushEntities, brushesCTSpawnBrushEntities,
                jercBoxBrushEntities,
                hostageEntities, ctSpawnEntities, tSpawnEntities,
                infoOverlayEntities, jercInfoOverlayEntities,
                jercConfigEntities, jercDividerEntities, jercFloorEntities, jercCeilingEntities, jercDispRotationEntities
            );
        }


        private static Dictionary<string, string> GetSettingsValuesFromJercConfig(IVNode jercConfigEntity)
        {
            var jercEntitySettingsValues = new Dictionary<string, string>();

            // jerc_config
            var jercConfig = jercConfigEntity.Body;

            jercEntitySettingsValues.Add("workshopId", jercConfig.FirstOrDefault(x => x.Name == "workshopId")?.Value);
            jercEntitySettingsValues.Add("overviewGamemodeType", jercConfig.FirstOrDefault(x => x.Name == "overviewGamemodeType")?.Value);
            jercEntitySettingsValues.Add("dangerZoneUses", jercConfig.FirstOrDefault(x => x.Name == "dangerZoneUses")?.Value);
            jercEntitySettingsValues.Add("alternateOutputPath", jercConfig.FirstOrDefault(x => x.Name == "alternateOutputPath")?.Value ?? string.Empty);
            jercEntitySettingsValues.Add("onlyOutputToAlternatePath", jercConfig.FirstOrDefault(x => x.Name == "onlyOutputToAlternatePath")?.Value);
            jercEntitySettingsValues.Add("includeEvenWhenHidden", jercConfig.FirstOrDefault(x => x.Name == "includeEvenWhenHidden")?.Value);
            jercEntitySettingsValues.Add("exportRadarAsSeparateLevels", jercConfig.FirstOrDefault(x => x.Name == "exportRadarAsSeparateLevels")?.Value);
            jercEntitySettingsValues.Add("useSeparateGradientEachLevel", jercConfig.FirstOrDefault(x => x.Name == "useSeparateGradientEachLevel")?.Value);
            jercEntitySettingsValues.Add("ignoreDisplacementXYChanges", jercConfig.FirstOrDefault(x => x.Name == "ignoreDisplacementXYChanges")?.Value);
            jercEntitySettingsValues.Add("rotateCutDispsAutomatic", jercConfig.FirstOrDefault(x => x.Name == "rotateCutDispsAutomatic")?.Value);
            jercEntitySettingsValues.Add("backgroundFilename", jercConfig.FirstOrDefault(x => x.Name == "backgroundFilename")?.Value ?? string.Empty);
            jercEntitySettingsValues.Add("radarSizeMultiplier", jercConfig.FirstOrDefault(x => x.Name == "radarSizeMultiplier")?.Value);
            jercEntitySettingsValues.Add("overlapAlpha", jercConfig.FirstOrDefault(x => x.Name == "overlapAlpha")?.Value);
            jercEntitySettingsValues.Add("dangerAlpha", jercConfig.FirstOrDefault(x => x.Name == "dangerAlpha")?.Value);
            jercEntitySettingsValues.Add("pathColourHigh", jercConfig.FirstOrDefault(x => x.Name == "pathColourHigh")?.Value);
            jercEntitySettingsValues.Add("pathColourLow", jercConfig.FirstOrDefault(x => x.Name == "pathColourLow")?.Value);
            jercEntitySettingsValues.Add("overlapColourHigh", jercConfig.FirstOrDefault(x => x.Name == "overlapColourHigh")?.Value);
            jercEntitySettingsValues.Add("overlapColourLow", jercConfig.FirstOrDefault(x => x.Name == "overlapColourLow")?.Value);
            jercEntitySettingsValues.Add("coverColourHigh", jercConfig.FirstOrDefault(x => x.Name == "coverColourHigh")?.Value);
            jercEntitySettingsValues.Add("coverColourLow", jercConfig.FirstOrDefault(x => x.Name == "coverColourLow")?.Value);
            jercEntitySettingsValues.Add("doorColour", jercConfig.FirstOrDefault(x => x.Name == "doorColour")?.Value);
            jercEntitySettingsValues.Add("ladderColour", jercConfig.FirstOrDefault(x => x.Name == "ladderColour")?.Value);
            jercEntitySettingsValues.Add("dangerColour", jercConfig.FirstOrDefault(x => x.Name == "dangerColour")?.Value);
            jercEntitySettingsValues.Add("overlaysColour", jercConfig.FirstOrDefault(x => x.Name == "overlaysColour")?.Value);
            jercEntitySettingsValues.Add("strokeWidth", jercConfig.FirstOrDefault(x => x.Name == "strokeWidth")?.Value);
            jercEntitySettingsValues.Add("strokeColour", jercConfig.FirstOrDefault(x => x.Name == "strokeColour")?.Value);
            jercEntitySettingsValues.Add("strokeAroundLayoutMaterials", jercConfig.FirstOrDefault(x => x.Name == "strokeAroundLayoutMaterials")?.Value);
            jercEntitySettingsValues.Add("strokeAroundRemoveMaterials", jercConfig.FirstOrDefault(x => x.Name == "strokeAroundRemoveMaterials")?.Value);
            jercEntitySettingsValues.Add("strokeAroundEntities", jercConfig.FirstOrDefault(x => x.Name == "strokeAroundEntities")?.Value);
            jercEntitySettingsValues.Add("strokeAroundBrushEntities", jercConfig.FirstOrDefault(x => x.Name == "strokeAroundBrushEntities")?.Value);
            jercEntitySettingsValues.Add("strokeAroundOverlays", jercConfig.FirstOrDefault(x => x.Name == "strokeAroundOverlays")?.Value);
            jercEntitySettingsValues.Add("defaultLevelNum", jercConfig.FirstOrDefault(x => x.Name == "defaultLevelNum")?.Value);
            jercEntitySettingsValues.Add("levelBackgroundEnabled", jercConfig.FirstOrDefault(x => x.Name == "levelBackgroundEnabled")?.Value);
            jercEntitySettingsValues.Add("levelBackgroundDarkenAlpha", jercConfig.FirstOrDefault(x => x.Name == "levelBackgroundDarkenAlpha")?.Value);
            jercEntitySettingsValues.Add("levelBackgroundBlurAmount", jercConfig.FirstOrDefault(x => x.Name == "levelBackgroundBlurAmount")?.Value);
            jercEntitySettingsValues.Add("higherLevelOutputName", jercConfig.FirstOrDefault(x => x.Name == "higherLevelOutputName")?.Value);
            jercEntitySettingsValues.Add("lowerLevelOutputName", jercConfig.FirstOrDefault(x => x.Name == "lowerLevelOutputName")?.Value);
            jercEntitySettingsValues.Add("exportTxt", jercConfig.FirstOrDefault(x => x.Name == "exportTxt")?.Value);
            jercEntitySettingsValues.Add("exportDds", jercConfig.FirstOrDefault(x => x.Name == "exportDds")?.Value);
            jercEntitySettingsValues.Add("exportPng", jercConfig.FirstOrDefault(x => x.Name == "exportPng")?.Value);
            jercEntitySettingsValues.Add("exportRawMasks", jercConfig.FirstOrDefault(x => x.Name == "exportRawMasks")?.Value);
            jercEntitySettingsValues.Add("exportBackgroundLevelsImage", jercConfig.FirstOrDefault(x => x.Name == "exportBackgroundLevelsImage")?.Value);

            return jercEntitySettingsValues;
        }


        private static Dictionary<string, string> GetSettingsValuesFromJercEntitiesExceptJercConfig(IEnumerable<IVNode> jercEntities)
        {
            var jercEntitySettingsValues = new Dictionary<string, string>();

            // jerc_disp_rotation
            var jercDispRotation = jercEntities.FirstOrDefault(x => x.Body.Any(y => y.Name == "classname" && y.Value == Classnames.JercDispRotation))?.Body;

            if (jercDispRotation != null)
            {
                jercEntitySettingsValues.Add("displacementRotationSideIds90", jercDispRotation.FirstOrDefault(x => x.Name == "displacementRotationSideIds90")?.Value);
                jercEntitySettingsValues.Add("displacementRotationSideIds180", jercDispRotation.FirstOrDefault(x => x.Name == "displacementRotationSideIds180")?.Value);
                jercEntitySettingsValues.Add("displacementRotationSideIds270", jercDispRotation.FirstOrDefault(x => x.Name == "displacementRotationSideIds270")?.Value);
            }

            return jercEntitySettingsValues;
        }


        private static IEnumerable<IVNode> GetBrushesByTextureName(IEnumerable<IVNode> allWorldBrushesInVisgroup, string textureName)
        {
            var allWorldBrushesAndDisplacements = (from x in allWorldBrushesInVisgroup
                    from y in x.Body
                    where y.Name == "side"
                    from z in y.Body
                    where z.Name == "material"
                    where z.Value.ToLower() == textureName.ToLower()
                    select x).Distinct().ToList();

            var allWorldDisplacements = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, textureName);

            return allWorldBrushesAndDisplacements.Where(x => !allWorldDisplacements.Any(y => y.Body.FirstOrDefault(z => z.Name == "id").Value == x.Body.FirstOrDefault(z => z.Name == "id").Value));
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


        private static IEnumerable<IVNode> GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndTriggerHurts(IEnumerable<IVNode> allBrushEntities, string textureName)
        {
            return (from x in allBrushEntities
                    from y in x.Body
                    where y.Name == "classname"
                    where y.Value.ToLower() != Classnames.FuncDoor && y.Value.ToLower() != Classnames.FuncDoorRotating && y.Value.ToLower() != Classnames.FuncLadder && y.Value.ToLower() != Classnames.TriggerHurt
                    from z in x.Body
                    where z.Name == "solid"
                    from a in z.Body
                    where a.Name == "side"
                    where !a.Body.Any(b => b.Name == "dispinfo")
                    from b in a.Body
                    where b.Name == "material"
                    where b.Value.ToLower() == textureName.ToLower()
                    select z).Distinct();
        }


        private static IEnumerable<IVNode> GetBrushEntityBrushesByClassname(IEnumerable<IVNode> allBrushEntities, string classname)
        {
            return (from x in allBrushEntities
                    from y in x.Body
                    where y.Name == "classname"
                    where y.Value.ToLower() == classname.ToLower()
                    from z in x.Body
                    where z.Name == "solid"
                    select z).Distinct();
        }


        private static IEnumerable<IVNode> GetBrushEntityBrushes(IEnumerable<IVNode> allBrushEntities)
        {
            return (from x in allBrushEntities
                    from y in x.Body
                    where y.Name == "solid"
                    select y).Distinct();
        }


        private static IEnumerable<IVNode> GetEntitiesByClassname(IEnumerable<IVNode> allEntities, string classname)
        {
            return (from x in allEntities
                    from y in x.Body
                    where y.Name == "classname"
                    where y.Value.ToLower() == classname.ToLower()
                    select x).Distinct();
        }


        private static IEnumerable<IVNode> GetEntitiesByClassnameInJercVisgroup(IEnumerable<IVNode> allEntities, string classname)
        {
            return (from x in allEntities
                    from y in x.Body
                    where y.Name == "classname"
                    where y.Value.ToLower() == classname.ToLower()
                    from z in x.Body
                    where z.Name == "editor"
                    from a in z.Body
                    where a.Name == "visgroupid"
                    where int.Parse(a.Value, Globalization.Style, Globalization.Culture) == visgroupIdsInMainVmf.Jerc ||
                    visgroupIdsInInstanceEntityIds.Values.Any(b => b.Jerc == int.Parse(a.Value, Globalization.Style, Globalization.Culture))
                    select x).Distinct();
        }


        private static IEnumerable<IVNode> GetEntitiesByClassnameInAnyJercVisgroup(IEnumerable<IVNode> allEntities, string visgroupName)
        {
            return (from x in allEntities
                    from y in x.Body
                    where y.Name == "classname"
                    where Classnames.GetAllClassnames().Any(x => x.ToLower() == y.Value.ToLower())
                    from z in x.Body
                    where z.Name == "editor"
                    from a in z.Body
                    where a.Name == "visgroupid"
                    where int.Parse(a.Value, Globalization.Style, Globalization.Culture) == visgroupIdsInMainVmf.GetVisgroupId(visgroupName) ||
                    visgroupIdsInInstanceEntityIds.Values.Any(b => b.GetVisgroupId(visgroupName) == int.Parse(a.Value, Globalization.Style, Globalization.Culture))
                    select x).Distinct();
        }


        private static void SortScaleStuff()
        {
            var allWorldBrushesAndDisplacementsExceptRemove = vmfRequiredData.brushesSidesPath
                .Concat(vmfRequiredData.brushesSidesCover)
                .Concat(vmfRequiredData.brushesSidesOverlap)
                .Concat(vmfRequiredData.brushesSidesDoor)
                .Concat(vmfRequiredData.brushesSidesLadder)
                .Concat(vmfRequiredData.brushesSidesDanger)
                .Concat(vmfRequiredData.displacementsSidesPath)
                .Concat(vmfRequiredData.displacementsSidesCover)
                .Concat(vmfRequiredData.displacementsSidesOverlap)
                .Concat(vmfRequiredData.displacementsSidesDoor)
                .Concat(vmfRequiredData.displacementsSidesLadder)
                .Concat(vmfRequiredData.displacementsSidesDanger);
            //var allWorldBrushes = vmfRequiredData.brushesSidesRemove.Concat(vmfRequiredData.displacementsSidesRemove).Concat(allWorldBrushesAndDisplacementsExceptRemove);

            if (allWorldBrushesAndDisplacementsExceptRemove == null || allWorldBrushesAndDisplacementsExceptRemove.Count() == 0)
                return;

            var minX = allWorldBrushesAndDisplacementsExceptRemove.Min(x => x.vertices_plus.Min(y => y.x));
            var maxX = allWorldBrushesAndDisplacementsExceptRemove.Max(x => x.vertices_plus.Max(y => y.x));
            var minY = allWorldBrushesAndDisplacementsExceptRemove.Min(x => x.vertices_plus.Min(y => y.y));
            var maxY = allWorldBrushesAndDisplacementsExceptRemove.Max(x => x.vertices_plus.Max(y => y.y));

            var sizeX = (maxX - minX) / configurationValues.radarSizeMultiplier;
            var sizeY = (maxY - minY) / configurationValues.radarSizeMultiplier;

            /*var scaleX = (sizeX - 1024) <= 0 ? 1 : ((sizeX - 1024) / OverviewOffsets.OverviewIncreasedUnitsShownPerScaleIntegerPosX) + 1;
            var scaleY = (sizeY - 1024) <= 0 ? 1 : ((sizeY - 1024) / OverviewOffsets.OverviewIncreasedUnitsShownPerScaleIntegerPosY) + 1;*/
            var scaleX = sizeX / OverviewOffsets.OverviewScaleDivider;
            var scaleY = sizeY / OverviewOffsets.OverviewScaleDivider;

            var scale = scaleX >= scaleY ? scaleX : scaleY;

            overviewPositionValues = new OverviewPositionValues(configurationValues, minX, maxX, minY, maxY, scale);

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

            var numOfOverviewLevels = configurationValues.exportRadarAsSeparateLevels ? jercDividerEntities.Count() + 1 : 1;
            for (int i = 0; i < numOfOverviewLevels; i++)
            {
                var overviewLevelName = string.Empty;

                var valueDiff = numOfOverviewLevels == 1 ? 0 : (i - configurationValues.defaultLevelNum); // set to 0 if there are no dividers
                if (valueDiff == 0)
                    overviewLevelName = "default";
                else if (valueDiff < 0)
                    overviewLevelName = string.Concat(configurationValues.lowerLevelOutputName, Math.Abs(valueDiff));
                else if (valueDiff > 0)
                    overviewLevelName = string.Concat(configurationValues.higherLevelOutputName, Math.Abs(valueDiff));

                var zMinForTxt = i == 0 ? -(Sizes.MaxHammerGridSize / 2) : levelHeights.ElementAt(i - 1).zMaxForTxt;
                var zMaxForTxt = i == (numOfOverviewLevels - 1) ? (Sizes.MaxHammerGridSize / 2) : new Vertices(jercDividerEntities.ElementAt(i).origin).z;

                var zMinForRadar = i == 0 ? vmfRequiredData.GetLowestVerticesZ() : levelHeights.ElementAt(i - 1).zMaxForRadar;
                var zMaxForRadar = i == (numOfOverviewLevels - 1) ? vmfRequiredData.GetHighestVerticesZ() : (float)(new Vertices(jercDividerEntities.ElementAt(i).origin).z);

                var jercFloorEntitiesInsideLevel = configurationValues.exportRadarAsSeparateLevels && configurationValues.useSeparateGradientEachLevel
                    ? vmfRequiredData.jercFloorEntities.Where(x => new Vertices(x.origin).z >= zMinForRadar && new Vertices(x.origin).z < zMaxForRadar).ToList()
                    : vmfRequiredData.jercFloorEntities;
                var zMinForGradient = jercFloorEntitiesInsideLevel.Any() ? jercFloorEntitiesInsideLevel.OrderBy(x => new Vertices(x.origin).z).Select(x => (float)(new Vertices(x.origin).z)).FirstOrDefault() : zMinForRadar; // takes the lowest (first) in the level if there are more than one

                var jercCeilingEntitiesInsideLevel = configurationValues.exportRadarAsSeparateLevels && configurationValues.useSeparateGradientEachLevel
                    ? vmfRequiredData.jercCeilingEntities.Where(x => new Vertices(x.origin).z >= zMinForRadar && new Vertices(x.origin).z < zMaxForRadar).ToList()
                    : vmfRequiredData.jercCeilingEntities;
                var zMaxForGradient = jercCeilingEntitiesInsideLevel.Any() ? jercCeilingEntitiesInsideLevel.OrderBy(x => new Vertices(x.origin).z).Select(x => (float)(new Vertices(x.origin).z)).LastOrDefault() : zMaxForRadar; // takes the highest (last) in the level if there are more than one

                levelHeights.Add(new LevelHeight(levelHeights.Count(), overviewLevelName, zMinForTxt, (float)zMaxForTxt, zMinForRadar, zMaxForRadar, zMinForGradient, zMaxForGradient));
            }

            return levelHeights;
        }


        private static void GenerateRadars(List<LevelHeight> levelHeights)
        {
            Logger.LogMessage("Generating radars...");

            var radarLevels = new List<RadarLevel>();

            // get overview for each separate level if levelBackgroundEnabled == true
            if (levelHeights.Count() > 1)
            {
                if (configurationValues.exportRadarAsSeparateLevels)
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
            if (configurationValues.exportRadarAsSeparateLevels && levelHeights != null && levelHeights.Count() > 1 && configurationValues.levelBackgroundEnabled)
            {
                var backgroundBmp = GetBackgroundToRadarLevels(radarLevels);

                foreach (var radarLevel in radarLevels)
                {
                    Bitmap newBmp = new Bitmap(radarLevels.FirstOrDefault().bmpRadar);
                    Graphics newGraphics = Graphics.FromImage(newBmp);

                    // apply blurred background levels to new image
                    newGraphics.CompositingMode = CompositingMode.SourceCopy;
                    newGraphics.DrawImage(backgroundBmp, 0, 0);
                    newGraphics.Save();
                    newGraphics.CompositingMode = CompositingMode.SourceOver;
                    newGraphics.DrawImage(radarLevel.bmpRadar, 0, 0);
                    newGraphics.Save();

                    radarLevelsToSaveList.Add(new RadarLevel(newBmp, radarLevel.levelHeight, radarLevel.bmpRawMasksByName));

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
                FlipImage(radarLevel.bmpRadar);

                SaveRadarLevel(radarLevel);
            }

            // save overview levels raw masks
            if (configurationValues.exportRawMasks)
            {
                foreach (var radarLevel in radarLevelsToSaveList)
                {
                    foreach (var bmpRawMask in radarLevel.bmpRawMasksByName)
                    {
                        FlipImage(bmpRawMask.Value);

                        SaveRadarLevelRawMask(radarLevel, bmpRawMask.Value, bmpRawMask.Key);
                    }
                }
            }

            // dispose
            foreach (var radarLevel in radarLevels.Concat(radarLevelsToSaveList))
            {
                DisposeGraphics(radarLevel.graphicsRadar);
                DisposeImage(radarLevel.bmpRadar);
            }

            Logger.LogMessage("Generating radars complete");
        }


        private static RadarLevel GenerateRadarLevel(LevelHeight levelHeight)
        {
            Logger.LogMessage(string.Concat("Generating radar level ", levelHeight.levelNum));

            Bitmap bmp = new Bitmap(overviewPositionValues.outputResolution, overviewPositionValues.outputResolution);
            var graphics = Graphics.FromImage(bmp);

            var bmpRawMaskByNameDictionary = GetNewBmpRawMaskByNameDictionary();

            var boundingBox = new BoundingBox(
                overviewPositionValues.brushVerticesPosMinX, overviewPositionValues.brushVerticesPosMaxX,
                overviewPositionValues.brushVerticesPosMinY, overviewPositionValues.brushVerticesPosMaxY,
                levelHeight.zMinForRadar, levelHeight.zMaxForRadar,
                levelHeight.zMinForRadarGradient, levelHeight.zMaxForRadarGradient
            );

            //graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.SmoothingMode = SmoothingMode.HighSpeed;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            graphics.SetClip(Rectangle.FromLTRB(0, 0, overviewPositionValues.outputResolution, overviewPositionValues.outputResolution));

            // get all brush sides and displacement sides to draw (brush volumes)
            var brushRemoveList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.brushesRemove, JercTypes.Remove, true);
            var displacementRemoveList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.displacementsRemove, JercTypes.Remove, true);

            var brushCoverList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.brushesCover, JercTypes.Cover);
            var displacementCoverList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.displacementsCover, JercTypes.Cover);

            var brushDoorList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.brushesDoor, JercTypes.Door);
            var displacementDoorList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.displacementsDoor, JercTypes.Door);

            var brushLadderList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.brushesLadder, JercTypes.Ladder);
            var displacementLadderList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.displacementsLadder, JercTypes.Ladder);

            var brushDangerList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.brushesDanger, JercTypes.Danger);
            var displacementDangerList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.displacementsDanger, JercTypes.Danger);

            var brushBuyzoneList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.brushesBuyzone, JercTypes.Buyzone);
            var displacementBuyzoneList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.displacementsBuyzone, JercTypes.Buyzone);

            var brushBombsiteAList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.brushesBombsiteA, JercTypes.BombsiteA);
            var displacementBombsiteAList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.displacementsBombsiteA, JercTypes.BombsiteA);

            var brushBombsiteBList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.brushesBombsiteB, JercTypes.BombsiteB);
            var displacementBombsiteBList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.displacementsBombsiteB, JercTypes.BombsiteB);

            var brushRescueZoneList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.brushesRescueZone, JercTypes.RescueZone);
            var displacementRescueZoneList = GetBrushVolumeListWithinLevelHeight(levelHeight, vmfRequiredData.displacementsRescueZone, JercTypes.RescueZone);

            // get all brush sides and displacement sides to draw (brush sides)
            var brushPathSideListById = GetBrushSideListWithinLevelHeight(levelHeight, vmfRequiredData.brushesPath, JercTypes.Path);
            var displacementPathSideListById = GetBrushSideListWithinLevelHeight(levelHeight, vmfRequiredData.displacementsPath, JercTypes.Path);

            var brushOverlapSideListById = GetBrushSideListWithinLevelHeight(levelHeight, vmfRequiredData.brushesOverlap, JercTypes.Overlap);
            var displacementOverlapSideListById = GetBrushSideListWithinLevelHeight(levelHeight, vmfRequiredData.displacementsOverlap, JercTypes.Overlap);

            // get all brushes and displacements to draw (brushes)
            var brushesToDrawPath = GetBrushesToDraw(boundingBox, brushPathSideListById);
            var displacementsToDrawPath = GetDisplacementsToDraw(boundingBox, vmfRequiredData.displacementsPath, displacementPathSideListById);

            var brushesToDrawOverlap = GetBrushesToDraw(boundingBox, brushOverlapSideListById);
            var displacementsToDrawOverlap = GetDisplacementsToDraw(boundingBox, vmfRequiredData.displacementsOverlap, displacementOverlapSideListById);


            var brushesToDrawCover = GetBrushesToDraw(boundingBox, brushCoverList);
            var displacementsToDrawCover = GetDisplacementsToDraw(boundingBox, vmfRequiredData.displacementsCover, displacementCoverList);

            var brushesToDrawDoor = GetBrushesToDraw(boundingBox, brushDoorList);
            var displacementsToDrawDoor = GetDisplacementsToDraw(boundingBox, vmfRequiredData.displacementsDoor, displacementDoorList);

            var brushesToDrawLadder = GetBrushesToDraw(boundingBox, brushLadderList);
            var displacementsToDrawLadder = GetDisplacementsToDraw(boundingBox, vmfRequiredData.displacementsLadder, displacementLadderList);

            var brushesToDrawDanger = GetBrushesToDraw(boundingBox, brushDangerList);
            var displacementsToDrawDanger = GetDisplacementsToDraw(boundingBox, vmfRequiredData.displacementsDanger, displacementDangerList);

            var brushesToDrawBuyzone = GetBrushesToDraw(boundingBox, brushBuyzoneList);
            var displacementsToDrawBuyzone = GetDisplacementsToDraw(boundingBox, vmfRequiredData.displacementsBuyzone, displacementBuyzoneList);

            var brushesToDrawBombsiteA = GetBrushesToDraw(boundingBox, brushBombsiteAList);
            var displacementsToDrawBombsiteA = GetDisplacementsToDraw(boundingBox, vmfRequiredData.displacementsBombsiteA, displacementBombsiteAList);

            var brushesToDrawBombsiteB = GetBrushesToDraw(boundingBox, brushBombsiteBList);
            var displacementsToDrawBombsiteB = GetDisplacementsToDraw(boundingBox, vmfRequiredData.displacementsBombsiteB, displacementBombsiteBList);

            var brushesToDrawRescueZone = GetBrushesToDraw(boundingBox, brushRescueZoneList);
            var displacementsToDrawRescueZone = GetDisplacementsToDraw(boundingBox, vmfRequiredData.displacementsRescueZone, displacementRescueZoneList);


            var allBrushesToDraw = new AllBrushesToDraw(brushesToDrawPath, brushesToDrawOverlap, brushesToDrawCover, brushesToDrawDoor, brushesToDrawLadder, brushesToDrawDanger, brushesToDrawBuyzone, brushesToDrawBombsiteA, brushesToDrawBombsiteB, brushesToDrawRescueZone);
            var allDisplacementsToDraw = new AllDisplacementsToDraw(displacementsToDrawPath, displacementsToDrawOverlap, displacementsToDrawCover, displacementsToDrawDoor, displacementsToDrawLadder, displacementsToDrawDanger, displacementsToDrawBuyzone, displacementsToDrawBombsiteA, displacementsToDrawBombsiteB, displacementsToDrawRescueZone);


            // get all entity sides to draw
            var entityBrushSideListById = GetEntityBrushSideListWithinLevelHeight(levelHeight);

            // get all brush entity sides to draw
            var brushEntityBrushSideListById = GetBrushEntityBrushSideListWithinLevelHeight(levelHeight);


            // add remove stuff first to set to graphics' clip
            AddRemoveRegion(bmp, graphics, brushRemoveList);
            AddRemoveRegion(bmp, graphics, displacementRemoveList);


            // draw everything
            DrawJercBrushOrPointEntities(graphics, levelHeight, bmpRawMaskByNameDictionary, brushEntityBrushSideListById, OverlayOrderNums.None, JercBoxOrderNums.First); // jerc_box
            DrawJercBrushOrPointEntities(graphics, levelHeight, bmpRawMaskByNameDictionary, entityBrushSideListById, OverlayOrderNums.First, JercBoxOrderNums.None); // overlays
            DrawBrushes(graphics, levelHeight, bmpRawMaskByNameDictionary, allBrushesToDraw, allDisplacementsToDraw, brushEntityBrushSideListById, entityBrushSideListById); // brushes and displacements (and jerc_box & overlays)
            DrawJercBrushOrPointEntities(graphics, levelHeight, bmpRawMaskByNameDictionary, brushEntityBrushSideListById, OverlayOrderNums.None, JercBoxOrderNums.AfterJERCBrushesAndDisplacements); // jerc_box
            DrawJercBrushOrPointEntities(graphics, levelHeight, bmpRawMaskByNameDictionary, entityBrushSideListById, OverlayOrderNums.AfterJERCBrushesAndDisplacements, JercBoxOrderNums.None); // overlays
            DrawBrushEntities(graphics, levelHeight, bmpRawMaskByNameDictionary, entityBrushSideListById); // brush entity (eg. func_buyzone)
            DrawJercBrushOrPointEntities(graphics, levelHeight, bmpRawMaskByNameDictionary, brushEntityBrushSideListById, OverlayOrderNums.None, JercBoxOrderNums.AfterBrushEntities); // jerc_box
            DrawBrushesTexturedEntities(graphics, levelHeight, bmpRawMaskByNameDictionary, allBrushesToDraw, allDisplacementsToDraw); // jerc brushes for entities (eg. Bombsite A Material)
            DrawJercBrushOrPointEntities(graphics, levelHeight, bmpRawMaskByNameDictionary, brushEntityBrushSideListById, OverlayOrderNums.None, JercBoxOrderNums.AfterJERCBrushesForEntities); // jerc_box


            graphics.Save();

            Logger.LogMessage(string.Concat("Generating radar level ", levelHeight.levelNum, " complete"));

            return new RadarLevel(bmp, levelHeight, bmpRawMaskByNameDictionary);
        }


        private static void DrawBrushes(Graphics graphics, LevelHeight levelHeight, Dictionary<string, Bitmap> bmpRawMaskByNameDictionary, AllBrushesToDraw allBrushesToDraw, AllDisplacementsToDraw allDisplacementsToDraw, Dictionary<int, List<EntityBrushSide>> brushEntityBrushSideListById, Dictionary<int, List<EntityBrushSide>> entityBrushSideListById)
        {
            var pathsOrdered = allBrushesToDraw.brushesToDrawPath.Concat(allDisplacementsToDraw.displacementsToDrawPath).OrderBy(x => x.zAxisAverage);
            var overlapsOrdered = allBrushesToDraw.brushesToDrawOverlap.Concat(allDisplacementsToDraw.displacementsToDrawOverlap).OrderBy(x => x.zAxisAverage);
            var coversOrdered = allBrushesToDraw.brushesToDrawCover.Concat(allDisplacementsToDraw.displacementsToDrawCover).OrderBy(x => x.zAxisAverage);
            var doorsOrdered = allBrushesToDraw.brushesToDrawDoor.Concat(allDisplacementsToDraw.displacementsToDrawDoor).OrderBy(x => x.zAxisAverage);
            var laddersOrdered = allBrushesToDraw.brushesToDrawLadder.Concat(allDisplacementsToDraw.displacementsToDrawLadder).OrderBy(x => x.zAxisAverage);
            var dangersOrdered = allBrushesToDraw.brushesToDrawDanger.Concat(allDisplacementsToDraw.displacementsToDrawDanger).OrderBy(x => x.zAxisAverage);

            var coversAndOverlapsAndDangersOrdered = overlapsOrdered.Concat(coversOrdered).Concat(dangersOrdered).OrderBy(x => x.zAxisAverage);

            // path and overlap brush stuff (for stroke)
            if (configurationValues.strokeAroundLayoutMaterials)
            {
                foreach (var brushToRender in pathsOrdered.Concat(overlapsOrdered).OrderBy(x => x.zAxisAverage))
                {
                    DrawStroke(graphics, brushToRender, Colours.ColourBrushesStroke(configurationValues.strokeColour));
                }
            }

            // path brush stuff
            Logger.LogMessage("Drawing paths...");
            foreach (var brushToRender in pathsOrdered)
            {
                DrawFilledPolygonGradient(graphics, brushToRender, true);
            }
            Logger.LogMessage("Finished drawing paths");

            // draw jerc_box brush entities that have the corresponding orderNum set
            DrawJercBrushOrPointEntities(graphics, levelHeight, bmpRawMaskByNameDictionary, brushEntityBrushSideListById, OverlayOrderNums.None, JercBoxOrderNums.BetweenPathAndOverlapBrushes); // jerc_box
            DrawJercBrushOrPointEntities(graphics, levelHeight, bmpRawMaskByNameDictionary, entityBrushSideListById, OverlayOrderNums.BetweenPathAndOverlapBrushes, JercBoxOrderNums.None); // overlays

            // cover and overlap, door, ladder, danger brush stuff
            foreach (var brushToRender in coversAndOverlapsAndDangersOrdered.Concat(doorsOrdered).Concat(laddersOrdered))
            {
                DrawFilledPolygonGradient(graphics, brushToRender, false);
            }

            // raw masks
            if (configurationValues.exportRawMasks)
            {
                // path
                using (Graphics graphicsRawMask = Graphics.FromImage(bmpRawMaskByNameDictionary["path"]))
                {
                    foreach (var brushEntitySide in pathsOrdered)
                    {
                        DrawFilledPolygonGradient(graphicsRawMask, brushEntitySide, false, levelHeight);
                    }

                    graphicsRawMask.Save();
                }

                // overlap
                using (Graphics graphicsRawMask = Graphics.FromImage(bmpRawMaskByNameDictionary["overlap"]))
                {
                    foreach (var brushEntitySide in overlapsOrdered)
                    {
                        DrawFilledPolygonGradient(graphicsRawMask, brushEntitySide, false, levelHeight);
                    }

                    graphicsRawMask.Save();
                }

                // cover
                using (Graphics graphicsRawMask = Graphics.FromImage(bmpRawMaskByNameDictionary["cover"]))
                {
                    foreach (var brushEntitySide in coversOrdered)
                    {
                        DrawFilledPolygonGradient(graphicsRawMask, brushEntitySide, false, levelHeight);
                    }

                    graphicsRawMask.Save();
                }

                // door
                using (Graphics graphicsRawMask = Graphics.FromImage(bmpRawMaskByNameDictionary["door"]))
                {
                    foreach (var brushEntitySide in doorsOrdered)
                    {
                        DrawFilledPolygonGradient(graphicsRawMask, brushEntitySide, false, levelHeight);
                    }

                    graphicsRawMask.Save();
                }

                // ladder
                using (Graphics graphicsRawMask = Graphics.FromImage(bmpRawMaskByNameDictionary["ladder"]))
                {
                    foreach (var brushEntitySide in laddersOrdered)
                    {
                        DrawFilledPolygonGradient(graphicsRawMask, brushEntitySide, false, levelHeight);
                    }

                    graphicsRawMask.Save();
                }

                // danger
                using (Graphics graphicsRawMask = Graphics.FromImage(bmpRawMaskByNameDictionary["danger"]))
                {
                    foreach (var brushEntitySide in dangersOrdered)
                    {
                        DrawFilledPolygonGradient(graphicsRawMask, brushEntitySide, false, levelHeight);
                    }

                    graphicsRawMask.Save();
                }
            }
        }


        private static void DrawBrushesTexturedEntities(Graphics graphics, LevelHeight levelHeight, Dictionary<string, Bitmap> bmpRawMaskByNameDictionary, AllBrushesToDraw allBrushesToDraw, AllDisplacementsToDraw allDisplacementsToDraw)
        {
            var allObjectiveAndBuyzoneBrushes = allBrushesToDraw.brushesToDrawBuyzone.Concat(allDisplacementsToDraw.displacementsToDrawBuyzone)
                .Concat(allBrushesToDraw.brushesToDrawBombsiteA).Concat(allDisplacementsToDraw.displacementsToDrawBombsiteA)
                .Concat(allBrushesToDraw.brushesToDrawBombsiteB).Concat(allDisplacementsToDraw.displacementsToDrawBombsiteB)
                .Concat(allBrushesToDraw.brushesToDrawRescueZone).Concat(allDisplacementsToDraw.displacementsToDrawRescueZone);

            // stroke
            if (configurationValues.strokeAroundBrushEntities)
            {
                foreach (var brushToRender in allObjectiveAndBuyzoneBrushes.OrderBy(x => x.zAxisAverage))
                {
                    Color colourStroke = brushToRender.jercType switch
                    {
                        JercTypes.Buyzone => Colours.ColourBuyzonesStroke(),
                        JercTypes.BombsiteA => Colours.ColourBombsitesStroke(),
                        JercTypes.BombsiteB => Colours.ColourBombsitesStroke(),
                        JercTypes.RescueZone => Colours.ColourRescueZonesStroke(),
                        JercTypes.None => throw new NotImplementedException(),
                        JercTypes.Remove => throw new NotImplementedException(),
                        JercTypes.Path => throw new NotImplementedException(),
                        JercTypes.Cover => throw new NotImplementedException(),
                        JercTypes.Overlap => throw new NotImplementedException(),
                        JercTypes.Door => throw new NotImplementedException(),
                        JercTypes.Ladder => throw new NotImplementedException(),
                        JercTypes.Danger => throw new NotImplementedException(),
                        JercTypes.Ignore => throw new NotImplementedException(),
                        JercTypes.Hostage => throw new NotImplementedException(),
                        JercTypes.TSpawn => throw new NotImplementedException(),
                        JercTypes.CTSpawn => throw new NotImplementedException(),
                        _ => throw new NotImplementedException()
                    };

                    DrawStroke(graphics, brushToRender, Colours.ColourBrushesStroke(colourStroke));
                }
            }

            // normal
            foreach (var brushToRender in allObjectiveAndBuyzoneBrushes.OrderBy(x => x.zAxisAverage))
            {
                DrawFilledPolygonGradient(graphics, brushToRender, false);
            }

            // raw masks
            if (configurationValues.exportRawMasks)
            {
                var brushEntitySides = allObjectiveAndBuyzoneBrushes.OrderBy(x => x.zAxisAverage).Where(x => new List<JercTypes>() { JercTypes.Buyzone, JercTypes.BombsiteA, JercTypes.BombsiteB, JercTypes.RescueZone }.Any(y => y == x.jercType));

                using (Graphics graphicsRawMask = Graphics.FromImage(bmpRawMaskByNameDictionary["buyzones_and_objectives"]))
                {
                    foreach (var brushEntitySide in brushEntitySides)
                    {
                        DrawFilledPolygonGradient(graphicsRawMask, brushEntitySide, false, levelHeight);
                    }

                    graphicsRawMask.Save();
                }
            }
        }


        private static void DrawBrushEntities(Graphics graphics, LevelHeight levelHeight, Dictionary<string, Bitmap> bmpRawMaskByNameDictionary, Dictionary<int, List<EntityBrushSide>> entityBrushSideListById)
        {
            // reset the clip so that entity brushes can render anywhere
            ////graphics.ResetClip();


            var entitySidesToDraw = GetBrushEntitiesToDraw(overviewPositionValues, entityBrushSideListById, OverlayOrderNums.None, JercBoxOrderNums.None); // does not give an overlayOrderNum or a jercBoxOrderNum value since overlay and jerc_box entities are drawn in DrawJercBrushOrPointEntities(), not here

            // normal
            foreach (var entitySideToRender in entitySidesToDraw)
            {
                DrawFilledPolygonGradient(graphics, entitySideToRender, true);
            }

            // raw masks
            if (configurationValues.exportRawMasks)
            {
                var entitySidesBuyzonesAndObjectives = entitySidesToDraw.Where(x => x.entityType == EntityTypes.Buyzone || x.entityType == EntityTypes.Bombsite || x.entityType == EntityTypes.RescueZone);

                using (Graphics graphicsRawMask = Graphics.FromImage(bmpRawMaskByNameDictionary["buyzones_and_objectives"]))
                {
                    foreach (var entitySide in entitySidesBuyzonesAndObjectives)
                    {
                        DrawFilledPolygonGradient(graphicsRawMask, entitySide, false, levelHeight);
                    }

                    graphicsRawMask.Save();
                }
            }

            // stroke
            if (configurationValues.strokeAroundEntities)
            {
                foreach (var entitySideToRender in entitySidesToDraw)
                {
                    Color colourStroke = Color.White;

                    switch (entitySideToRender.entityType)
                    {
                        case EntityTypes.Buyzone:
                            colourStroke = Colours.ColourBuyzonesStroke();
                            break;
                        case EntityTypes.Bombsite:
                            colourStroke = Colours.ColourBombsitesStroke();
                            break;
                        case EntityTypes.RescueZone:
                            colourStroke = Colours.ColourRescueZonesStroke();
                            break;
                    }

                    DrawStroke(graphics, entitySideToRender, colourStroke);
                }
            }
        }


        private static void DrawJercBrushOrPointEntities(Graphics graphics, LevelHeight levelHeight, Dictionary<string, Bitmap> bmpRawMaskByNameDictionary, Dictionary<int, List<EntityBrushSide>> brushSideListById, OverlayOrderNums overlayOrderNum, JercBoxOrderNums jercBoxOrderNum)
        {
            var brushEntitySidesToDraw = GetBrushEntitiesToDraw(overviewPositionValues, brushSideListById, overlayOrderNum, jercBoxOrderNum);

            // normal
            foreach (var brushEntitySideToRender in brushEntitySidesToDraw)
            {
                DrawFilledPolygonGradient(graphics, brushEntitySideToRender, true);
            }

            // raw masks
            if (configurationValues.exportRawMasks)
            {
                var entitySidesOverlays = brushEntitySidesToDraw.Where(x => x.entityType == EntityTypes.Overlays);
                var entitySidesJercBox = brushEntitySidesToDraw.Where(x => x.entityType == EntityTypes.JercBox);

                using (Graphics graphicsRawMask = Graphics.FromImage(bmpRawMaskByNameDictionary["overlays"]))
                {
                    foreach (var entitySide in entitySidesOverlays)
                    {
                        DrawFilledPolygonGradient(graphicsRawMask, entitySide, false, levelHeight);

                        if (entitySide.strokeWidth > 0 && entitySide.colourStroke != null && entitySide.colourStroke.HasValue && entitySide.colourStroke.Value.A > 0)
                            DrawStroke(graphicsRawMask, entitySide, (Color)entitySide.colourStroke, entitySide.strokeWidth);
                    }

                    graphicsRawMask.Save();
                }

                using (Graphics graphicsRawMask = Graphics.FromImage(bmpRawMaskByNameDictionary["jerc_box"]))
                {
                    foreach (var entitySide in entitySidesJercBox)
                    {
                        DrawFilledPolygonGradient(graphicsRawMask, entitySide, false, levelHeight);

                        if (entitySide.strokeWidth > 0 && entitySide.colourStroke != null && entitySide.colourStroke.HasValue && entitySide.colourStroke.Value.A > 0)
                            DrawStroke(graphicsRawMask, entitySide, (Color)entitySide.colourStroke, entitySide.strokeWidth);
                    }

                    graphicsRawMask.Save();
                }
            }

            // stroke
            if ((configurationValues.strokeAroundBrushEntities && jercBoxOrderNum != JercBoxOrderNums.None) ||
                (configurationValues.strokeAroundOverlays && overlayOrderNum != OverlayOrderNums.None)
            )
            {
                foreach (var brushEntitySideToRender in brushEntitySidesToDraw)
                {
                    Color colour = Color.White;

                    switch (brushEntitySideToRender.entityType)
                    {
                        case EntityTypes.JercBox:
                        case EntityTypes.Overlays:
                            colour = (Color)brushEntitySideToRender.colourStroke;
                            break;
                    }

                    DrawStroke(graphics, brushEntitySideToRender, colour, brushEntitySideToRender.strokeWidth);
                }
            }
        }


        private static Dictionary<string, Bitmap> GetNewBmpRawMaskByNameDictionary()
        {
            return new Dictionary<string, Bitmap>()
            {
                { "path", new Bitmap(overviewPositionValues.outputResolution, overviewPositionValues.outputResolution) },
                { "cover", new Bitmap(overviewPositionValues.outputResolution, overviewPositionValues.outputResolution) },
                { "overlap", new Bitmap(overviewPositionValues.outputResolution, overviewPositionValues.outputResolution) },
                { "door", new Bitmap(overviewPositionValues.outputResolution, overviewPositionValues.outputResolution) },
                { "ladder", new Bitmap(overviewPositionValues.outputResolution, overviewPositionValues.outputResolution) },
                { "danger", new Bitmap(overviewPositionValues.outputResolution, overviewPositionValues.outputResolution) },
                { "buyzones_and_objectives", new Bitmap(overviewPositionValues.outputResolution, overviewPositionValues.outputResolution) },
                { "overlays", new Bitmap(overviewPositionValues.outputResolution, overviewPositionValues.outputResolution) },
                { "jerc_box", new Bitmap(overviewPositionValues.outputResolution, overviewPositionValues.outputResolution) },
            };
        }


        private static void RotateObjectBeforeDrawingIfSpecified(ObjectToDraw objectToDraw)
        {
            var rotated90 = false;
            var rotated180 = false;
            var rotated270 = false;

            if ((bool)objectToDraw.needsRotating90)
            {
                rotated90 = true;

                MoveAndRotateObjectToDrawAroundOrigin(objectToDraw, 90);

                /*var firstVertColour = objectToDraw.verticesToDraw[0].colour;
                objectToDraw.verticesToDraw[0].colour = objectToDraw.verticesToDraw[1].colour;
                objectToDraw.verticesToDraw[1].colour = objectToDraw.verticesToDraw[2].colour;
                objectToDraw.verticesToDraw[2].colour = objectToDraw.verticesToDraw[3].colour;
                objectToDraw.verticesToDraw[3].colour = firstVertColour;*/
            }

            if ((bool)objectToDraw.needsRotating180)
            {
                if (rotated90)
                {
                    Logger.LogImportantWarning($"Skipped rotating brush {objectToDraw.brushId} 180 degrees as it has already rotated 90 degrees clockwise.");
                }
                else
                {
                    rotated180 = true;

                    MoveAndRotateObjectToDrawAroundOrigin(objectToDraw, 180);

                    /*var firstVertColour = objectToDraw.verticesToDraw[0].colour;
                    objectToDraw.verticesToDraw[0].colour = objectToDraw.verticesToDraw[2].colour;
                    objectToDraw.verticesToDraw[2].colour = firstVertColour;

                    var secondVertColour = objectToDraw.verticesToDraw[1].colour;
                    objectToDraw.verticesToDraw[1].colour = objectToDraw.verticesToDraw[3].colour;
                    objectToDraw.verticesToDraw[3].colour = secondVertColour;*/
                }
            }

            if ((bool)objectToDraw.needsRotating270)
            {
                if (rotated90)
                {
                    Logger.LogImportantWarning($"Skipped rotating brush {objectToDraw.brushId} 90 degrees anti-clockwise as it has already rotated 90 degrees clockwise.");
                }
                else if (rotated180)
                {
                    Logger.LogImportantWarning($"Skipped rotating brush {objectToDraw.brushId} 90 degrees anti-clockwise as it has already rotated 180 degrees.");
                }
                else
                {
                    rotated270 = true;

                    MoveAndRotateObjectToDrawAroundOrigin(objectToDraw, 270);

                    /*var firstVertColour = objectToDraw.verticesToDraw[0].colour;
                    objectToDraw.verticesToDraw[0].colour = objectToDraw.verticesToDraw[3].colour;
                    objectToDraw.verticesToDraw[3].colour = objectToDraw.verticesToDraw[2].colour;
                    objectToDraw.verticesToDraw[2].colour = objectToDraw.verticesToDraw[1].colour;
                    objectToDraw.verticesToDraw[1].colour = firstVertColour;*/
                }
            }
        }


        private static void DrawStroke(Graphics graphics, ObjectToDraw objectToDraw, Color colourStroke, int? strokeWidthOverride = null)
        {
            RotateObjectBeforeDrawingIfSpecified(objectToDraw); // jerc_disp_rotation

            var strokeSolidBrush = new SolidBrush(Color.Transparent);
            var strokePen = new Pen(colourStroke);
            strokePen.Width *= strokeWidthOverride == null ? configurationValues.strokeWidth : (int)strokeWidthOverride;

            DrawFilledPolygonObjectBrushes(graphics, strokeSolidBrush, strokePen, objectToDraw.verticesToDraw.Select(x => new Point((int)x.vertices.x, (int)x.vertices.y)).ToArray());

            // dispose
            strokeSolidBrush?.Dispose();
            strokePen?.Dispose();
        }


        private static Bitmap GetBackgroundToRadarLevels(List<RadarLevel> radarLevels)
        {
            var allGraphics = radarLevels.Select(x => x.graphicsRadar);

            Bitmap backgroundBmp = new Bitmap(radarLevels.FirstOrDefault().bmpRadar);
            Graphics backgroundGraphics = Graphics.FromImage(backgroundBmp);
            backgroundGraphics.ResetClip();
            backgroundGraphics.Clear(Color.Transparent);

            // draw all levels on top of one another, bottom first
            foreach (var radarLevel in radarLevels)
            {
                //radarLevel.graphics.ResetClip();
                backgroundGraphics.CompositingMode = CompositingMode.SourceOver;
                backgroundGraphics.DrawImage(radarLevel.bmpRadar, 0, 0);
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
            backgroundGraphics.FillRectangle(new SolidBrush(Color.FromArgb(configurationValues.levelBackgroundDarkenAlpha, 0, 0, 0)), rectangle);

            // reapply the transparent pixels
            foreach (var pixel in transparentPixelLocations)
            {
                backgroundBmp.SetPixel((int)pixel.x, (int)pixel.y, Color.Transparent);
            }

            // save graphics
            backgroundGraphics.Save();

            // blur and save background image
            backgroundBmp = new Bitmap(backgroundBmp, Sizes.FinalOutputImageResolution, Sizes.FinalOutputImageResolution);
            var imageFactoryBlurred = imageProcessorExtender.GetBlurredImage(backgroundBmp, configurationValues.levelBackgroundBlurAmount); //blur the image
            backgroundBmp = new Bitmap(backgroundBmp, overviewPositionValues.outputResolution, overviewPositionValues.outputResolution);


            if (configurationValues.exportBackgroundLevelsImage)
            {
                if (!configurationValues.onlyOutputToAlternatePath)
                {
                    imageFactoryBlurred.Save(outputImageBackgroundLevelsFilepath);
                }

                CreateDirectoryOfFileIfDoesntExist(alternateExtrasOutputFilepathPrefix);

                if (!string.IsNullOrWhiteSpace(alternateExtrasOutputFilepathPrefix))
                {
                    var outputImageFilepath = string.Concat(alternateExtrasOutputFilepathPrefix, "_background_levels.png");
                    imageFactoryBlurred.Save(outputImageFilepath);
                }
            }

            imageFactoryBlurred.Dispose();

            return backgroundBmp;
        }


        private static void SaveRadarLevel(RadarLevel radarLevel)
        {
            radarLevel.bmpRadar = new Bitmap(radarLevel.bmpRadar, Sizes.FinalOutputImageResolution, Sizes.FinalOutputImageResolution);

            if (!string.IsNullOrWhiteSpace(configurationValues.backgroundFilename))
                radarLevel.bmpRadar = AddBackgroundImage(radarLevel);

            var radarLevelString = GetRadarLevelString(radarLevel);

            if (configurationValues.exportDds || configurationValues.exportPng)
            {
                if (!configurationValues.onlyOutputToAlternatePath)
                {
                    // always output resource/overviews/ radar image, used on loading screens
                    var outputImageFilepathRadar = string.Concat(overviewsOutputFilepathPrefix, radarLevelString, "_radar");
                    SaveImage(outputImageFilepathRadar, radarLevel.bmpRadar);

                    if (configurationValues.overviewGamemodeType == 1)
                    {
                        if (configurationValues.dangerZoneUses == 0 || configurationValues.dangerZoneUses == 1)
                            SaveImage(dzTabletOutputFilepathPrefix, radarLevel.bmpRadar, dangerZoneSpecificFile: DangerZoneSpecificFiles.TabletImage);
                        if (configurationValues.dangerZoneUses == 0 || configurationValues.dangerZoneUses == 2)
                            SaveImage(dzSpawnselectOutputFilepathPrefix, radarLevel.bmpRadar, dangerZoneSpecificFile: DangerZoneSpecificFiles.SpawnselectImage);

                        if (configurationValues.workshopId > 0) // set to 0 by default
                        {
                            if (configurationValues.dangerZoneUses == 0 || configurationValues.dangerZoneUses == 1)
                            {
                                CreateDirectoryOfFileIfDoesntExist(dzTabletWorkshopOutputFilepathPrefix);
                                SaveImage(dzTabletWorkshopOutputFilepathPrefix, radarLevel.bmpRadar, dangerZoneSpecificFile: DangerZoneSpecificFiles.TabletImage);
                            }
                            if (configurationValues.dangerZoneUses == 0 || configurationValues.dangerZoneUses == 2)
                            {
                                CreateDirectoryOfFileIfDoesntExist(dzSpawnselectWorkshopOutputFilepathPrefix);
                                SaveImage(dzSpawnselectWorkshopOutputFilepathPrefix, radarLevel.bmpRadar, dangerZoneSpecificFile: DangerZoneSpecificFiles.SpawnselectImage);
                            }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(configurationValues.alternateOutputPath))
                {
                    // always output resource/overviews/ radar image, used on loading screens
                    var outputImageFilepathRadar = string.Concat(configurationValues.alternateOutputPath, @"resource\overviews\", mapName, radarLevelString, "_radar");
                    CreateDirectoryOfFileIfDoesntExist(outputImageFilepathRadar);
                    SaveImage(outputImageFilepathRadar, radarLevel.bmpRadar);

                    if (configurationValues.overviewGamemodeType == 1)
                    {
                        if (configurationValues.dangerZoneUses == 0 || configurationValues.dangerZoneUses == 1)
                        {
                            var outputImageFilepath = string.Concat(configurationValues.alternateOutputPath, @"materials\models\weapons\v_models\tablet\tablet_radar_", mapName);
                            CreateDirectoryOfFileIfDoesntExist(outputImageFilepath);
                            SaveImage(outputImageFilepath, radarLevel.bmpRadar, dangerZoneSpecificFile: DangerZoneSpecificFiles.TabletImage);
                        }
                        if (configurationValues.dangerZoneUses == 0 || configurationValues.dangerZoneUses == 2)
                        {
                            var outputImageFilepath = string.Concat(configurationValues.alternateOutputPath, @"materials\panorama\images\survival\spawnselect\map_", mapName);
                            CreateDirectoryOfFileIfDoesntExist(outputImageFilepath);
                            SaveImage(outputImageFilepath, radarLevel.bmpRadar, dangerZoneSpecificFile: DangerZoneSpecificFiles.SpawnselectImage);
                        }

                        if (configurationValues.workshopId > 0) // set to 0 by default
                        {
                            if (configurationValues.dangerZoneUses == 0 || configurationValues.dangerZoneUses == 1)
                            {
                                var outputImageFilepath = string.Concat(configurationValues.alternateOutputPath, @"materials\models\weapons\v_models\tablet\tablet_radar_workshop\", configurationValues.workshopId, @"\", mapName);
                                CreateDirectoryOfFileIfDoesntExist(outputImageFilepath);
                                SaveImage(outputImageFilepath, radarLevel.bmpRadar, dangerZoneSpecificFile: DangerZoneSpecificFiles.TabletImage);
                            }
                            if (configurationValues.dangerZoneUses == 0 || configurationValues.dangerZoneUses == 2)
                            {
                                var outputImageFilepath = string.Concat(configurationValues.alternateOutputPath, @"materials\panorama\images\survival\spawnselect\map_workshop\", configurationValues.workshopId, @"\", mapName);
                                CreateDirectoryOfFileIfDoesntExist(outputImageFilepath);
                                SaveImage(outputImageFilepath, radarLevel.bmpRadar, dangerZoneSpecificFile: DangerZoneSpecificFiles.SpawnselectImage);
                            }
                        }
                    }
                }
            }
        }


        private static void SaveRadarLevelRawMask(RadarLevel radarLevel, Bitmap bmpRawMask, string rawMaskType)
        {
            bmpRawMask = new Bitmap(bmpRawMask, Sizes.FinalOutputImageResolution, Sizes.FinalOutputImageResolution);

            var radarLevelString = GetRadarLevelString(radarLevel);


            // set inner jerc_extras output folder name
            var exportJercExtrasForOverview = true;
            var exportJercExtrasForTablet = false;
            var exportJercExtrasForSpawnSelect = false;

            if (configurationValues.overviewGamemodeType == 1)
            {
                switch (configurationValues.dangerZoneUses)
                {
                    case (int)DangerZoneSpecificFiles.None: // Both tablet and spawn select
                        exportJercExtrasForTablet = true;
                        exportJercExtrasForSpawnSelect = true;
                        break;
                    case (int)DangerZoneSpecificFiles.TabletImage:
                        exportJercExtrasForTablet = true;
                        break;
                    case (int)DangerZoneSpecificFiles.SpawnselectImage:
                        exportJercExtrasForSpawnSelect = true;
                        break;
                }
            }


            if (!configurationValues.onlyOutputToAlternatePath)
            {
                if (exportJercExtrasForOverview)
                {
                    if (configurationValues.overviewGamemodeType == 0) // standard gamemode
                    {
                        var outputImageFilepath = string.Concat(extrasOutputFilepathPrefixOverview, radarLevelString, "_radar_", rawMaskType, "_mask");
                        SaveImage(outputImageFilepath, bmpRawMask, forcePngOnly: true);
                    }
                    else if (configurationValues.overviewGamemodeType == 1) // danger zone gamemode
                    {
                        var outputImageFilepath = string.Concat(extrasOutputFilepathPrefixOverview, radarLevelString, "_radar_", rawMaskType, "_mask");
                        SaveImage(outputImageFilepath, bmpRawMask, forcePngOnly: true, dangerZoneSpecificFile: DangerZoneSpecificFiles.SpawnselectImage); // uses spawnselect to make it the smaller size
                    }
                }

                if (exportJercExtrasForTablet)
                {
                    var outputImageFilepath = string.Concat(extrasOutputFilepathPrefixTablet, radarLevelString, "_radar_", rawMaskType, "_mask");
                    SaveImage(outputImageFilepath, bmpRawMask, forcePngOnly: true, dangerZoneSpecificFile: DangerZoneSpecificFiles.TabletImage);
                }

                if (exportJercExtrasForSpawnSelect)
                {
                    var outputImageFilepath = string.Concat(extrasOutputFilepathPrefixSpawnSelect, radarLevelString, "_radar_", rawMaskType, "_mask");
                    SaveImage(outputImageFilepath, bmpRawMask, forcePngOnly: true, dangerZoneSpecificFile: DangerZoneSpecificFiles.SpawnselectImage);
                }
            }

            if (!string.IsNullOrWhiteSpace(alternateExtrasOutputFilepathPrefix))
            {
                CreateDirectoryOfFileIfDoesntExist(alternateExtrasOutputFilepathPrefix);

                if (exportJercExtrasForOverview)
                {
                    CreateDirectoryOfFileIfDoesntExist(alternateExtrasOutputFilepathPrefixOverview);

                    if (configurationValues.overviewGamemodeType == 0) // standard gamemode
                    {
                        var outputImageFilepath = string.Concat(alternateExtrasOutputFilepathPrefixOverview, radarLevelString, "_radar_", rawMaskType, "_mask");
                        SaveImage(outputImageFilepath, bmpRawMask, forcePngOnly: true);
                    }
                    else if (configurationValues.overviewGamemodeType == 1) // danger zone gamemode
                    {
                        var outputImageFilepath = string.Concat(alternateExtrasOutputFilepathPrefixOverview, radarLevelString, "_radar_", rawMaskType, "_mask");
                        SaveImage(outputImageFilepath, bmpRawMask, forcePngOnly: true, dangerZoneSpecificFile: DangerZoneSpecificFiles.SpawnselectImage); // uses spawnselect to make it the smaller size
                    }
                }

                if (exportJercExtrasForTablet)
                {
                    CreateDirectoryOfFileIfDoesntExist(alternateExtrasOutputFilepathPrefixTablet);

                    var outputImageFilepath = string.Concat(alternateExtrasOutputFilepathPrefixTablet, radarLevelString, "_radar_", rawMaskType, "_mask");
                    SaveImage(outputImageFilepath, bmpRawMask, forcePngOnly: true, dangerZoneSpecificFile: DangerZoneSpecificFiles.TabletImage);
                }

                if (exportJercExtrasForSpawnSelect)
                {
                    CreateDirectoryOfFileIfDoesntExist(alternateExtrasOutputFilepathPrefixSpawnSelect);

                    var outputImageFilepath = string.Concat(alternateExtrasOutputFilepathPrefixSpawnSelect, radarLevelString, "_radar_", rawMaskType, "_mask");
                    SaveImage(outputImageFilepath, bmpRawMask, forcePngOnly: true, dangerZoneSpecificFile: DangerZoneSpecificFiles.SpawnselectImage);
                }
            }
        }


        private static string GetRadarLevelString(RadarLevel radarLevel)
        {
            return radarLevel.levelHeight.levelName.ToLower() == "default" ? string.Empty : string.Concat("_", radarLevel.levelHeight.levelName.ToLower());
        }


        private static Bitmap AddBackgroundImage(RadarLevel radarLevel)
        {
            var backgroundImageFilepath = string.Concat(backgroundImagesDirectory, configurationValues.backgroundFilename, ".bmp");

            if (!File.Exists(backgroundImageFilepath))
            {
                Logger.LogImportantWarning("Background image does not exist");
                return radarLevel.bmpRadar;
            }

            Bitmap newBmp = new Bitmap(radarLevel.bmpRadar);
            Graphics newGraphics = Graphics.FromImage(newBmp);

            Bitmap backgroundBmp = new Bitmap(backgroundImageFilepath);
            backgroundBmp = new Bitmap(backgroundBmp, Sizes.FinalOutputImageResolution, Sizes.FinalOutputImageResolution);
            Graphics backgroundGraphics = Graphics.FromImage(backgroundBmp);

            newGraphics.CompositingMode = CompositingMode.SourceCopy;
            newGraphics.DrawImage(backgroundBmp, 0, 0);
            newGraphics.Save();
            newGraphics.CompositingMode = CompositingMode.SourceOver;
            newGraphics.DrawImage(radarLevel.bmpRadar, 0, 0);
            newGraphics.Save();

            // dispose
            DisposeGraphics(backgroundGraphics);
            DisposeImage(backgroundBmp);

            DisposeGraphics(radarLevel.graphicsRadar);
            DisposeImage(radarLevel.bmpRadar);

            return newBmp;
        }


        private static Dictionary<int, List<EntityBrushSide>> GetEntityVerticesListById()
        {
            var entityBrushSideListById = new Dictionary<int, List<EntityBrushSide>>();

            var entityBuyzoneVerticesListById = GetEntityBrushSideList(vmfRequiredData.entitiesSidesByEntityBuyzoneId, EntityTypes.Buyzone);
            var entityBombsiteVerticesListById = GetEntityBrushSideList(vmfRequiredData.entitiesSidesByEntityBombsiteId, EntityTypes.Bombsite);
            var entityRescueZoneVerticesListById = GetEntityBrushSideList(vmfRequiredData.entitiesSidesByEntityRescueZoneId, EntityTypes.RescueZone);
            var entityOverlayVerticesListById = GetEntityBrushSideList(vmfRequiredData.entitiesSidesByEntityOverlayId, EntityTypes.Overlays);

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
            if (entityOverlayVerticesListById != null && entityOverlayVerticesListById.Any())
            {
                foreach (var list in entityOverlayVerticesListById)
                {
                    entityBrushSideListById.Add(list.Key, list.Value);
                }
            }

            return entityBrushSideListById;
        }


        private static Dictionary<int, List<EntityBrushSide>> GetBrushEntityVerticesListById()
        {
            /**** use this code if another new JERC brush entity is added in future ****/

            /*
            var brushEntityBrushSideListById = new Dictionary<int, List<EntityBrushSide>>();

            var brushEntityJercBoxVerticesListById = GetBrushEntityBrushSideList(vmfRequiredData.entitiesSidesByEntityJercBoxId, EntityTypes.JercBox);

            if (brushEntityJercBoxVerticesListById != null && brushEntityJercBoxVerticesListById.Any())
            {
                foreach (var list in brushEntityJercBoxVerticesListById)
                {
                    /////////////////////////////////////////////////////////// TODO: This is a dreadful way to handle different instaces containing the same IDs. They should be handled sepearately somehow, NOT added together
                    if (brushEntityBrushSideListById.ContainsKey(list.Key))
                        brushEntityBrushSideListById[list.Key].AddRange(list.Value);
                    else
                        brushEntityBrushSideListById.Add(list.Key, list.Value);
                }
            }

            return brushEntityBrushSideListById;
            */

            return GetBrushEntityBrushSideList(vmfRequiredData.entitiesSidesByEntityJercBoxId, EntityTypes.JercBox);
        }


        private static List<BrushVolume> GetBrushVolumeListWithinLevelHeight(LevelHeight levelHeight, List<Models.Brush> brushList, JercTypes jercType, bool allowInMultipleLevels = false)
        {
            var brushVolumeList = new List<BrushVolume>();

            foreach (var brush in brushList)
            {
                var brushVolumeUnchecked = GetBrushVolume(brush, jercType);

                var brushSides = brushVolumeUnchecked.brushSides.SelectMany(a => a.vertices).ToList();

                if (allowInMultipleLevels &&
                    brushSides.Any(a => a.z >= levelHeight.zMinForRadar) &&
                    brushSides.Any(a => a.z <= levelHeight.zMaxForRadar))
                {
                    brushVolumeList.Add(brushVolumeUnchecked);
                }
                else if (!allowInMultipleLevels &&
                         brushSides.All(a => a.z >= levelHeight.zMinForRadar) &&
                         brushSides.All(a => a.z <= levelHeight.zMaxForRadar) // may appear on more than 1 level if their brushes span across level dividers or touch edges ?
                )
                {
                    brushVolumeList.Add(brushVolumeUnchecked);
                }
            }

            return brushVolumeList;
        }


        private static Dictionary<int, List<BrushSide>> GetBrushSideListWithinLevelHeight(LevelHeight levelHeight, List<Models.Brush> brushList, JercTypes jercType)
        {
            var brushSideListById = new Dictionary<int, List<BrushSide>>();

            foreach (var brush in brushList)
            {
                var brushSideListUnfiltered = GetBrushSideList(brush.id, brush.side, jercType);

                // may appear on more than 1 level if their brushes span across level dividers or touch edges ?
                if (brushSideListUnfiltered.Any(x =>
                    x.vertices.All(y => y.z >= levelHeight.zMinForRadar) &&
                    x.vertices.All(y => y.z <= levelHeight.zMaxForRadar)
                ))
                {
                    /////////////////////////////////////////////////////////// TODO: This is a dreadful way to handle different instaces containing the same IDs. They should be handled sepearately somehow, NOT added together
                    if (brushSideListById.ContainsKey(brush.id))
                        brushSideListById[brush.id].AddRange(brushSideListUnfiltered);
                    else
                        brushSideListById.Add(brush.id, brushSideListUnfiltered);
                }
            }

            return brushSideListById;
        }


        private static Dictionary<int, List<EntityBrushSide>> GetEntityBrushSideListWithinLevelHeight(LevelHeight levelHeight)
        {
            var entityBrushSideListById = new Dictionary<int, List<EntityBrushSide>>();

            var entityBrushSideListByIdUnfiltered = GetEntityVerticesListById();

            foreach (var entityBrushSideById in entityBrushSideListByIdUnfiltered)
            {
                // may appear on more than 1 level if their brushes span across level dividers or touch edges ?
                if (entityBrushSideById.Value.Any(x =>
                    x.vertices.All(y => y.z >= levelHeight.zMinForRadar) &&
                    x.vertices.All(y => y.z <= levelHeight.zMaxForRadar)
                ))
                {
                    /////////////////////////////////////////////////////////// TODO: This is a dreadful way to handle different instaces containing the same IDs. They should be handled sepearately somehow, NOT added together
                    if (entityBrushSideListById.ContainsKey(entityBrushSideById.Key))
                        entityBrushSideListById[entityBrushSideById.Key].AddRange(entityBrushSideById.Value);
                    else
                        entityBrushSideListById.Add(entityBrushSideById.Key, entityBrushSideById.Value);
                }
            }

            return entityBrushSideListById;
        }


        private static Dictionary<int, List<EntityBrushSide>> GetBrushEntityBrushSideListWithinLevelHeight(LevelHeight levelHeight)
        {
            var brushEntityBrushSideListById = new Dictionary<int, List<EntityBrushSide>>();
            var brushEntityBrushSideListByIdUnfiltered = GetBrushEntityVerticesListById();

            foreach (var brushEntityBrushSideById in brushEntityBrushSideListByIdUnfiltered)
            {
                // may appear on more than 1 level if their brushes span across level dividers or touch edges ?
                if (brushEntityBrushSideById.Value.Any(x =>
                    x.vertices.All(y => y.z >= levelHeight.zMinForRadar) &&
                    x.vertices.All(y => y.z <= levelHeight.zMaxForRadar)
                ))
                {
                    /////////////////////////////////////////////////////////// TODO: This is a dreadful way to handle different instaces containing the same IDs. They should be handled sepearately somehow, NOT added together
                    if (brushEntityBrushSideListById.ContainsKey(brushEntityBrushSideById.Key))
                        brushEntityBrushSideListById[brushEntityBrushSideById.Key].AddRange(brushEntityBrushSideById.Value);
                    else
                        brushEntityBrushSideListById.Add(brushEntityBrushSideById.Key, brushEntityBrushSideById.Value);
                }
            }

            return brushEntityBrushSideListById;
        }


        private static BrushVolume GetBrushVolume(Models.Brush brush, JercTypes jercType)
        {
            var brushVolume = new BrushVolume(brush.id);

            foreach (var brushSide in brush.side.ToList())
            {
                // calculate vertices_plus for every brush side for vanilla hammer vmfs, as hammer++ adds vertices itself when saving a vmf
                if (GameConfigurationValues.isVanillaHammer == true)
                {
                    VanillaHammerVmfFixer.CalculateVerticesPlusForAllBrushes(new List<Models.Brush>() { brush });
                }

                var brushSideNew = new BrushSide(brushSide.id, brush.id);
                foreach (var vertices in brushSide.vertices_plus.ToList())
                {
                    brushSideNew.vertices.Add(new Vertices(vertices.x / Sizes.SizeReductionMultiplier, vertices.y / Sizes.SizeReductionMultiplier, (float)vertices.z));
                }

                brushSideNew.jercType = jercType;

                // add displacement stuff if the brush is a displacement
                if (brushSide.isDisplacement)
                {
                    brushSideNew.displacementStuff = new DisplacementStuff(configurationValues, brushSide.dispinfo, brushSideNew.vertices); //// brushSide.vertices_plus ??
                }

                brushVolume.brushSides.Add(brushSideNew);
            }

            brushVolume.jercType = jercType;

            return brushVolume;
        }


        private static List<BrushSide> GetBrushSideList(int brushId, List<Side> sideList, JercTypes jercType)
        {
            var brushSideList = new List<BrushSide>();

            foreach (var side in sideList)
            {
                var brushSideNew = new BrushSide(side.id, brushId); //// side.brushId
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    brushSideNew.vertices.Add(new Vertices(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier, (float)vert.z));
                }

                brushSideNew.jercType = jercType;

                // add displacement stuff if the brush is a displacement
                if (side.isDisplacement)
                {
                    brushSideNew.displacementStuff = new DisplacementStuff(configurationValues, side.dispinfo, brushSideNew.vertices); //// brushSide.vertices_plus ??
                }

                brushSideList.Add(brushSideNew);
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
                    var entityBrushSide = new EntityBrushSide(side.id, side.brushId);

                    if (entityType == EntityTypes.Overlays)
                    {
                        entityBrushSide.entityType = entityType;
                        entityBrushSide.orderNum = vmfRequiredData.overlayByEntityOverlayId[entitySides.Key].orderNum;
                        entityBrushSide.rendercolor = vmfRequiredData.overlayByEntityOverlayId[entitySides.Key].rendercolor;
                        entityBrushSide.colourStroke = vmfRequiredData.overlayByEntityOverlayId[entitySides.Key].colourStroke;
                        entityBrushSide.strokeWidth = vmfRequiredData.overlayByEntityOverlayId[entitySides.Key].strokeWidth;
                        entityBrushSide.material = side.material;
                    }

                    for (int i = 0; i < side.vertices_plus.Count(); i++)
                    {
                        var vert = side.vertices_plus[i];

                        entityBrushSide.vertices.Add(new Vertices(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier, (float)vert.z));
                    }

                    entityBrushSide.entityType = entityType;
                    entityBrushSide.material = side.material;

                    if (entityBrushSideListById.ContainsKey(entitySides.Key))
                        entityBrushSideListById[entitySides.Key].Add(entityBrushSide);
                    else
                        entityBrushSideListById.Add(entitySides.Key, new List<EntityBrushSide>() { entityBrushSide });
                }
            }

            return entityBrushSideListById;
        }


        private static Dictionary<int, List<EntityBrushSide>> GetBrushEntityBrushSideList(Dictionary<int, List<Side>> sideListByEntityIdDictionary, EntityTypes entityType)
        {
            var entityBrushSideListById = new Dictionary<int, List<EntityBrushSide>>();

            foreach (var entitySides in sideListByEntityIdDictionary)
            {
                foreach (var side in entitySides.Value)
                {
                    var entityBrushSide = new EntityBrushSide(side.id, side.brushId)
                    {
                        entityType = entityType,
                        orderNum = vmfRequiredData.jercBoxByEntityJercBoxId[entitySides.Key].orderNum,
                        rendercolor = vmfRequiredData.jercBoxByEntityJercBoxId[entitySides.Key].rendercolor,
                        colourStroke = vmfRequiredData.jercBoxByEntityJercBoxId[entitySides.Key].colourStroke,
                        strokeWidth = vmfRequiredData.jercBoxByEntityJercBoxId[entitySides.Key].strokeWidth,
                        material = side.material
                    };

                    for (int i = 0; i < side.vertices_plus.Count(); i++)
                    {
                        var vert = side.vertices_plus[i];

                        entityBrushSide.vertices.Add(new Vertices(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier, (float)vert.z));
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
            var verticesNew = new Vertices(0, 0);
            verticesNew.x = (vertices.x - overviewPositionValues.brushVerticesPosMinX + overviewPositionValues.brushVerticesOffsetX) / Sizes.SizeReductionMultiplier;
            verticesNew.y = (vertices.y - overviewPositionValues.brushVerticesPosMinY + overviewPositionValues.brushVerticesOffsetY) / Sizes.SizeReductionMultiplier;
            verticesNew.z = vertices.z;

            return verticesNew;
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

            if (graphicsPath.GetBounds().Width == 0 || graphicsPath.GetBounds().Height == 0)
                return;

            // add stroke
            if (configurationValues.strokeAroundRemoveMaterials)
            {
                // adds the stroke to the outside of the brush instead of the inside
                var averagePointsX = graphicsPath.PathPoints.Average(x => x.X);
                var averagePointsY = graphicsPath.PathPoints.Average(x => x.Y);

                // scale
                var scaleX = (configurationValues.strokeWidth / 2 / graphicsPath.GetBounds().Width) + 1;
                var scaleY = (configurationValues.strokeWidth / 2 / graphicsPath.GetBounds().Height) + 1;

                Matrix matrix = new Matrix();
                matrix.Scale(scaleX, scaleY, MatrixOrder.Append);
                graphicsPath.Transform(matrix);


                // move position after scaling away from (0,0)
                var averageNewPointsX = graphicsPath.PathPoints.Average(x => x.X);
                var averageNewPointsY = graphicsPath.PathPoints.Average(x => x.Y);

                var translateX = -(averageNewPointsX - averagePointsX);
                var translateY = -(averageNewPointsY - averagePointsY);

                matrix = new Matrix();
                matrix.Translate(translateX, translateY, MatrixOrder.Append);
                graphicsPath.Transform(matrix);

                // draw the stroke
                var strokeSolidBrush = new SolidBrush(Color.Transparent);
                var strokePen = new Pen(Colours.ColourRemoveStroke(configurationValues.strokeColour), configurationValues.strokeWidth / 2);
                DrawFilledPolygonObjectBrushes(graphics, strokeSolidBrush, strokePen, graphicsPath.PathPoints.Select(x => new Point((int)x.X, (int)x.Y)).ToArray());
            }

            var region = new Region(graphicsPath);
            graphics.ExcludeClip(region);

            graphicsPath.CloseFigure();
        }


        private static List<ObjectToDraw> GetBrushesToDraw(BoundingBox boundingBox, List<BrushVolume> brushVolumesList)
        {
            var brushesToDrawList = new List<ObjectToDraw>();

            foreach (var brushVolume in brushVolumesList)
            {
                var brushSidesListById = new Dictionary<int, List<BrushSide>>();
                brushSidesListById.Add(brushVolume.brushId, brushVolume.brushSides);

                var brushesToDraw = GetBrushesToDraw(boundingBox, brushSidesListById);

                brushesToDrawList.AddRange(brushesToDraw);
            }

            return brushesToDrawList;
        }


        private static List<ObjectToDraw> GetBrushesToDraw(BoundingBox boundingBox, Dictionary<int, List<BrushSide>> brushSidesListById)
        {
            var brushesToDraw = new List<ObjectToDraw>();

            for (int i = 0; i < brushSidesListById.Values.Count(); i++)
            {
                var brushSidesList = brushSidesListById.Values.ElementAt(i).Where(x => x.brushId == brushSidesListById.ElementAt(i).Key);

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
                            JercTypes.Path => Colours.ColourBrush(configurationValues.pathColourLow, configurationValues.pathColourHigh, percentageAboveMin),
                            JercTypes.Cover => Colours.ColourBrush(configurationValues.coverColourLow, configurationValues.coverColourHigh, percentageAboveMin),
                            JercTypes.Overlap => Colours.ColourBrush(configurationValues.overlapColourLow, configurationValues.overlapColourHigh, percentageAboveMin, configurationValues.overlapAlpha),
                            JercTypes.Door => configurationValues.doorColour,
                            JercTypes.Ladder => configurationValues.ladderColour,
                            JercTypes.Danger => Colours.ColourDanger(configurationValues.dangerColour, configurationValues.dangerAlpha),
                            JercTypes.Buyzone => Colours.ColourBuyzones(),
                            JercTypes.BombsiteA => Colours.ColourBombsites(),
                            JercTypes.BombsiteB => Colours.ColourBombsites(),
                            JercTypes.RescueZone => Colours.ColourRescueZones(),
                            JercTypes.None => throw new NotImplementedException(),
                            JercTypes.Remove => throw new NotImplementedException(),
                            JercTypes.Ignore => throw new NotImplementedException(),
                            JercTypes.Hostage => throw new NotImplementedException(),
                            JercTypes.TSpawn => throw new NotImplementedException(),
                            JercTypes.CTSpawn => throw new NotImplementedException(),
                            _ => throw new NotImplementedException()
                        };

                        verticesOffsetsToUse = verticesOffsetsToUse.Distinct().ToList(); // TODO: doesn't seem to work

                        verticesOffsetsToUse.Add(new VerticesToDraw(new Vertices((int)verticesOffset.x, (int)verticesOffset.y, (int)verticesOffset.z), colour));
                    }

                    // get the brush center (for rotating around if specified)
                    var brushSideCenter = GetCenterOfVerticesList(verticesOffsetsToUse.Select(x => x.vertices).ToList());

                    // corrects the verts by taking into account the movement from space in world to the space in the image (which starts at (0,0))
                    var brushSideCenterOffset = GetCorrectedVerticesPositionInWorld(brushSideCenter);

                    // finish
                    brushesToDraw.Add(new ObjectToDraw(configurationValues, brushSide.id, brushSide.brushId, brushSideCenterOffset, verticesOffsetsToUse, false, brushSide.jercType));
                }
            }

            return brushesToDraw;
        }


        private static List<ObjectToDraw> GetDisplacementsToDraw(BoundingBox boundingBox, List<Models.Brush> displacementsInJercType, List<BrushVolume> brushVolumesList)
        {
            var brushesToDrawList = new List<ObjectToDraw>();

            foreach (var brushVolume in brushVolumesList)
            {
                var brushSidesListById = new Dictionary<int, List<BrushSide>>();
                brushSidesListById.Add(brushVolume.brushId, brushVolume.brushSides);

                var brushesToDraw = GetDisplacementsToDraw(boundingBox, displacementsInJercType, brushSidesListById);

                brushesToDrawList.AddRange(brushesToDraw);
            }

            return brushesToDrawList;
        }


        private static List<ObjectToDraw> GetDisplacementsToDraw(BoundingBox boundingBox, List<Models.Brush> displacementsInJercType, Dictionary<int, List<BrushSide>> brushSidesListById)
        {
            var brushesToDraw = new List<ObjectToDraw>();

            for (int i = 0; i < brushSidesListById.Values.Count(); i++)
            {
                var brushSidesList = brushSidesListById.Values.ElementAt(i).Where(x => x.brushId == brushSidesListById.ElementAt(i).Key);

                foreach (var brushSide in brushSidesList.Where(x => x.displacementStuff != null))
                {
                    for (int x = 0; x < brushSide.displacementStuff.numOfRows - 1; x++)
                    {
                        for (int y = 0; y < brushSide.displacementStuff.numOfRows - 1; y++)
                        {
                            var verticesOffsetsToUse = new List<VerticesToDraw>();

                            foreach (var vertices in brushSide.displacementStuff.GetSquareVerticesPositions(x, y))
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
                                    JercTypes.Path => Colours.ColourBrush(configurationValues.pathColourLow, configurationValues.pathColourHigh, percentageAboveMin),
                                    JercTypes.Cover => Colours.ColourBrush(configurationValues.coverColourLow, configurationValues.coverColourHigh, percentageAboveMin),
                                    JercTypes.Overlap => Colours.ColourBrush(configurationValues.overlapColourLow, configurationValues.overlapColourHigh, percentageAboveMin, configurationValues.overlapAlpha),
                                    JercTypes.Door => configurationValues.doorColour,
                                    JercTypes.Ladder => configurationValues.ladderColour,
                                    JercTypes.Danger => Colours.ColourDanger(configurationValues.dangerColour, configurationValues.dangerAlpha),
                                    JercTypes.Buyzone => Colours.ColourBuyzones(),
                                    JercTypes.BombsiteA => Colours.ColourBombsites(),
                                    JercTypes.BombsiteB => Colours.ColourBombsites(),
                                    JercTypes.RescueZone => Colours.ColourRescueZones(),
                                    JercTypes.None => throw new NotImplementedException(),
                                    JercTypes.Remove => throw new NotImplementedException(),
                                    JercTypes.Ignore => throw new NotImplementedException(),
                                    JercTypes.Hostage => throw new NotImplementedException(),
                                    JercTypes.TSpawn => throw new NotImplementedException(),
                                    JercTypes.CTSpawn => throw new NotImplementedException(),
                                    _ => throw new NotImplementedException()
                                };

                                verticesOffsetsToUse = verticesOffsetsToUse.Distinct().ToList(); // TODO: doesn't seem to work

                                verticesOffsetsToUse.Add(new VerticesToDraw(new Vertices((int)verticesOffset.x, (int)verticesOffset.y, (int)verticesOffset.z), colour));
                            }

                            // get the brush center (for rotating around if specified)
                            var brush = displacementsInJercType.FirstOrDefault(x => x.id == brushSide.brushId);
                            var brushCenter = GetCenterOfVerticesList(brush.side.SelectMany(x => x.vertices_plus).ToList());

                            // corrects the verts by taking into account the movement from space in world to the space in the image (which starts at (0,0))
                            var brushCenterOffset = GetCorrectedVerticesPositionInWorld(brushCenter);

                            // finish
                            brushesToDraw.Add(new ObjectToDraw(configurationValues, brushSide.id, brushSide.brushId, brushCenterOffset, verticesOffsetsToUse, true, brushSide.jercType));
                        }
                    }
                }
            }

            return brushesToDraw;
        }


        private static List<ObjectToDraw> GetBrushEntitiesToDraw(OverviewPositionValues overviewPositionValues, Dictionary<int, List<EntityBrushSide>> brushSideListById, OverlayOrderNums overlayOrderNum, JercBoxOrderNums jercBoxOrderNum)
        {
            var brushEntitiesToDraw = new List<ObjectToDraw>();

            for (int i = 0; i < brushSideListById.Values.Count(); i++)
            {
                var brushEntityBrushSideByBrush = brushSideListById.Values.ElementAt(i);

                foreach (var brushEntityBrushSide in brushEntityBrushSideByBrush)
                {
                    // if a brush side is using the corresponding material, it has already been drawn previously, so don't draw it again
                    switch (brushEntityBrushSide.entityType)
                    {
                        case EntityTypes.Buyzone:
                            if (brushEntityBrushSide.material.ToLower() == TextureNames.BuyzoneTextureName)
                                continue;
                            break;
                        case EntityTypes.Bombsite:
                            if (TextureNames.AllBombsiteTextureNames.Any(x => x.ToLower() == brushEntityBrushSide.material.ToLower()))
                                continue;
                            break;
                        case EntityTypes.RescueZone:
                            if (brushEntityBrushSide.material.ToLower() == TextureNames.RescueZoneTextureName)
                                continue;
                            break;
                    }

                    var verticesOffsetsToUse = new List<VerticesToDraw>();

                    foreach (var vertices in brushEntityBrushSide.vertices)
                    {
                        // corrects the verts by taking into account the movement from space in world to the space in the image (which starts at (0,0))
                        var verticesOffset = GetCorrectedVerticesPositionInWorld(vertices);

                        Color colour = brushEntityBrushSide.entityType switch
                        {
                            EntityTypes.Buyzone => Colours.ColourBuyzones(),
                            EntityTypes.Bombsite => Colours.ColourBombsites(),
                            EntityTypes.RescueZone => Colours.ColourRescueZones(),
                            EntityTypes.Overlays => brushEntityBrushSide.rendercolor,
                            EntityTypes.JercBox => brushEntityBrushSide.rendercolor,
                            EntityTypes.None => throw new NotImplementedException(),
                            _ => throw new NotImplementedException(),
                        };

                        verticesOffsetsToUse.Add(new VerticesToDraw(new Vertices((int)verticesOffset.x, (int)verticesOffset.y, (int)verticesOffset.z), colour));
                    }

                    verticesOffsetsToUse = verticesOffsetsToUse.Distinct().ToList(); // TODO: doesn't seem to work

                    // get the brush center (for rotating around if specified)
                    var brushSideCenter = GetCenterOfVerticesList(verticesOffsetsToUse.Select(x => x.vertices).ToList());

                    // corrects the verts by taking into account the movement from space in world to the space in the image (which starts at (0,0))
                    var brushSideCenterOffset = GetCorrectedVerticesPositionInWorld(brushSideCenter);

                    // finish
                    if (brushEntityBrushSide.entityType == EntityTypes.Overlays || brushEntityBrushSide.entityType == EntityTypes.JercBox)
                    {
                        if (brushEntityBrushSide.orderNum == (int)overlayOrderNum) // ignore any that are being drawn at a different order num
                        {
                            brushEntitiesToDraw.Add(new ObjectToDraw(configurationValues, brushEntityBrushSide.id, brushEntityBrushSide.brushId, brushSideCenterOffset, verticesOffsetsToUse, false, brushEntityBrushSide.entityType, brushEntityBrushSide.rendercolor, brushEntityBrushSide.colourStroke, brushEntityBrushSide.strokeWidth));
                        }
                        else if (brushEntityBrushSide.orderNum == (int)jercBoxOrderNum) // ignore any that are being drawn at a different order num
                        {
                            brushEntitiesToDraw.Add(new ObjectToDraw(configurationValues, brushEntityBrushSide.id, brushEntityBrushSide.brushId, brushSideCenterOffset, verticesOffsetsToUse, false, brushEntityBrushSide.entityType, brushEntityBrushSide.rendercolor, brushEntityBrushSide.colourStroke, brushEntityBrushSide.strokeWidth));
                        }
                    }
                    else
                    {
                        brushEntitiesToDraw.Add(new ObjectToDraw(configurationValues, brushEntityBrushSide.id, brushEntityBrushSide.brushId, brushSideCenterOffset, verticesOffsetsToUse, false, brushEntityBrushSide.entityType));
                    }
                }
            }

            return brushEntitiesToDraw;
        }


        public static Vertices GetCenterOfVerticesList(List<Vertices> verticesList)
        {
            var center = new Vertices(verticesList.Average(x => x.x), verticesList.Average(x => x.y), (float)verticesList.Average(x => x.z));
            return center;
        }


        private static void DisposeGraphics(Graphics graphics)
        {
            graphics.Dispose();
        }


        private static void DisposeImage(Bitmap bmp)
        {
            bmp.Dispose();
        }


        private static void DrawFilledPolygonGradient(Graphics graphics, ObjectToDraw objectToDraw, bool drawAroundEdge, LevelHeight levelHeightOverride = null)
        {
            RotateObjectBeforeDrawingIfSpecified(objectToDraw); // jerc_disp_rotation

            // Make the points for a polygon.
            var vertices = objectToDraw.verticesToDraw.Select(x => x.vertices).ToList();

            // remove duplicate point positions (this can be caused by vertical brush sides, where their X and Y values are the same (Z is not taken into account here))
            vertices = vertices.Distinct().ToList();

            // check there are still more than 2 points
            if (vertices.Count() < 3)
                return;

            // check there are more than 1 value on each axis (otherwise they are just side faces)
            if (vertices.Select(x => x.x).Distinct().Count() < 2 || vertices.Select(x => x.y).Distinct().Count() < 2)
                return;

            // draw polygon
            var verticesArray = vertices.Select(x => new Point((int)x.x, (int)x.y)).ToArray();

            using (PathGradientBrush pathBrush = new PathGradientBrush(verticesArray))
            {
                var colourUsing = Color.White;

                if (levelHeightOverride == null) // being drawn for the normal radar levels
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

                    colourUsing = averageColour;
                    pathBrush.SurroundColors = colours.ToArray();
                }
                else
                {
                    if ((objectToDraw.entityType != null && objectToDraw.entityType != EntityTypes.None) || (objectToDraw.jercType != null && objectToDraw.jercType != JercTypes.None)) // being drawn for the raw masks
                    {
                        if (objectToDraw.entityType != null && objectToDraw.entityType != EntityTypes.None) // being drawn for the raw masks
                        {
                            switch (objectToDraw.entityType)
                            {
                                case EntityTypes.Buyzone:
                                    colourUsing = Colours.ColourBuyzonesStroke();
                                    break;
                                case EntityTypes.Bombsite:
                                    colourUsing = Colours.ColourBombsitesStroke();
                                    break;
                                case EntityTypes.RescueZone:
                                    colourUsing = Colours.ColourRescueZonesStroke();
                                    break;
                                case EntityTypes.Overlays:
                                case EntityTypes.JercBox:
                                    //colourUsing = GetGreyscaleColourByHeight((float)vertices.Average(x => x.z), levelHeightOverride);
                                    colourUsing = (Color)objectToDraw.colour;
                                    break;
                                default:
                                    colourUsing = Colours.ColourError;
                                    break;
                            }
                        }
                        else // being drawn for the raw masks
                        {
                            switch (objectToDraw.jercType)
                            {
                                case JercTypes.Path:
                                case JercTypes.Cover:
                                case JercTypes.Overlap:
                                case JercTypes.Door:
                                case JercTypes.Ladder:
                                case JercTypes.Danger:
                                    colourUsing = GetGreyscaleColourByHeight((float)vertices.Average(x => x.z), levelHeightOverride);
                                    break;
                                case JercTypes.Buyzone:
                                    colourUsing = Colours.ColourBuyzonesStroke();
                                    break;
                                case JercTypes.BombsiteA:
                                case JercTypes.BombsiteB:
                                    colourUsing = Colours.ColourBombsitesStroke();
                                    break;
                                case JercTypes.RescueZone:
                                    colourUsing = Colours.ColourRescueZonesStroke();
                                    break;
                                default:
                                    colourUsing = Colours.ColourError;
                                    break;
                            }
                        }

                        var colours = new List<Color>();
                        for (int i = 0; i < verticesArray.Length; i++)
                        {
                            colours.Add(colourUsing);
                        }

                        pathBrush.SurroundColors = colours.ToArray();
                    }
                    else // is this ever called ?
                    {
                        colourUsing = GetGreyscaleColourByHeight((float)vertices.Min(a => a.z), levelHeightOverride);
                    }

                    // surrounding vertices colours
                    if (objectToDraw.jercType != null && (objectToDraw.jercType == JercTypes.Path || objectToDraw.jercType == JercTypes.Cover || objectToDraw.jercType == JercTypes.Overlap))
                    {
                        var colours = new List<Color>();
                        for (int i = 0; i < verticesArray.Length; i++)
                        {
                            colours.Add(GetGreyscaleColourByHeight((float)objectToDraw.verticesToDraw[i].vertices.z, levelHeightOverride));
                        }

                        pathBrush.SurroundColors = colours.ToArray();
                    }
                    else
                    {
                        var colours = new List<Color>();
                        for (int i = 0; i < verticesArray.Length; i++)
                        {
                            colours.Add(colourUsing);
                        }

                        pathBrush.SurroundColors = colours.ToArray();
                    }
                }

                // Define the center and surround colors.
                pathBrush.CenterColor = colourUsing;

                // Fill the polygon
                graphics.FillPolygon(pathBrush, verticesArray);

                // Draw border of the polygon
                if (drawAroundEdge)
                {
                    var verticesToDrawList = objectToDraw.verticesToDraw;
                    for (int i = 0; i < verticesToDrawList.Count(); i++)
                    {
                        var verticesToDraw1 = verticesToDrawList[i];
                        var verticesToDraw2 = (i == verticesToDrawList.Count() - 1) ? verticesToDrawList[0] : verticesToDrawList[i + 1];

                        if (verticesToDraw1.vertices == verticesToDraw2.vertices ||
                            (verticesToDraw1.vertices.x == verticesToDraw2.vertices.x && verticesToDraw1.vertices.y == verticesToDraw2.vertices.y)
                        )
                        {
                            continue;
                        }

                        var pointToDraw1 = new Point((int)verticesToDraw1.vertices.x, (int)verticesToDraw1.vertices.y);
                        var pointToDraw2 = new Point((int)verticesToDraw2.vertices.x, (int)verticesToDraw2.vertices.y);

                        try
                        {
                            using (LinearGradientBrush linearBrush = new LinearGradientBrush(pointToDraw1, pointToDraw2, verticesToDraw1.colour, verticesToDraw2.colour))
                            {
                                Pen pen = new Pen(linearBrush);

                                graphics.DrawLine(pen, pointToDraw1, pointToDraw2);

                                pen?.Dispose();
                            }
                        }
                        catch (OutOfMemoryException e)
                        {
                            Logger.LogError($"Out of memory. You most likely have something in the JERC visgroup that is not inside the boundary of your Path brushes/displacements. Skipping. Brush ID: {objectToDraw.brushId}");
                            return;
                        }
                    }
                }
            }
        }


        private static Color GetGreyscaleColourByHeight(float zValue, LevelHeight levelHeight)
        {
            //var heightAboveMin = vertices.Min(x => x.z) - levelHeight.zMinForRadarGradient;
            var heightAboveMin = zValue - levelHeight.zMinForRadarGradient;
            var percentageAboveMin = (float)((Math.Ceiling(Convert.ToDouble(heightAboveMin)) / (levelHeight.zMaxForRadarGradient - levelHeight.zMinForRadarGradient)));
            return Colours.GetGreyscaleGradient(percentageAboveMin * 255);
        }


        private static void DrawFilledPolygonObjectBrushes(Graphics graphics, SolidBrush solidBrush, Pen pen, Point[] vertices)
        {
            graphics.FillPolygon(solidBrush, vertices);
            graphics.DrawPolygon(pen, vertices);

            solidBrush?.Dispose();
            pen?.Dispose();
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


        private static void SaveImage(string filepath, Bitmap bmp, bool forcePngOnly = false, DangerZoneSpecificFiles dangerZoneSpecificFile = DangerZoneSpecificFiles.None)
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
            if (!canSave)
            {
                Logger.LogError($"Could not save to {filepath}, file is most likely locked.");
                return;
            }


            var filepathPng = filepath + ".png";
            var filepathDds = filepath + ".dds";

            // danger zone tablet or spawn select
            if (configurationValues.overviewGamemodeType == 1 && dangerZoneSpecificFile != DangerZoneSpecificFiles.None)
            {
                float newBitmapSizeDivider = dangerZoneSpecificFile == DangerZoneSpecificFiles.TabletImage
                    ? ((float)overviewPositionValues.widthWithoutStroke + (float)overviewPositionValues.paddingSizeX) / (float)DangerZoneValues.DangerZoneOverviewSize // 20480 (tablet)
                    : ((float)overviewPositionValues.widthWithoutStroke + (float)overviewPositionValues.paddingSizeX) / (float)DangerZoneValues.DangerZonePlayareaSize; // 16384 (spawn select)

                var newSizeX = (int)(Sizes.FinalOutputImageResolution / newBitmapSizeDivider);
                var newSizeY = (int)(Sizes.FinalOutputImageResolution / newBitmapSizeDivider);
                Bitmap bmpTemp = new Bitmap(newSizeX, newSizeY);
                using (Graphics graphicsTemp = Graphics.FromImage(bmpTemp))
                {
                    graphicsTemp.SmoothingMode = SmoothingMode.HighSpeed;
                    graphicsTemp.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphicsTemp.Clear(Color.Transparent);
                    int xStartPos = (bmpTemp.Width - Sizes.FinalOutputImageResolution) / 2;
                    int yStartPos = (bmpTemp.Height - Sizes.FinalOutputImageResolution) / 2;
                    graphicsTemp.DrawImage(bmp, xStartPos, yStartPos);
                }

                bmp = new Bitmap(bmpTemp, Sizes.FinalOutputImageResolution, Sizes.FinalOutputImageResolution);

                if (dangerZoneSpecificFile == DangerZoneSpecificFiles.TabletImage)
                {
                    bmp.Save(filepathPng, ImageFormat.Png);

                    if (!File.Exists(vmfcmdFilepath))
                    {
                        Logger.LogImportantWarning("VMTCmd.exe not found, exporting Danger Zone tablet image as a png instead of a vtf");
                    }
                    else
                    {
                        Logger.LogMessage($"Converting {filepathPng} to vtf");

                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.CreateNoWindow = false;
                        startInfo.UseShellExecute = false;
                        startInfo.FileName = vmfcmdFilepath;
                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        startInfo.Arguments = $"-file \"{filepathPng}\" -output \"{Path.GetDirectoryName(filepathPng)}\" -silent";

                        try
                        {
                            Process.Start(startInfo).WaitForExit();

                            Logger.LogMessage($"Successfully converted {filepathPng} to vtf");

                            if (File.Exists(filepathPng) && !configurationValues.exportPng)
                                File.Delete(filepathPng);
                        }
                        catch (Exception e)
                        {
                            Logger.LogImportantWarning("Failed to export Danger Zone tablet image as a vtf, exporting as png instead");
                        }
                    }
                }
                else if (dangerZoneSpecificFile == DangerZoneSpecificFiles.SpawnselectImage)
                {
                    bmp.Save(filepathPng, ImageFormat.Png);
                }
                else
                {
                    if (configurationValues.exportDds && !forcePngOnly)
                        bmp.Save(filepathDds);

                    if (configurationValues.exportPng || forcePngOnly)
                        bmp.Save(filepathPng, ImageFormat.Png);
                }

                // dispose
                DisposeGraphics(Graphics.FromImage(bmpTemp));
                DisposeImage(bmpTemp);
            }
            else // overview (danger zone or standard)
            {
                if (configurationValues.exportDds && !forcePngOnly)
                    bmp.Save(filepathDds);

                if (configurationValues.exportPng || forcePngOnly)
                    bmp.Save(filepathPng, ImageFormat.Png);
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
                    Logger.LogWarning(string.Concat("File has been locked ", retries, " time(s). Waiting ", waitTimeSeconds, " seconds before trying again. Filepath: ", filepath));

                    Thread.Sleep(waitTimeSeconds * 1000);
                    continue;
                }
            }

            Logger.LogImportantWarning(string.Concat("SKIPPING! File has been locked ", maxRetries, " times. Filepath: ", filepath));

            return false;
        }


        private static void CreateFileIfDoesntExist(string filepath)
        {
            if (!File.Exists(filepath))
            {
                File.Create(filepath).Close();
            }
        }


        private static void CreateDirectoryOfFileIfDoesntExist(string filepath)
        {
            var directory = Path.GetDirectoryName(filepath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }


        private static void FlipImage(Bitmap bmp)
        {
            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
        }


        private static void GenerateTxt(List<LevelHeight> levelHeights)
        {
            Logger.LogMessage("Generating txt");

            var overviewTxt = GetOverviewTxt(overviewPositionValues);

            var lines = overviewTxt.GetInExportableFormat(configurationValues, levelHeights, mapName);

            if (!configurationValues.onlyOutputToAlternatePath)
            {
                var outputTxtFilepath = string.Concat(overviewsOutputFilepathPrefix, ".txt");
                SaveOutputTxtFile(outputTxtFilepath, lines);
            }

            if (!string.IsNullOrWhiteSpace(configurationValues.alternateOutputPath))
            {
                var outputTxtFilepath = string.Concat(configurationValues.alternateOutputPath, @"resource\overviews\", mapName, ".txt");
                CreateDirectoryOfFileIfDoesntExist(outputTxtFilepath);
                SaveOutputTxtFile(outputTxtFilepath, lines);
            }

            Logger.LogMessage("Generating txt complete");
        }


        private static OverviewTxt GetOverviewTxt(OverviewPositionValues overviewPositionValues)
        {
            string scale = configurationValues.overviewGamemodeType == 1 ? DangerZoneValues.DangerZoneOverviewTxtScale.ToString() : overviewPositionValues.scale.ToString(); // forces specific value if DZ
            string pos_x = configurationValues.overviewGamemodeType == 1 ? DangerZoneValues.DangerZoneOverviewTxtPosX.ToString() : overviewPositionValues.posX.ToString(); // forces specific value if DZ
            string pos_y = configurationValues.overviewGamemodeType == 1 ? DangerZoneValues.DangerZoneOverviewTxtPosY.ToString() : overviewPositionValues.posY.ToString(); // forces specific value if DZ
            string rotate = null;
            string zoom = null;

            string inset_left = null, inset_top = null, inset_right = null, inset_bottom = null;

            string CTSpawn_x = null, CTSpawn_y = null, TSpawn_x = null, TSpawn_y = null;

            string bombA_x = null, bombA_y = null, bombB_x = null, bombB_y = null;

            string Hostage1_x = null, Hostage1_y = null, Hostage2_x = null, Hostage2_y = null, Hostage3_x = null, Hostage3_y = null, Hostage4_x = null, Hostage4_y = null;
            string Hostage5_x = null, Hostage5_y = null, Hostage6_x = null, Hostage6_y = null, Hostage7_x = null, Hostage7_y = null, Hostage8_x = null, Hostage8_y = null;


            var paddingPercentageEachSideX = overviewPositionValues.paddingPercentageX == 0 ? 0 : (overviewPositionValues.paddingPercentageX / 2);
            var paddingPercentageEachSideY = overviewPositionValues.paddingPercentageY == 0 ? 0 : (overviewPositionValues.paddingPercentageY / 2);

            // ct spawns
            if (vmfRequiredData.brushesCTSpawn.Any() || vmfRequiredData.displacementsCTSpawn.Any())
            {
                var vertices = vmfRequiredData.brushesCTSpawn.Concat(vmfRequiredData.displacementsCTSpawn).SelectMany(x => x.side.SelectMany(y => y.vertices_plus));

                var xAllValues = vertices.Select(x => x.x);
                var yAllValues = vertices.Select(x => x.y);
                var xAverage = xAllValues.Average();
                var yAverage = yAllValues.Average();
                //var xPercent = Math.Abs((xAverage - (overviewPositionValues.brushVerticesPosMinX + overviewPositionValues.paddingSizeX)) / overviewPositionValues.outputResolution);
                var xPercent = Math.Abs((Math.Abs(Math.Abs(xAverage - overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                var yPercent = Math.Abs((Math.Abs(Math.Abs(yAverage - overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                CTSpawn_x = FlipOverviewTxtValues(xPercent, true);
                CTSpawn_y = FlipOverviewTxtValues(yPercent, false);
            }
            else if (vmfRequiredData.ctSpawnEntities.Any())
            {
                var origins = vmfRequiredData.ctSpawnEntities.Select(x => new Vertices(x.origin));
                var xPercent = Math.Abs((Math.Abs(Math.Abs(origins.Average(x => x.x) - overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                var yPercent = Math.Abs((Math.Abs(Math.Abs(origins.Average(x => x.y) - overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                CTSpawn_x = FlipOverviewTxtValues(xPercent, true);
                CTSpawn_y = FlipOverviewTxtValues(yPercent, false);
            }
            //

            // t spawns
            if (vmfRequiredData.brushesTSpawn.Any() || vmfRequiredData.displacementsTSpawn.Any())
            {
                var vertices = vmfRequiredData.brushesTSpawn.Concat(vmfRequiredData.displacementsTSpawn).SelectMany(x => x.side.SelectMany(y => y.vertices_plus));

                var xAllValues = vertices.Select(x => x.x);
                var yAllValues = vertices.Select(x => x.y);
                var xAverage = xAllValues.Average();
                var yAverage = yAllValues.Average();
                //var xPercent = Math.Abs((xAverage - (overviewPositionValues.brushVerticesPosMinX + overviewPositionValues.paddingSizeX)) / overviewPositionValues.outputResolution);
                var xPercent = Math.Abs((Math.Abs(Math.Abs(xAverage - overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                var yPercent = Math.Abs((Math.Abs(Math.Abs(yAverage - overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                TSpawn_x = FlipOverviewTxtValues(xPercent, true);
                TSpawn_y = FlipOverviewTxtValues(yPercent, false);
            }
            else if (vmfRequiredData.tSpawnEntities.Any())
            {
                var origins = vmfRequiredData.tSpawnEntities.Select(x => new Vertices(x.origin));
                var xPercent = Math.Abs((Math.Abs(Math.Abs(origins.Average(x => x.x) - overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                var yPercent = Math.Abs((Math.Abs(Math.Abs(origins.Average(x => x.y) - overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                TSpawn_x = FlipOverviewTxtValues(xPercent, true);
                TSpawn_y = FlipOverviewTxtValues(yPercent, false);
            }
            //


            // bombsites
            if (vmfRequiredData.brushesBombsiteA.Any() || vmfRequiredData.displacementsBombsiteA.Any() || vmfRequiredData.brushesBombsiteB.Any() || vmfRequiredData.displacementsBombsiteB.Any())
            {
                // bombsite a
                var vertices1 = vmfRequiredData.brushesBombsiteA.Any() || vmfRequiredData.displacementsBombsiteA.Any()
                    ? vmfRequiredData.brushesBombsiteA.Concat(vmfRequiredData.displacementsBombsiteA).SelectMany(x => x.side.SelectMany(y => y.vertices_plus))
                    : null;

                var xAllValues1 = vertices1.Select(x => x.x);
                var yAllValues1 = vertices1.Select(x => x.y);
                var xAverage1 = xAllValues1.Average();
                var yAverage1 = yAllValues1.Average();
                //var xPercent1 = Math.Abs((xAverage1 - (overviewPositionValues.brushVerticesPosMinX + overviewPositionValues.paddingSizeX)) / overviewPositionValues.outputResolution);
                var xPercent1 = Math.Abs((Math.Abs(Math.Abs(xAverage1 - overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                var yPercent1 = Math.Abs((Math.Abs(Math.Abs(yAverage1 - overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                bombA_x = FlipOverviewTxtValues(xPercent1, true);
                bombA_y = FlipOverviewTxtValues(yPercent1, false);

                // bombsite b
                if (vmfRequiredData.brushesBombsiteB.Any() || vmfRequiredData.displacementsBombsiteB.Any())
                {
                    var vertices2 = vmfRequiredData.brushesBombsiteB.Any() || vmfRequiredData.displacementsBombsiteB.Any()
                        ? vmfRequiredData.brushesBombsiteB.Concat(vmfRequiredData.displacementsBombsiteB).SelectMany(x => x.side.SelectMany(y => y.vertices_plus))
                        : null;

                    var xAllValues2 = vertices2.Select(x => x.x);
                    var yAllValues2 = vertices2.Select(x => x.y);
                    var xAverage2 = xAllValues2.Average();
                    var yAverage2 = yAllValues2.Average();
                    var xPercent2 = Math.Abs((Math.Abs(Math.Abs(xAverage2 - overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                    var yPercent2 = Math.Abs((Math.Abs(Math.Abs(yAverage2 - overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                    bombB_x = FlipOverviewTxtValues(xPercent2, true);
                    bombB_y = FlipOverviewTxtValues(yPercent2, false);
                }
            }
            else if (vmfRequiredData.bombsiteBrushEntities.Any()) // won't do bombsite entities if JERC bombsite materials are found on brushes
            {
                var bombsiteEntities = vmfRequiredData.bombsiteBrushEntities;

                if (!string.IsNullOrWhiteSpace(vmfRequiredData.bombsiteBrushEntities.LastOrDefault().targetname) && vmfRequiredData.bombsiteBrushEntities.LastOrDefault().targetname.ToLower().Contains("bombsite_a"))
                {
                    bombsiteEntities.Reverse();
                }

                var xAllValues1 = bombsiteEntities.FirstOrDefault().brushes.SelectMany(x => x.side.SelectMany(y => y.vertices_plus.Select(x => x.x)));
                var yAllValues1 = bombsiteEntities.FirstOrDefault().brushes.SelectMany(x => x.side.SelectMany(y => y.vertices_plus.Select(x => x.y)));
                var xAverage1 = xAllValues1.Average();
                var yAverage1 = yAllValues1.Average();
                //var xPercent1 = Math.Abs((xAverage1 - (overviewPositionValues.brushVerticesPosMinX + overviewPositionValues.paddingSizeX)) / overviewPositionValues.outputResolution);
                var xPercent1 = Math.Abs((Math.Abs(Math.Abs(xAverage1 - overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                var yPercent1 = Math.Abs((Math.Abs(Math.Abs(yAverage1 - overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                bombA_x = FlipOverviewTxtValues(xPercent1, true);
                bombA_y = FlipOverviewTxtValues(yPercent1, false);

                if (vmfRequiredData.bombsiteBrushEntities.Count() > 1)
                {
                    var xAllValues2 = bombsiteEntities.Skip(1).FirstOrDefault().brushes.SelectMany(x => x.side.SelectMany(y => y.vertices_plus.Select(x => x.x)));
                    var yAllValues2 = bombsiteEntities.Skip(1).FirstOrDefault().brushes.SelectMany(x => x.side.SelectMany(y => y.vertices_plus.Select(x => x.y)));
                    var xAverage2 = xAllValues2.Average();
                    var yAverage2 = yAllValues2.Average();
                    var xPercent2 = Math.Abs((Math.Abs(Math.Abs(xAverage2 - overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                    var yPercent2 = Math.Abs((Math.Abs(Math.Abs(yAverage2 - overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                    bombB_x = FlipOverviewTxtValues(xPercent2, true);
                    bombB_y = FlipOverviewTxtValues(yPercent2, false);
                }
            }
            //

            // hostage spawns
            if (vmfRequiredData.brushesHostage.Any() || vmfRequiredData.displacementsHostage.Any())
            {
                var brushSpawns = vmfRequiredData.brushesHostage.Concat(vmfRequiredData.displacementsHostage).ToList();
                for (var i = 1; i <= brushSpawns.Count(); i++)
                {
                    var vertices = brushSpawns[i-1].side.SelectMany(y => y.vertices_plus);

                    var xAllValues = vertices.Select(x => x.x);
                    var yAllValues = vertices.Select(x => x.y);
                    var xAverage = xAllValues.Average();
                    var yAverage = yAllValues.Average();
                    //var xPercent = Math.Abs((xAverage - (overviewPositionValues.brushVerticesPosMinX + overviewPositionValues.paddingSizeX)) / overviewPositionValues.outputResolution);
                    var xPercent = Math.Abs((Math.Abs(Math.Abs(xAverage - overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                    var yPercent = Math.Abs((Math.Abs(Math.Abs(yAverage - overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                    switch (i)
                    {
                        case 1:
                            Hostage1_x = FlipOverviewTxtValues(xPercent, true);
                            Hostage1_y = FlipOverviewTxtValues(yPercent, false);
                            break;
                        case 2:
                            Hostage2_x = FlipOverviewTxtValues(xPercent, true);
                            Hostage2_y = FlipOverviewTxtValues(yPercent, false);
                            break;
                        case 3:
                            Hostage3_x = FlipOverviewTxtValues(xPercent, true);
                            Hostage3_y = FlipOverviewTxtValues(yPercent, false);
                            break;
                        case 4:
                            Hostage4_x = FlipOverviewTxtValues(xPercent, true);
                            Hostage4_y = FlipOverviewTxtValues(yPercent, false);
                            break;
                        case 5:
                            Hostage5_x = FlipOverviewTxtValues(xPercent, true);
                            Hostage5_y = FlipOverviewTxtValues(yPercent, false);
                            break;
                        case 6:
                            Hostage6_x = FlipOverviewTxtValues(xPercent, true);
                            Hostage6_y = FlipOverviewTxtValues(yPercent, false);
                            break;
                        case 7:
                            Hostage7_x = FlipOverviewTxtValues(xPercent, true);
                            Hostage7_y = FlipOverviewTxtValues(yPercent, false);
                            break;
                        case 8:
                            Hostage8_x = FlipOverviewTxtValues(xPercent, true);
                            Hostage8_y = FlipOverviewTxtValues(yPercent, false);
                            break;
                    }
                }
            }
            else if (vmfRequiredData.hostageEntities.Any())
            {
                for (var i = 1; i <= vmfRequiredData.hostageEntities.Count(); i++)
                {
                    var origin = new Vertices(vmfRequiredData.hostageEntities.ElementAt(i-1).origin);
                    var xPercent = Math.Abs((Math.Abs(Math.Abs(origin.x - overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                    var yPercent = Math.Abs((Math.Abs(Math.Abs(origin.y - overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                    switch (i)
                    {
                        case 1:
                            Hostage1_x = FlipOverviewTxtValues(xPercent, true);
                            Hostage1_y = FlipOverviewTxtValues(yPercent, false);
                            break;
                        case 2:
                            Hostage2_x = FlipOverviewTxtValues(xPercent, true);
                            Hostage2_y = FlipOverviewTxtValues(yPercent, false);
                            break;
                        case 3:
                            Hostage3_x = FlipOverviewTxtValues(xPercent, true);
                            Hostage3_y = FlipOverviewTxtValues(yPercent, false);
                            break;
                        case 4:
                            Hostage4_x = FlipOverviewTxtValues(xPercent, true);
                            Hostage4_y = FlipOverviewTxtValues(yPercent, false);
                            break;
                        case 5:
                            Hostage5_x = FlipOverviewTxtValues(xPercent, true);
                            Hostage5_y = FlipOverviewTxtValues(yPercent, false);
                            break;
                        case 6:
                            Hostage6_x = FlipOverviewTxtValues(xPercent, true);
                            Hostage6_y = FlipOverviewTxtValues(yPercent, false);
                            break;
                        case 7:
                            Hostage7_x = FlipOverviewTxtValues(xPercent, true);
                            Hostage7_y = FlipOverviewTxtValues(yPercent, false);
                            break;
                        case 8:
                            Hostage8_x = FlipOverviewTxtValues(xPercent, true);
                            Hostage8_y = FlipOverviewTxtValues(yPercent, false);
                            break;
                    }
                }
            }
            //


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
