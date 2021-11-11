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
        private static readonly bool debugging = false;
        private static readonly string debuggingJercPath = @"F:\Coding Stuff\GitHub Files\JERC\";

        private static readonly ImageProcessorExtender imageProcessorExtender = new ImageProcessorExtender();

        private static string backgroundImagesDirectory;
        private static string outputFilepathPrefix;
        private static string outputImageBackgroundLevelsFilepath;

        private static readonly string visgroupName = "JERC";

        private static ConfigurationValues configurationValues;

        private static int visgroupIdMainVmf;
        private static readonly Dictionary<int, int> visgroupIdsByInstanceEntityIds = new Dictionary<int, int>();

        private static string mapName;

        private static VMF vmf;
        private static readonly Dictionary<VMF, int> instanceEntityIdsByVmf = new Dictionary<VMF, int>();
        private static VmfRequiredData vmfRequiredData;
        private static OverviewPositionValues overviewPositionValues;


        static void Main(string[] args)
        {
            GameConfigurationValues.SetArgs(args);

            if (!debugging && (!GameConfigurationValues.VerifyAllValuesSet() || !GameConfigurationValues.VerifyAllDirectoriesAndFilesExist()))
            {
                Logger.LogError("Game configuration files missing. Check the compile configuration's parameters.");
                return;
            }

            if (!debugging && (GameConfigurationValues.binFolderPath.Split(@"\").Reverse().Skip(1).FirstOrDefault() != "bin" || GameConfigurationValues.binFolderPath.Replace("/", @"\").Replace(@"\\", @"\").Contains(@"\csgo\bin")))
            {
                Logger.LogError(@"JERC's folder should be placed in ...\Counter-Strike Global Offensive\bin");
                return;
            }

            var lines = File.ReadAllLines(GameConfigurationValues.vmfFilepath);

            mapName = Path.GetFileNameWithoutExtension(GameConfigurationValues.vmfFilepath);

            if (debugging)
            {
                Logger.LogDebugInfo("Setting backgroundImagesDirectory to empty string");
                Logger.LogDebugInfo("Setting outputFilepathPrefix to empty string");

                backgroundImagesDirectory = string.Concat(debuggingJercPath, @"JERC\Resources\materials\jerc\backgrounds\");
                outputFilepathPrefix = string.Concat(debuggingJercPath, mapName);
            }
            else
            {
                backgroundImagesDirectory = string.Concat(GameConfigurationValues.csgoFolderPath, @"materials\jerc\backgrounds\");
                outputFilepathPrefix = string.Concat(GameConfigurationValues.overviewsFolderPath, mapName);
            }

            outputImageBackgroundLevelsFilepath = string.Concat(outputFilepathPrefix, "_background_levels.png");

            vmf = new VMF(lines);

            if (vmf == null)
            {
                Logger.LogError("Error parsing VMF, aborting");
                return;
            }

            Logger.LogNewLine();
            Logger.LogMessage("VMF parsed sucessfully");

            SortInstances(vmf);

            SetVisgroupIdMainVmf();
            SetVisgroupIdInstancesByEntityId();

            vmfRequiredData = GetVmfRequiredData();

            if (vmfRequiredData == null)
            {
                Logger.LogError("Either no jerc_config entity found, or more than one jerc_disp_rotation entity found. Aborting!");
                return;
            }

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

            var allDisplayedBrushes = vmfRequiredData.GetAllDisplayedBrushes();
            foreach (var brushId in configurationValues.displacementRotationIds90.Where(x => !allDisplayedBrushes.Any(y => y.id == x)))
            {
                Logger.LogImportantWarning($"Could not find brush {brushId} to rotate 90 degrees clockwise");
            }
            foreach (var brushId in configurationValues.displacementRotationIds180.Where(x => !allDisplayedBrushes.Any(y => y.id == x)))
            {
                Logger.LogImportantWarning($"Could not find brush {brushId} to rotate 180 degrees");
            }
            foreach (var brushId in configurationValues.displacementRotationIds270.Where(x => !allDisplayedBrushes.Any(y => y.id == x)))
            {
                Logger.LogImportantWarning($"Could not find brush {brushId} to rotate 90 degrees anti-clockwise");
            }
        }


        private static void SortInstances(VMF vmf)
        {
            var instanceEntities = vmf.Body.Where(x => x.Name == "entity").Where(x => x.Body.Any(y => y.Name == "classname" && y.Value == "func_instance")).ToList();

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

                var lines = File.ReadAllLines(filepath);

                var newVmf = new VMF(lines);

                if (newVmf == null)
                {
                    Logger.LogImportantWarning("Instance vmf data was null, skipping. Entity ID: " + instance.id);
                    continue;
                }

                // correct origins and angles
                foreach (var entity in newVmf.Body.Where(x => x.Name == "entity"))
                {
                    // entity id is not changed
                    // brush rotation is not changed

                    var originIVNode = entity.Body.FirstOrDefault(x => x.Name == "origin");
                    if (originIVNode != null)
                        MoveAndRotateVerticesInInstance(instance, originIVNode);

                    var allBrushSidesInEntity = entity.Body.Where(x => x.Name == "solid" && x.Body != null)?.SelectMany(x => x.Body.Where(y => y.Name == "side" && y.Body != null)?.Select(y => y.Body))?.ToList();
                    MoveAndRotateAllBrushSidesInInstance(instance, allBrushSidesInEntity);
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
        }


        private static void MoveAndRotateAllBrushSidesInInstance(FuncInstance instance, List<IList<IVNode>> brushSideIVNodeList)
        {
            foreach (var brushSide in brushSideIVNodeList)
            {
                // brush id is not changed
                // rotation is not changed

                foreach (var verticesPlusIVNode in brushSide.Where(x => x.Name == "vertices_plus").SelectMany(x => x.Body))
                {
                    MoveAndRotateVerticesInInstance(instance, verticesPlusIVNode);
                }
            }
        }


        private static void MoveAndRotateVerticesInInstance(FuncInstance instance, IVNode ivNode)
        {
            ivNode.Value = MergeVerticesToString(instance.origin, ivNode.Value); // removes the offset that being in an instances causes
            ivNode.Value = GetRotatedVerticesNewPositionAsString(new Vertices(ivNode.Value), instance.origin, instance.angles.yaw); // removes the rotation that being in an instances causes
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


        private static void SetVisgroupIdMainVmf()
        {
            visgroupIdMainVmf = GetJercVisgroupIdFromVmf(vmf);
        }


        private static void SetVisgroupIdInstancesByEntityId()
        {
            if (instanceEntityIdsByVmf != null && instanceEntityIdsByVmf.Any())
            {
                foreach (var instance in instanceEntityIdsByVmf)
                {
                    var id = GetJercVisgroupIdFromVmf(instance.Key);

                    visgroupIdsByInstanceEntityIds.Add(instance.Value, id);
                }
            }
        }


        private static int GetJercVisgroupIdFromVmf(VMF vmf)
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


        private static VmfRequiredData GetVmfRequiredData()
        {
            Logger.LogNewLine();
            Logger.LogMessage("Getting required data from the vmf and instances...");

            // main vmf contents
            var allWorldBrushes = vmf.World.Body.Where(x => x.Name == "solid");
            var allEntities = vmf.Body.Where(x => x.Name == "entity");

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
            var allWorldBrushesInVisgroup = from x in allWorldBrushes
                                            from y in x.Body
                                            where y.Name == "editor"
                                            from z in y.Body
                                            where z.Name == "visgroupid"
                                            where int.Parse(z.Value, Globalization.Style, Globalization.Culture) == visgroupIdMainVmf ||
                                                visgroupIdsByInstanceEntityIds.Values.Any(a => a == int.Parse(z.Value, Globalization.Style, Globalization.Culture))
                                            select x;

            // brushes
            var brushesIgnore = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.IgnoreTextureName);
            var brushesRemove = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.RemoveTextureName);
            var brushesPath = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.PathTextureName);
            var brushesCover = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.CoverTextureName);
            var brushesOverlap = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.OverlapTextureName);
            var brushesDoor = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.DoorTextureName);
            var brushesLadder = new List<IVNode>();
            foreach (var ladderTextureName in TextureNames.LadderTextureNames)
            {
                brushesLadder.AddRange(GetBrushesByTextureName(allWorldBrushesInVisgroup, ladderTextureName));
            }
            var brushesDanger = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.DangerTextureName);

            var brushesBuyzone = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.BuyzoneTextureName);
            var brushesBombsiteA = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.BombsiteATextureName);
            var brushesBombsiteB = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.BombsiteBTextureName);
            var brushesRescueZone = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.RescueZoneTextureName);
            var brushesHostage = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.HostageTextureName);
            var brushesTSpawn = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.TSpawnTextureName);
            var brushesCTSpawn = GetBrushesByTextureName(allWorldBrushesInVisgroup, TextureNames.CTSpawnTextureName);

            // displacements
            var displacementsIgnore = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.IgnoreTextureName);
            var displacementsRemove = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.RemoveTextureName);
            var displacementsPath = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.PathTextureName);
            var displacementsCover = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.CoverTextureName);
            var displacementsOverlap = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.OverlapTextureName);
            var displacementsDoor = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.DoorTextureName);
            var displacementsLadder = new List<IVNode>();
            foreach (var ladderTextureName in TextureNames.LadderTextureNames)
            {
                displacementsLadder.AddRange(GetDisplacementsByTextureName(allWorldBrushesInVisgroup, ladderTextureName));
            }
            var displacementsDanger = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.DangerTextureName);

            var displacementsBuyzone = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.BuyzoneTextureName);
            var displacementsBombsiteA = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.BombsiteATextureName);
            var displacementsBombsiteB = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.BombsiteBTextureName);
            var displacementsRescueZone = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.RescueZoneTextureName);
            var displacementsHostage = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.HostageTextureName);
            var displacementsTSpawn = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.TSpawnTextureName);
            var displacementsCTSpawn = GetDisplacementsByTextureName(allWorldBrushesInVisgroup, TextureNames.CTSpawnTextureName);

            // brush entities (in game)
            var buyzoneBrushEntities = GetEntitiesByClassname(allEntities, Classnames.Buyzone, true);
            var bombsiteBrushEntities = GetEntitiesByClassname(allEntities, Classnames.Bombsite, true);
            var rescueZoneBrushEntities = GetEntitiesByClassname(allEntities, Classnames.RescueZone, true);

            // entities (in game)
            var hostageEntities = GetEntitiesByClassname(allEntities, Classnames.Hostage, true);
            var tSpawnEntities = GetEntitiesByClassname(allEntities, Classnames.TSpawn, true);
            var ctSpawnEntities = GetEntitiesByClassname(allEntities, Classnames.CTSpawn, true);

            // brush entities (JERC)
            var funcBrushBrushEntities = GetEntitiesByClassname(allEntities, Classnames.FuncBrush, true);
            var funcDetailBrushEntities = GetEntitiesByClassname(allEntities, Classnames.FuncDetail, true);
            var funcDoorBrushEntities = GetEntitiesByClassname(allEntities, Classnames.FuncDoor, true);
            var funcDoorBrushRotatingEntities = GetEntitiesByClassname(allEntities, Classnames.FuncDoorRotating, true);
            var funcLadderBrushEntities = GetEntitiesByClassname(allEntities, Classnames.FuncLadder, true);
            var triggerHurtBrushEntities = GetEntitiesByClassname(allEntities, Classnames.TriggerHurt, true);

            var allBrushesBrushEntities = buyzoneBrushEntities
                .Concat(bombsiteBrushEntities)
                .Concat(rescueZoneBrushEntities)
                .Concat(funcBrushBrushEntities)
                .Concat(funcDetailBrushEntities)
                .Concat(funcDoorBrushEntities)
                .Concat(funcDoorBrushRotatingEntities)
                .Concat(funcLadderBrushEntities)
                .Concat(triggerHurtBrushEntities);

            var brushesIgnoreBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndDangers(allBrushesBrushEntities, TextureNames.IgnoreTextureName);
            var brushesRemoveBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndDangers(allBrushesBrushEntities, TextureNames.RemoveTextureName);
            var brushesPathBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndDangers(allBrushesBrushEntities, TextureNames.PathTextureName);
            var brushesCoverBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndDangers(allBrushesBrushEntities, TextureNames.CoverTextureName);
            var brushesOverlapBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndDangers(allBrushesBrushEntities, TextureNames.OverlapTextureName);
            var brushesDoorBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndDangers(allBrushesBrushEntities, TextureNames.DoorTextureName)
                .Concat(GetBrushEntityBrushesByClassname(allBrushesBrushEntities, Classnames.FuncDoor))
                .Concat(GetBrushEntityBrushesByClassname(allBrushesBrushEntities, Classnames.FuncDoorRotating));
            var brushesLadderBrushEntities = GetBrushEntityBrushesByClassname(allBrushesBrushEntities, Classnames.FuncLadder).ToList();
            foreach (var ladderTextureName in TextureNames.LadderTextureNames)
            {
                brushesLadderBrushEntities.AddRange(GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndDangers(allBrushesBrushEntities, ladderTextureName));
            }
            var brushesDangerBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndDangers(allBrushesBrushEntities, TextureNames.DangerTextureName)
                .Concat(GetBrushEntityBrushesByClassname(allBrushesBrushEntities, Classnames.TriggerHurt));
            var brushesBuyzoneBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndDangers(allBrushesBrushEntities, TextureNames.BuyzoneTextureName);
            var brushesBombsiteABrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndDangers(allBrushesBrushEntities, TextureNames.BombsiteATextureName);
            var brushesBombsiteBBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndDangers(allBrushesBrushEntities, TextureNames.BombsiteBTextureName);
            var brushesRescueZoneBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndDangers(allBrushesBrushEntities, TextureNames.RescueZoneTextureName);
            var brushesHostageBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndDangers(allBrushesBrushEntities, TextureNames.HostageTextureName);
            var brushesTSpawnBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndDangers(allBrushesBrushEntities, TextureNames.TSpawnTextureName);
            var brushesCTSpawnBrushEntities = GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndDangers(allBrushesBrushEntities, TextureNames.CTSpawnTextureName);


            // brush entities (JERC)
            var jercBoxBrushEntities = GetEntitiesByClassname(allEntities, Classnames.JercBox, true);

            // entities (JERC)
            var jercConfigEntities = GetEntitiesByClassname(allEntities, Classnames.JercConfig, false);
            var jercDividerEntities = GetEntitiesByClassname(allEntities, Classnames.JercDivider, false);
            var jercFloorEntities = GetEntitiesByClassname(allEntities, Classnames.JercFloor, false);
            var jercCeilingEntities = GetEntitiesByClassname(allEntities, Classnames.JercCeiling, false);
            var jercDispRotationEntities = GetEntitiesByClassname(allEntities, Classnames.JercDispRotation, false);

            if (jercConfigEntities == null || !jercConfigEntities.Any())
                return null;

            if (jercDispRotationEntities != null && jercDispRotationEntities.Count() > 1)
                return null;

            var allJercEntities = jercConfigEntities.Concat(jercDividerEntities).Concat(jercFloorEntities).Concat(jercCeilingEntities).Concat(jercDispRotationEntities);

            configurationValues = new ConfigurationValues(GetSettingsValuesFromJercEntities(allJercEntities), jercDividerEntities.Count(), jercDispRotationEntities.Any());

            Logger.LogMessage("Retrieved data from the vmf and instances");

            return new VmfRequiredData(
                brushesIgnore, brushesRemove, brushesPath, brushesCover, brushesOverlap, brushesDoor, brushesLadder, brushesDanger,
                brushesBuyzone, brushesBombsiteA, brushesBombsiteB, brushesRescueZone, brushesHostage, brushesTSpawn, brushesCTSpawn,
                displacementsIgnore, displacementsRemove, displacementsPath, displacementsCover, displacementsOverlap, displacementsDoor, displacementsLadder, displacementsDanger,
                displacementsBuyzone, displacementsBombsiteA, displacementsBombsiteB, displacementsRescueZone, displacementsHostage, displacementsTSpawn, displacementsCTSpawn,
                buyzoneBrushEntities, bombsiteBrushEntities, rescueZoneBrushEntities, hostageEntities, ctSpawnEntities, tSpawnEntities,
                brushesIgnoreBrushEntities, brushesRemoveBrushEntities, brushesPathBrushEntities, brushesCoverBrushEntities, brushesOverlapBrushEntities, brushesDoorBrushEntities, brushesLadderBrushEntities, brushesDangerBrushEntities,
                brushesBuyzoneBrushEntities, brushesBombsiteABrushEntities, brushesBombsiteBBrushEntities, brushesRescueZoneBrushEntities, brushesHostageBrushEntities, brushesTSpawnBrushEntities, brushesCTSpawnBrushEntities,
                jercBoxBrushEntities,
                jercConfigEntities, jercDividerEntities, jercFloorEntities, jercCeilingEntities, jercDispRotationEntities
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
            jercEntitySettingsValues.Add("ignoreDisplacementXYChanges", jercConfig.FirstOrDefault(x => x.Name == "ignoreDisplacementXYChanges")?.Value);
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
            jercEntitySettingsValues.Add("strokeWidth", jercConfig.FirstOrDefault(x => x.Name == "strokeWidth")?.Value);
            jercEntitySettingsValues.Add("strokeColour", jercConfig.FirstOrDefault(x => x.Name == "strokeColour")?.Value);
            jercEntitySettingsValues.Add("strokeAroundLayoutMaterials", jercConfig.FirstOrDefault(x => x.Name == "strokeAroundLayoutMaterials")?.Value);
            jercEntitySettingsValues.Add("strokeAroundRemoveMaterials", jercConfig.FirstOrDefault(x => x.Name == "strokeAroundRemoveMaterials")?.Value);
            jercEntitySettingsValues.Add("strokeAroundEntities", jercConfig.FirstOrDefault(x => x.Name == "strokeAroundEntities")?.Value);
            jercEntitySettingsValues.Add("strokeAroundBrushEntities", jercConfig.FirstOrDefault(x => x.Name == "strokeAroundBrushEntities")?.Value);
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


            // jerc_disp_rotation
            var jercDispRotation = jercEntities.FirstOrDefault(x => x.Body.Any(y => y.Name == "classname" && y.Value == Classnames.JercDispRotation))?.Body;

            if (jercDispRotation != null)
            {
                jercEntitySettingsValues.Add("displacementRotationIds90", jercDispRotation.FirstOrDefault(x => x.Name == "displacementRotationIds90")?.Value);
                jercEntitySettingsValues.Add("displacementRotationIds180", jercDispRotation.FirstOrDefault(x => x.Name == "displacementRotationIds180")?.Value);
                jercEntitySettingsValues.Add("displacementRotationIds270", jercDispRotation.FirstOrDefault(x => x.Name == "displacementRotationIds270")?.Value);
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


        private static IEnumerable<IVNode> GetBrushEntityBrushesByTextureNameIgnoreDoorsAndLaddersAndDangers(IEnumerable<IVNode> allBrushEntities, string textureName)
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


        private static IEnumerable<IVNode> GetEntitiesByClassname(IEnumerable<IVNode> allEntities, string classname, bool onlyIncludeIfInVisgroup)
        {
            if (onlyIncludeIfInVisgroup)
            {
                return (from x in allEntities
                        from y in x.Body
                        where y.Name == "classname"
                        where y.Value.ToLower() == classname.ToLower()
                        from z in x.Body
                        where z.Name == "editor"
                        from a in z.Body
                        where a.Name == "visgroupid"
                        where int.Parse(a.Value, Globalization.Style, Globalization.Culture) == visgroupIdMainVmf ||
                        visgroupIdsByInstanceEntityIds.Values.Any(b => b == int.Parse(a.Value, Globalization.Style, Globalization.Culture))
                        select x).Distinct();
            }
            else
            {
                return (from x in allEntities
                        from y in x.Body
                        where y.Name == "classname"
                        where y.Value.ToLower() == classname.ToLower()
                        select x).Distinct();
            }
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
            Logger.LogMessage("Generating radars");

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
            DrawJercBrushEntities(graphics, levelHeight, bmpRawMaskByNameDictionary, brushEntityBrushSideListById, JercBoxOrderNums.First);
            DrawBrushes(graphics, levelHeight, bmpRawMaskByNameDictionary, allBrushesToDraw, allDisplacementsToDraw, brushEntityBrushSideListById);
            DrawJercBrushEntities(graphics, levelHeight, bmpRawMaskByNameDictionary, brushEntityBrushSideListById, JercBoxOrderNums.AfterJERCBrushesAndDisplacements);
            DrawBrushesTexturedEntities(graphics, levelHeight, bmpRawMaskByNameDictionary, allBrushesToDraw, allDisplacementsToDraw);
            DrawJercBrushEntities(graphics, levelHeight, bmpRawMaskByNameDictionary, brushEntityBrushSideListById, JercBoxOrderNums.AfterJERCBrushesForEntities);
            DrawBrushEntities(graphics, levelHeight, bmpRawMaskByNameDictionary, entityBrushSideListById);
            DrawJercBrushEntities(graphics, levelHeight, bmpRawMaskByNameDictionary, brushEntityBrushSideListById, JercBoxOrderNums.AfterBrushEntities);


            graphics.Save();

            Logger.LogMessage(string.Concat("Generating radar level ", levelHeight.levelNum, " complete"));

            return new RadarLevel(bmp, levelHeight, bmpRawMaskByNameDictionary);
        }


        private static void DrawBrushes(Graphics graphics, LevelHeight levelHeight, Dictionary<string, Bitmap> bmpRawMaskByNameDictionary, AllBrushesToDraw allBrushesToDraw, AllDisplacementsToDraw allDisplacementsToDraw, Dictionary<int, List<EntityBrushSide>> brushEntityBrushSideListById)
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
            foreach (var brushToRender in pathsOrdered)
            {
                DrawFilledPolygonGradient(graphics, brushToRender, true);
            }

            // draw jerc_box brush entities that have the corresponding orderNum set
            DrawJercBrushEntities(graphics, levelHeight, bmpRawMaskByNameDictionary, brushEntityBrushSideListById, JercBoxOrderNums.BetweenPathAndOverlapBrushes);

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


            var entitySidesToDraw = GetBrushEntitiesToDraw(overviewPositionValues, entityBrushSideListById, JercBoxOrderNums.None); // does not give a jercBoxOrderNum value since jerc_box entities are drawn in DrawJercBrushEntities(), not here

            // normal
            foreach (var entitySideToRender in entitySidesToDraw)
            {
                DrawFilledPolygonGradient(graphics, entitySideToRender, true);
            }

            // raw masks
            if (configurationValues.exportRawMasks)
            {
                var entitySides = entitySidesToDraw.Where(x => x.entityType != EntityTypes.None);

                using (Graphics graphicsRawMask = Graphics.FromImage(bmpRawMaskByNameDictionary["buyzones_and_objectives"]))
                {
                    foreach (var entitySide in entitySides)
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


        private static void DrawJercBrushEntities(Graphics graphics, LevelHeight levelHeight, Dictionary<string, Bitmap> bmpRawMaskByNameDictionary, Dictionary<int, List<EntityBrushSide>> brushEntityBrushSideListById, JercBoxOrderNums jercBoxOrderNum)
        {
            var brushEntitySidesToDraw = GetBrushEntitiesToDraw(overviewPositionValues, brushEntityBrushSideListById, jercBoxOrderNum);

            // normal
            foreach (var brushEntitySideToRender in brushEntitySidesToDraw)
            {
                DrawFilledPolygonGradient(graphics, brushEntitySideToRender, true);
            }

            // raw masks
            if (configurationValues.exportRawMasks)
            {
                var entitySides = brushEntitySidesToDraw.Where(x => x.entityType == EntityTypes.JercBox);

                using (Graphics graphicsRawMask = Graphics.FromImage(bmpRawMaskByNameDictionary["jerc_box"]))
                {
                    foreach (var entitySide in entitySides)
                    {
                        DrawFilledPolygonGradient(graphicsRawMask, entitySide, false, levelHeight);
                    }

                    graphicsRawMask.Save();
                }
            }

            // stroke
            if (configurationValues.strokeAroundBrushEntities)
            {
                foreach (var brushEntitySideToRender in brushEntitySidesToDraw)
                {
                    Color colour = Color.White;

                    switch (brushEntitySideToRender.entityType)
                    {
                        case EntityTypes.JercBox:
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
            RotateObjectBeforeDrawingIfSpecified(objectToDraw);

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

                if (!string.IsNullOrWhiteSpace(configurationValues.alternateOutputPath) && Directory.Exists(configurationValues.alternateOutputPath))
                {
                    var outputImageBackgroundLevelsFilepath = string.Concat(configurationValues.alternateOutputPath, mapName, "_background_levels.png");
                    imageFactoryBlurred.Save(outputImageBackgroundLevelsFilepath);
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
                    var outputImageFilepath = string.Concat(outputFilepathPrefix, radarLevelString, "_radar");
                    SaveImage(outputImageFilepath, radarLevel.bmpRadar);
                }

                if (!string.IsNullOrWhiteSpace(configurationValues.alternateOutputPath) && Directory.Exists(configurationValues.alternateOutputPath))
                {
                    var outputImageFilepath = string.Concat(configurationValues.alternateOutputPath, mapName, radarLevelString, "_radar");
                    SaveImage(outputImageFilepath, radarLevel.bmpRadar);
                }
            }
        }


        private static void SaveRadarLevelRawMask(RadarLevel radarLevel, Bitmap bmpRawMask, string rawMaskType)
        {
            bmpRawMask = new Bitmap(bmpRawMask, Sizes.FinalOutputImageResolution, Sizes.FinalOutputImageResolution);

            var radarLevelString = GetRadarLevelString(radarLevel);

            if (!configurationValues.onlyOutputToAlternatePath)
            {
                var outputImageFilepath = string.Concat(outputFilepathPrefix, radarLevelString, "_radar_", rawMaskType, "_mask");
                SaveImage(outputImageFilepath, bmpRawMask, true);
            }

            if (!string.IsNullOrWhiteSpace(configurationValues.alternateOutputPath) && Directory.Exists(configurationValues.alternateOutputPath))
            {
                var outputImageFilepath = string.Concat(configurationValues.alternateOutputPath, mapName, radarLevelString, "_radar_", rawMaskType, "_mask");
                SaveImage(outputImageFilepath, bmpRawMask, true);
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


        private static List<BrushVolume> GetBrushVolumeListWithinLevelHeight(LevelHeight levelHeight, List<Models.Brush> brushList, JercTypes jercType)
        {
            var brushVolumeList = new List<BrushVolume>();

            foreach (var brush in brushList)
            {
                var brushVolumeUnchecked = GetBrushVolume(brush, jercType);

                // may appear on more than 1 level if their brushes span across level dividers or touch edges ?
                if (brushVolumeUnchecked.brushSides.SelectMany(a => a.vertices).All(a => a.z >= levelHeight.zMinForRadar) &&
                    brushVolumeUnchecked.brushSides.SelectMany(a => a.vertices).All(a => a.z <= levelHeight.zMaxForRadar)
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
                if (GameConfigurationValues.isVanillaHammer)
                {
                    VanillaHammerVmfFixer.CalculateVerticesPlusForAllBrushSides(brush.side);
                }

                var brushSideNew = new BrushSide(brush.id);
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
                var brushSideNew = new BrushSide(brushId);
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
                    var entityBrushSide = new EntityBrushSide();
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
                    var entityBrushSide = new EntityBrushSide
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
                    brushesToDraw.Add(new ObjectToDraw(configurationValues, brushSidesListById.Keys.ElementAt(i), brushSideCenterOffset, verticesOffsetsToUse, false, brushSide.jercType));
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
                            brushesToDraw.Add(new ObjectToDraw(configurationValues, brushSidesListById.Keys.ElementAt(i), brushCenterOffset, verticesOffsetsToUse, true, brushSide.jercType));
                        }
                    }
                }
            }

            return brushesToDraw;
        }


        private static List<ObjectToDraw> GetBrushEntitiesToDraw(OverviewPositionValues overviewPositionValues, Dictionary<int, List<EntityBrushSide>> brushEntityBrushSideListById, JercBoxOrderNums jercBoxOrderNum)
        {
            var brushEntitiesToDraw = new List<ObjectToDraw>();

            for (int i = 0; i < brushEntityBrushSideListById.Values.Count(); i++)
            {
                var brushEntityBrushSideByBrush = brushEntityBrushSideListById.Values.ElementAt(i);

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
                    if (brushEntityBrushSide.entityType == EntityTypes.JercBox)
                    {
                        if (brushEntityBrushSide.orderNum == (int)jercBoxOrderNum) // ignore any that are being drawn at a different order num
                        {
                            brushEntitiesToDraw.Add(new ObjectToDraw(configurationValues, brushEntityBrushSideListById.Keys.ElementAt(i), brushSideCenterOffset, verticesOffsetsToUse, false, brushEntityBrushSide.entityType, brushEntityBrushSide.rendercolor, brushEntityBrushSide.colourStroke, brushEntityBrushSide.strokeWidth));
                        }
                    }
                    else
                    {
                        brushEntitiesToDraw.Add(new ObjectToDraw(configurationValues, brushEntityBrushSideListById.Keys.ElementAt(i), brushSideCenterOffset, verticesOffsetsToUse, false, brushEntityBrushSide.entityType));
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
            RotateObjectBeforeDrawingIfSpecified(objectToDraw);

            // Make the points for a polygon.
            var vertices = objectToDraw.verticesToDraw.Select(x => x.vertices).ToList();

            // remove duplicate point positions (this can be caused by vertical brush sides, where their X and Y values are the same (Z is not taken into account here))
            vertices = vertices.Distinct().ToList();

            // check there are still more than 2 points
            if (vertices.Count() < 3)
                return;

            // check there are more than 1 value on each axis
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


        private static void SaveImage(string filepath, Bitmap bmp, bool forcePngOnly = false)
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
                if (configurationValues.exportDds && !forcePngOnly)
                    bmp.Save(filepath + ".dds");

                if (configurationValues.exportPng || forcePngOnly)
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
                var outputTxtFilepath = string.Concat(outputFilepathPrefix, ".txt");
                SaveOutputTxtFile(outputTxtFilepath, lines);
            }

            if (!string.IsNullOrWhiteSpace(configurationValues.alternateOutputPath) && Directory.Exists(configurationValues.alternateOutputPath))
            {
                var outputTxtFilepath = string.Concat(configurationValues.alternateOutputPath, mapName, ".txt");
                SaveOutputTxtFile(outputTxtFilepath, lines);
            }

            Logger.LogMessage("Generating txt complete");
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

            // ct spawns
            if (vmfRequiredData.brushesCTSpawn.Any() || vmfRequiredData.displacementsCTSpawn.Any())
            {
                var vertices = vmfRequiredData.brushesCTSpawn.Concat(vmfRequiredData.displacementsCTSpawn).SelectMany(x => x.side.SelectMany(y => y.vertices_plus));

                var xAllValues = vertices.Select(x => x.x);
                var yAllValues = vertices.Select(x => x.y);
                var xAverage = xAllValues.Average();
                var yAverage = yAllValues.Average();
                //var xPercent = Math.Abs((xAverage - (overviewPositionValues.brushVerticesPosMinX + overviewPositionValues.paddingSizeX)) / overviewPositionValues.outputResolution);
                var xPercent = Math.Abs((Math.Abs(Math.Abs(xAverage) - Math.Abs(overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                var yPercent = Math.Abs((Math.Abs(Math.Abs(yAverage) - Math.Abs(overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                CTSpawn_x = FlipOverviewTxtValues(xPercent, true);
                CTSpawn_y = FlipOverviewTxtValues(yPercent, false);
            }
            else if (vmfRequiredData.ctSpawnEntities.Any())
            {
                var origins = vmfRequiredData.ctSpawnEntities.Select(x => new Vertices(x.origin));
                var xPercent = Math.Abs((Math.Abs(Math.Abs(origins.Average(x => x.x)) - Math.Abs(overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                var yPercent = Math.Abs((Math.Abs(Math.Abs(origins.Average(x => x.y)) - Math.Abs(overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

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
                var xPercent = Math.Abs((Math.Abs(Math.Abs(xAverage) - Math.Abs(overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                var yPercent = Math.Abs((Math.Abs(Math.Abs(yAverage) - Math.Abs(overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                TSpawn_x = FlipOverviewTxtValues(xPercent, true);
                TSpawn_y = FlipOverviewTxtValues(yPercent, false);
            }
            else if (vmfRequiredData.tSpawnEntities.Any())
            {
                var origins = vmfRequiredData.tSpawnEntities.Select(x => new Vertices(x.origin));
                var xPercent = Math.Abs((Math.Abs(Math.Abs(origins.Average(x => x.x)) - Math.Abs(overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                var yPercent = Math.Abs((Math.Abs(Math.Abs(origins.Average(x => x.y)) - Math.Abs(overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

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
                var xPercent1 = Math.Abs((Math.Abs(Math.Abs(xAverage1) - Math.Abs(overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                var yPercent1 = Math.Abs((Math.Abs(Math.Abs(yAverage1) - Math.Abs(overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                bombA_x = FlipOverviewTxtValues(xPercent1, true);
                bombA_y = FlipOverviewTxtValues(yPercent1, false);

                // bombsite b
                var vertices2 = vmfRequiredData.brushesBombsiteB.Any() || vmfRequiredData.displacementsBombsiteB.Any()
                    ? vmfRequiredData.brushesBombsiteB.Concat(vmfRequiredData.displacementsBombsiteB).SelectMany(x => x.side.SelectMany(y => y.vertices_plus))
                    : null;

                var xAllValues2 = vertices2.Select(x => x.x);
                var yAllValues2 = vertices2.Select(x => x.y);
                var xAverage2 = xAllValues2.Average();
                var yAverage2 = yAllValues2.Average();
                var xPercent2 = Math.Abs((Math.Abs(Math.Abs(xAverage2) - Math.Abs(overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                var yPercent2 = Math.Abs((Math.Abs(Math.Abs(yAverage2) - Math.Abs(overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                bombB_x = FlipOverviewTxtValues(xPercent2, true);
                bombB_y = FlipOverviewTxtValues(yPercent2, false);
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
                var xPercent1 = Math.Abs((Math.Abs(Math.Abs(xAverage1) - Math.Abs(overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                var yPercent1 = Math.Abs((Math.Abs(Math.Abs(yAverage1) - Math.Abs(overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

                bombA_x = FlipOverviewTxtValues(xPercent1, true);
                bombA_y = FlipOverviewTxtValues(yPercent1, false);

                if (vmfRequiredData.bombsiteBrushEntities.Count() > 1)
                {
                    var xAllValues2 = bombsiteEntities.Skip(1).FirstOrDefault().brushes.SelectMany(x => x.side.SelectMany(y => y.vertices_plus.Select(x => x.x)));
                    var yAllValues2 = bombsiteEntities.Skip(1).FirstOrDefault().brushes.SelectMany(x => x.side.SelectMany(y => y.vertices_plus.Select(x => x.y)));
                    var xAverage2 = xAllValues2.Average();
                    var yAverage2 = yAllValues2.Average();
                    var xPercent2 = Math.Abs((Math.Abs(Math.Abs(xAverage2) - Math.Abs(overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                    var yPercent2 = Math.Abs((Math.Abs(Math.Abs(yAverage2) - Math.Abs(overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

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
                    var xPercent = Math.Abs((Math.Abs(Math.Abs(xAverage) - Math.Abs(overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                    var yPercent = Math.Abs((Math.Abs(Math.Abs(yAverage) - Math.Abs(overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

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
                    var xPercent = Math.Abs((Math.Abs(Math.Abs(origin.x) - Math.Abs(overviewPositionValues.brushVerticesPosMinX)) - (overviewPositionValues.radarSizeMultiplierChangeAmountWidth / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideX;
                    var yPercent = Math.Abs((Math.Abs(Math.Abs(origin.y) - Math.Abs(overviewPositionValues.brushVerticesPosMinY)) - (overviewPositionValues.radarSizeMultiplierChangeAmountHeight / 2)) / overviewPositionValues.outputResolution) + paddingPercentageEachSideY;

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
