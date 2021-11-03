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

        private static JercConfigValues jercConfigValues;

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
                Logger.LogError("No jerc_config entity found, aborting");
                return;
            }

            if (debugging)
            {
                Logger.LogDebugInfo("Setting alternateOutputPath to empty string");

                jercConfigValues.alternateOutputPath = string.Empty;
            }

            // calculate vertices_plus for every brush side for vanilla hammer vmfs, as hammer++ adds vertices itself when saving a vmf
            if (GameConfigurationValues.isVanillaHammer)
            {
                CalculateVerticesPlusForAllBrushSides();
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

            if (jercConfigValues.exportTxt)
                GenerateTxt(levelHeights);
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
                var filepath = string.Concat(GameConfigurationValues.vmfFilepathDirectory, @"\", instance.file);

                if (!File.Exists(filepath))
                    continue;

                var lines = File.ReadAllLines(filepath);

                var newVmf = new VMF(lines);

                if (newVmf == null)
                    continue;

                // correct origins and angles
                foreach (var entity in newVmf.Body.Where(x => x.Name == "entity"))
                {
                    // entity id is not changed
                    // brush rotation is not changed

                    var originIVNode = entity.Body.FirstOrDefault(x => x.Name == "origin");
                    if (originIVNode != null)
                        MoveAndRotateVertices(instance, originIVNode);

                    var allBrushSidesInEntity = entity.Body.Where(x => x.Name == "solid").SelectMany(x => x.Body.Where(y => y.Name == "side").Select(y => y.Body)).ToList();
                    MoveAndRotateAllBrushSides(instance, allBrushSidesInEntity);
                }

                var allWorldBrushSides = newVmf.World.Body.Where(x => x.Name == "solid").SelectMany(x => x.Body.Where(y => y.Name == "side").Select(y => y.Body)).ToList();
                MoveAndRotateAllBrushSides(instance, allWorldBrushSides);

                instanceEntityIdsByVmf.Add(newVmf, instance.id);
            }

            Logger.LogMessage(string.Concat(instanceEntityIdsByVmf.Count(), " instance", instanceEntityIdsByVmf.Count() == 1 ? string.Empty : "s", " successfully parsed."));
        }


        private static void MoveAndRotateAllBrushSides(FuncInstance instance, List<IList<IVNode>> brushSideIVNodeList)
        {
            foreach (var brushSide in brushSideIVNodeList)
            {
                // brush id is not changed
                // rotation is not changed

                foreach (var verticesPlusIVNode in brushSide.Where(x => x.Name == "vertices_plus").SelectMany(x => x.Body))
                {
                    MoveAndRotateVertices(instance, verticesPlusIVNode);
                }
            }
        }


        private static void MoveAndRotateVertices(FuncInstance instance, IVNode ivNode)
        {
            ivNode.Value = MergeVerticesToString(instance.origin, ivNode.Value); // removes the offset that being in an instances causes
            ivNode.Value = GetRotatedVerticesNewPositionAsString(new Vertices(ivNode.Value), instance.origin, instance.angles.yaw); // removes the rotation that being in an instances causes
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
            Logger.LogMessage("Getting required data from the vmf and instances");

            // main vmf contents
            var allWorldBrushes = vmf.World.Body.Where(x => x.Name == "solid");
            var allEntities = vmf.Body.Where(x => x.Name == "entity");

            // instances contents
            if (instanceEntityIdsByVmf != null && instanceEntityIdsByVmf.Any())
            {
                foreach (var instance in instanceEntityIdsByVmf.Keys)
                {
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

            if (jercConfigEntities == null || !jercConfigEntities.Any())
                return null;

            var allJercEntities = jercConfigEntities.Concat(jercDividerEntities).Concat(jercFloorEntities).Concat(jercCeilingEntities);

            jercConfigValues = new JercConfigValues(GetSettingsValuesFromJercEntities(allJercEntities), jercDividerEntities.Count());

            Logger.LogMessage("Retrieved data from the vmf and instances");
            Logger.LogNewLine();

            return new VmfRequiredData(
                brushesRemove, brushesPath, brushesCover, brushesOverlap, brushesDoor, brushesLadder, brushesDanger,
                brushesBuyzone, brushesBombsiteA, brushesBombsiteB, brushesRescueZone, brushesHostage, brushesTSpawn, brushesCTSpawn,
                displacementsRemove, displacementsPath, displacementsCover, displacementsOverlap, displacementsDoor, displacementsLadder, displacementsDanger,
                displacementsBuyzone, displacementsBombsiteA, displacementsBombsiteB, displacementsRescueZone, displacementsHostage, displacementsTSpawn, displacementsCTSpawn,
                buyzoneBrushEntities, bombsiteBrushEntities, rescueZoneBrushEntities, hostageEntities, ctSpawnEntities, tSpawnEntities,
                brushesRemoveBrushEntities, brushesPathBrushEntities, brushesCoverBrushEntities, brushesOverlapBrushEntities, brushesDoorBrushEntities, brushesLadderBrushEntities, brushesDangerBrushEntities,
                brushesBuyzoneBrushEntities, brushesBombsiteABrushEntities, brushesBombsiteBBrushEntities, brushesRescueZoneBrushEntities, brushesHostageBrushEntities, brushesTSpawnBrushEntities, brushesCTSpawnBrushEntities,
                jercBoxBrushEntities,
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


        private static void CalculateVerticesPlusForAllBrushSides()
        {
            var allBrushesAndDisplacementsExceptIgnore = vmfRequiredData.brushesRemove
                .Concat(vmfRequiredData.brushesPath)
                .Concat(vmfRequiredData.brushesCover)
                .Concat(vmfRequiredData.brushesOverlap)
                .Concat(vmfRequiredData.brushesDoor)
                .Concat(vmfRequiredData.brushesLadder)
                .Concat(vmfRequiredData.brushesDanger)
                .Concat(vmfRequiredData.brushesBuyzone)
                .Concat(vmfRequiredData.brushesBombsiteA)
                .Concat(vmfRequiredData.brushesBombsiteB)
                .Concat(vmfRequiredData.brushesRescueZone)
                .Concat(vmfRequiredData.brushesHostage)
                .Concat(vmfRequiredData.brushesTSpawn)
                .Concat(vmfRequiredData.brushesCTSpawn)
                .Concat(vmfRequiredData.displacementsRemove)
                .Concat(vmfRequiredData.displacementsPath)
                .Concat(vmfRequiredData.displacementsCover)
                .Concat(vmfRequiredData.displacementsOverlap)
                .Concat(vmfRequiredData.displacementsDoor)
                .Concat(vmfRequiredData.displacementsLadder)
                .Concat(vmfRequiredData.displacementsDanger)
                .Concat(vmfRequiredData.displacementsBuyzone)
                .Concat(vmfRequiredData.displacementsBombsiteA)
                .Concat(vmfRequiredData.displacementsBombsiteB)
                .Concat(vmfRequiredData.displacementsRescueZone)
                .Concat(vmfRequiredData.displacementsHostage)
                .Concat(vmfRequiredData.displacementsTSpawn)
                .Concat(vmfRequiredData.displacementsCTSpawn);

            if (allBrushesAndDisplacementsExceptIgnore == null || allBrushesAndDisplacementsExceptIgnore.Count() == 0)
                return;

            foreach (var brush in allBrushesAndDisplacementsExceptIgnore)
            {
                var planesVerticesList = brush.side.Select(x => x.plane.Replace("(", string.Empty).Replace(")", string.Empty).Split(" ").ToArray()).ToList();

                var squarePlaneList = new List<SquarePlane>();
                for (int i = 0; i < planesVerticesList.Count(); i++)
                {
                    var vertices1 = new Vertices(planesVerticesList[i][0] + " " + planesVerticesList[i][1] + " " + planesVerticesList[i][2]); // 3 vertices per plane in vanilla hammer
                    var vertices2 = new Vertices(planesVerticesList[i][3] + " " + planesVerticesList[i][4] + " " + planesVerticesList[i][5]); // 3 vertices per plane in vanilla hammer
                    var vertices3 = new Vertices(planesVerticesList[i][6] + " " + planesVerticesList[i][7] + " " + planesVerticesList[i][8]); // 3 vertices per plane in vanilla hammer

                    squarePlaneList.Add(new SquarePlane(brush.side.ElementAt(i).brushId, brush.side.ElementAt(i).id, vertices1, vertices2, vertices3));
                }

                brush.SquarePlaneList = squarePlaneList;
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

            var sizeX = (maxX - minX) / jercConfigValues.radarSizeMultiplier;
            var sizeY = (maxY - minY) / jercConfigValues.radarSizeMultiplier;

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
                var zMaxForRadar = i == (numOfOverviewLevels - 1) ? vmfRequiredData.GetHighestVerticesZ() : (float)(new Vertices(jercDividerEntities.ElementAt(i).origin).z);

                var jercFloorEntitiesInsideLevel = jercConfigValues.exportRadarAsSeparateLevels && jercConfigValues.useSeparateGradientEachLevel
                    ? vmfRequiredData.jercFloorEntities.Where(x => new Vertices(x.origin).z >= zMinForRadar && new Vertices(x.origin).z < zMaxForRadar).ToList()
                    : vmfRequiredData.jercFloorEntities;
                var zMinForGradient = jercFloorEntitiesInsideLevel.Any() ? jercFloorEntitiesInsideLevel.OrderBy(x => new Vertices(x.origin).z).Select(x => (float)(new Vertices(x.origin).z)).FirstOrDefault() : zMinForRadar; // takes the lowest (first) in the level if there are more than one

                var jercCeilingEntitiesInsideLevel = jercConfigValues.exportRadarAsSeparateLevels && jercConfigValues.useSeparateGradientEachLevel
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
            if (jercConfigValues.exportRawMasks)
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
            var brushPathSideList = GetBrushSideListWithinLevelHeight(levelHeight, vmfRequiredData.brushesSidesPath, JercTypes.Path);
            var displacementPathSideList = GetBrushSideListWithinLevelHeight(levelHeight, vmfRequiredData.displacementsSidesPath, JercTypes.Path);

            var brushOverlapSideList = GetBrushSideListWithinLevelHeight(levelHeight, vmfRequiredData.brushesSidesOverlap, JercTypes.Overlap);
            var displacementOverlapSideList = GetBrushSideListWithinLevelHeight(levelHeight, vmfRequiredData.displacementsSidesOverlap, JercTypes.Overlap);

            // get all brushes and displacements to draw (brushes)
            var brushesToDrawPath = GetBrushesToDraw(boundingBox, brushPathSideList);
            var displacementsToDrawPath = GetDisplacementsToDraw(boundingBox, displacementPathSideList);

            var brushesToDrawOverlap = GetBrushesToDraw(boundingBox, brushOverlapSideList);
            var displacementsToDrawOverlap = GetDisplacementsToDraw(boundingBox, displacementOverlapSideList);

            var brushesToDrawCover = GetBrushesToDraw(boundingBox, brushCoverList.SelectMany(x => x.brushSides).ToList());
            var displacementsToDrawCover = GetDisplacementsToDraw(boundingBox, displacementCoverList.SelectMany(x => x.brushSides).ToList());

            var brushesToDrawDoor = GetBrushesToDraw(boundingBox, brushDoorList.SelectMany(x => x.brushSides).ToList());
            var displacementsToDrawDoor = GetDisplacementsToDraw(boundingBox, displacementDoorList.SelectMany(x => x.brushSides).ToList());

            var brushesToDrawLadder = GetBrushesToDraw(boundingBox, brushLadderList.SelectMany(x => x.brushSides).ToList());
            var displacementsToDrawLadder = GetDisplacementsToDraw(boundingBox, displacementLadderList.SelectMany(x => x.brushSides).ToList());

            var brushesToDrawDanger = GetBrushesToDraw(boundingBox, brushDangerList.SelectMany(x => x.brushSides).ToList());
            var displacementsToDrawDanger = GetDisplacementsToDraw(boundingBox, displacementDangerList.SelectMany(x => x.brushSides).ToList());

            var brushesToDrawBuyzone = GetBrushesToDraw(boundingBox, brushBuyzoneList.SelectMany(x => x.brushSides).ToList());
            var displacementsToDrawBuyzone = GetDisplacementsToDraw(boundingBox, displacementBuyzoneList.SelectMany(x => x.brushSides).ToList());

            var brushesToDrawBombsiteA = GetBrushesToDraw(boundingBox, brushBombsiteAList.SelectMany(x => x.brushSides).ToList());
            var displacementsToDrawBombsiteA = GetDisplacementsToDraw(boundingBox, displacementBombsiteAList.SelectMany(x => x.brushSides).ToList());

            var brushesToDrawBombsiteB = GetBrushesToDraw(boundingBox, brushBombsiteBList.SelectMany(x => x.brushSides).ToList());
            var displacementsToDrawBombsiteB = GetDisplacementsToDraw(boundingBox, displacementBombsiteBList.SelectMany(x => x.brushSides).ToList());

            var brushesToDrawRescueZone = GetBrushesToDraw(boundingBox, brushRescueZoneList.SelectMany(x => x.brushSides).ToList());
            var displacementsToDrawRescueZone = GetDisplacementsToDraw(boundingBox, displacementRescueZoneList.SelectMany(x => x.brushSides).ToList());

            // get all entity sides to draw
            var entityBrushSideListById = GetEntityBrushSideListWithinLevelHeight(levelHeight);

            // get all brush entity sides to draw
            var brushEntityBrushSideListById = GetBrushEntityBrushSideListWithinLevelHeight(levelHeight);


            // add remove stuff first to set to graphics' clip
            AddRemoveRegion(bmp, graphics, brushRemoveList);
            AddRemoveRegion(bmp, graphics, displacementRemoveList);


            // brush stuff
            var pathsOrdered = brushesToDrawPath.Concat(displacementsToDrawPath).OrderBy(x => x.zAxisAverage);
            var overlapsOrdered = brushesToDrawOverlap.Concat(displacementsToDrawOverlap).OrderBy(x => x.zAxisAverage);
            var coversOrdered = brushesToDrawCover.Concat(displacementsToDrawCover).OrderBy(x => x.zAxisAverage);
            var doorsOrdered = brushesToDrawDoor.Concat(displacementsToDrawDoor).OrderBy(x => x.zAxisAverage);
            var laddersOrdered = brushesToDrawLadder.Concat(displacementsToDrawLadder).OrderBy(x => x.zAxisAverage);
            var dangersOrdered = brushesToDrawDanger.Concat(displacementsToDrawDanger).OrderBy(x => x.zAxisAverage);

            var coversAndOverlapsOrdered = overlapsOrdered.Concat(coversOrdered).OrderBy(x => x.zAxisAverage);

            // path and overlap brush stuff (for stroke)
            if (jercConfigValues.strokeAroundLayoutMaterials)
            {
                foreach (var brushToRender in pathsOrdered.Concat(overlapsOrdered).OrderBy(x => x.zAxisAverage))
                {
                    DrawStroke(graphics, brushToRender, Colours.ColourBrushesStroke(jercConfigValues.strokeColour));
                }
            }

            // path brush stuff
            foreach (var brushToRender in pathsOrdered)
            {
                DrawFilledPolygonGradient(graphics, brushToRender, true);
            }

            // cover and overlap, door, ladder, danger brush stuff
            foreach (var brushToRender in coversAndOverlapsOrdered.Concat(doorsOrdered).Concat(laddersOrdered).Concat(dangersOrdered))
            {
                DrawFilledPolygonGradient(graphics, brushToRender, false);
            }

            // raw masks
            if (jercConfigValues.exportRawMasks)
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
            }

            // brush entity texture stuff (in game)
            var allObjectiveAndBuyzoneBrushes = brushesToDrawBuyzone.Concat(displacementsToDrawBuyzone)
                .Concat(brushesToDrawBombsiteA).Concat(displacementsToDrawBombsiteA)
                .Concat(brushesToDrawBombsiteB).Concat(displacementsToDrawBombsiteB)
                .Concat(brushesToDrawRescueZone).Concat(displacementsToDrawRescueZone);

            // stroke
            if (jercConfigValues.strokeAroundBrushEntities)
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
            if (jercConfigValues.exportRawMasks)
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



            // reset the clip so that entity brushes can render anywhere
            ////graphics.ResetClip();



            // brush entities next
            var entitySidesToDraw = GetBrushEntitiesToDraw(overviewPositionValues, entityBrushSideListById);

            // normal
            foreach (var entitySideToRender in entitySidesToDraw)
            {
                DrawFilledPolygonGradient(graphics, entitySideToRender, true);
            }

            // raw masks
            if (jercConfigValues.exportRawMasks)
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
            if (jercConfigValues.strokeAroundEntities)
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


            // brush entities (JERC)
            var brushEntitySidesToDraw = GetBrushEntitiesToDraw(overviewPositionValues, brushEntityBrushSideListById);

            // normal
            foreach (var brushEntitySideToRender in brushEntitySidesToDraw)
            {
                DrawFilledPolygonGradient(graphics, brushEntitySideToRender, true);
            }

            // stroke
            if (jercConfigValues.strokeAroundBrushEntities)
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

            graphics.Save();

            Logger.LogMessage(string.Concat("Generating radar level ", levelHeight.levelNum, " complete"));

            return new RadarLevel(bmp, levelHeight, bmpRawMaskByNameDictionary);
        }


        private static Dictionary<string, Bitmap> GetNewBmpRawMaskByNameDictionary()
        {
            return new Dictionary<string, Bitmap>()
            {
                { "path", new Bitmap(overviewPositionValues.outputResolution, overviewPositionValues.outputResolution) },
                { "cover", new Bitmap(overviewPositionValues.outputResolution, overviewPositionValues.outputResolution) },
                { "overlap", new Bitmap(overviewPositionValues.outputResolution, overviewPositionValues.outputResolution) },
                { "buyzones_and_objectives", new Bitmap(overviewPositionValues.outputResolution, overviewPositionValues.outputResolution) },
            };
        }


        private static void DrawStroke(Graphics graphics, ObjectToDraw objectToDraw, Color colourStroke, int? strokeWidthOverride = null)
        {
            var strokeSolidBrush = new SolidBrush(Color.Transparent);
            var strokePen = new Pen(colourStroke);
            strokePen.Width *= strokeWidthOverride == null ? jercConfigValues.strokeWidth : (int)strokeWidthOverride;

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
            {
                if (!jercConfigValues.onlyOutputToAlternatePath)
                {
                    imageFactoryBlurred.Save(outputImageBackgroundLevelsFilepath);
                }

                if (!string.IsNullOrWhiteSpace(jercConfigValues.alternateOutputPath) && Directory.Exists(jercConfigValues.alternateOutputPath))
                {
                    var outputImageBackgroundLevelsFilepath = string.Concat(jercConfigValues.alternateOutputPath, mapName, "_background_levels.png");
                    imageFactoryBlurred.Save(outputImageBackgroundLevelsFilepath);
                }
            }

            imageFactoryBlurred.Dispose();

            return backgroundBmp;
        }


        private static void SaveRadarLevel(RadarLevel radarLevel)
        {
            radarLevel.bmpRadar = new Bitmap(radarLevel.bmpRadar, Sizes.FinalOutputImageResolution, Sizes.FinalOutputImageResolution);

            if (!string.IsNullOrWhiteSpace(jercConfigValues.backgroundFilename))
                radarLevel.bmpRadar = AddBackgroundImage(radarLevel);

            var radarLevelString = GetRadarLevelString(radarLevel);

            if (jercConfigValues.exportDds || jercConfigValues.exportPng)
            {
                if (!jercConfigValues.onlyOutputToAlternatePath)
                {
                    var outputImageFilepath = string.Concat(outputFilepathPrefix, radarLevelString, "_radar");
                    SaveImage(outputImageFilepath, radarLevel.bmpRadar);
                }

                if (!string.IsNullOrWhiteSpace(jercConfigValues.alternateOutputPath) && Directory.Exists(jercConfigValues.alternateOutputPath))
                {
                    var outputImageFilepath = string.Concat(jercConfigValues.alternateOutputPath, mapName, radarLevelString, "_radar");
                    SaveImage(outputImageFilepath, radarLevel.bmpRadar);
                }
            }
        }


        private static void SaveRadarLevelRawMask(RadarLevel radarLevel, Bitmap bmpRawMask, string rawMaskType)
        {
            bmpRawMask = new Bitmap(bmpRawMask, Sizes.FinalOutputImageResolution, Sizes.FinalOutputImageResolution);

            var radarLevelString = GetRadarLevelString(radarLevel);

            if (!jercConfigValues.onlyOutputToAlternatePath)
            {
                var outputImageFilepath = string.Concat(outputFilepathPrefix, radarLevelString, "_radar_", rawMaskType, "_mask");
                SaveImage(outputImageFilepath, bmpRawMask, true);
            }

            if (!string.IsNullOrWhiteSpace(jercConfigValues.alternateOutputPath) && Directory.Exists(jercConfigValues.alternateOutputPath))
            {
                var outputImageFilepath = string.Concat(jercConfigValues.alternateOutputPath, mapName, radarLevelString, "_radar_", rawMaskType, "_mask");
                SaveImage(outputImageFilepath, bmpRawMask, true);
            }
        }


        private static string GetRadarLevelString(RadarLevel radarLevel)
        {
            return radarLevel.levelHeight.levelName.ToLower() == "default" ? string.Empty : string.Concat("_", radarLevel.levelHeight.levelName.ToLower());
        }


        private static Bitmap AddBackgroundImage(RadarLevel radarLevel)
        {
            var backgroundImageFilepath = string.Concat(backgroundImagesDirectory, jercConfigValues.backgroundFilename, ".bmp");

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
            /* use this code if another new JERC brush entity is added in future */


            /*
            var brushEntityBrushSideListById = new Dictionary<int, List<EntityBrushSide>>();

            var brushEntityJercBoxVerticesListById = GetBrushEntityBrushSideList(vmfRequiredData.entitiesSidesByEntityJercBoxId, EntityTypes.JercBox);

            if (brushEntityJercBoxVerticesListById != null && brushEntityJercBoxVerticesListById.Any())
            {
                foreach (var list in brushEntityJercBoxVerticesListById)
                {
                    brushEntityBrushSideListById.Add(list.Key, list.Value);
                }
            }

            return brushEntityBrushSideListById;
            */

            return GetBrushEntityBrushSideList(vmfRequiredData.entitiesSidesByEntityJercBoxId, EntityTypes.JercBox);
        }


        private static List<BrushVolume> GetBrushVolumeListWithinLevelHeight(LevelHeight levelHeight, List<Models.Brush> brushList, JercTypes jercType)
        {
            // may appear on more than 1 level if their brushes span across level dividers or touch edges ?
            return GetBrushVolumeList(brushList, jercType).Where(x =>
                x.brushSides.SelectMany(y => y.vertices).All(y => y.z >= levelHeight.zMinForRadar) &&
                x.brushSides.SelectMany(y => y.vertices).All(y => y.z <= levelHeight.zMaxForRadar)
            ).ToList();
        }


        private static List<BrushSide> GetBrushSideListWithinLevelHeight(LevelHeight levelHeight, List<Side> sideList, JercTypes jercType)
        {
            // may appear on more than 1 level if their brushes span across level dividers or touch edges ?
            return GetBrushSideList(sideList, jercType).Where(x =>
                x.vertices.All(y => y.z >= levelHeight.zMinForRadar) &&
                x.vertices.All(y => y.z <= levelHeight.zMaxForRadar)
            ).ToList();
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
                )) {
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
                )) {
                    brushEntityBrushSideListById.Add(brushEntityBrushSideById.Key, brushEntityBrushSideById.Value);
                }
            }

            return brushEntityBrushSideListById;
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
                        brushSideNew.vertices.Add(new Vertices(vertices.x / Sizes.SizeReductionMultiplier, vertices.y / Sizes.SizeReductionMultiplier, (float)vertices.z));
                        brushSideNew.jercType = jercType;
                    }

                    // add displacement stuff if the brush is a displacement
                    if (brushSide.isDisplacement)
                    {
                        brushSideNew.displacementStuff = new DisplacementStuff(brushSide.dispinfo, brushSideNew.vertices); //// brushSide.vertices_plus ??
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
                var brushSideNew = new BrushSide();
                for (int i = 0; i < side.vertices_plus.Count(); i++)
                {
                    var vert = side.vertices_plus[i];

                    brushSideNew.vertices.Add(new Vertices(vert.x / Sizes.SizeReductionMultiplier, vert.y / Sizes.SizeReductionMultiplier, (float)vert.z));
                    brushSideNew.jercType = jercType;
                }

                // add displacement stuff if the brush is a displacement
                if (side.isDisplacement)
                {
                    brushSideNew.displacementStuff = new DisplacementStuff(side.dispinfo, brushSideNew.vertices); //// brushSide.vertices_plus ??
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

            if (graphicsPath.GetBounds().Width == 0 || graphicsPath.GetBounds().Height == 0)
                return;

            // add stroke
            if (jercConfigValues.strokeAroundRemoveMaterials)
            {
                // adds the stroke to the outside of the brush instead of the inside
                var averagePointsX = graphicsPath.PathPoints.Average(x => x.X);
                var averagePointsY = graphicsPath.PathPoints.Average(x => x.Y);

                // scale
                var scaleX = (jercConfigValues.strokeWidth / 2 / graphicsPath.GetBounds().Width) + 1;
                var scaleY = (jercConfigValues.strokeWidth / 2 / graphicsPath.GetBounds().Height) + 1;

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
                var strokePen = new Pen(Colours.ColourRemoveStroke(jercConfigValues.strokeColour), jercConfigValues.strokeWidth / 2);
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
                        JercTypes.Overlap => Colours.ColourBrush(jercConfigValues.overlapColourLow, jercConfigValues.overlapColourHigh, percentageAboveMin, jercConfigValues.overlapAlpha),
                        JercTypes.Door => jercConfigValues.doorColour,
                        JercTypes.Ladder => jercConfigValues.ladderColour,
                        JercTypes.Danger => Colours.ColourDanger(jercConfigValues.dangerColour, jercConfigValues.dangerAlpha),
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

                brushesToDraw.Add(new ObjectToDraw(verticesOffsetsToUse, false, brushSide.jercType));
            }

            return brushesToDraw;
        }


        private static List<ObjectToDraw> GetDisplacementsToDraw(BoundingBox boundingBox, List<BrushSide> brushSidesList)
        {
            var brushesToDraw = new List<ObjectToDraw>();

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
                                JercTypes.Path => Colours.ColourBrush(jercConfigValues.pathColourLow, jercConfigValues.pathColourHigh, percentageAboveMin),
                                JercTypes.Cover => Colours.ColourBrush(jercConfigValues.coverColourLow, jercConfigValues.coverColourHigh, percentageAboveMin),
                                JercTypes.Overlap => Colours.ColourBrush(jercConfigValues.overlapColourLow, jercConfigValues.overlapColourHigh, percentageAboveMin, jercConfigValues.overlapAlpha),
                                JercTypes.Door => jercConfigValues.doorColour,
                                JercTypes.Ladder => jercConfigValues.ladderColour,
                                JercTypes.Danger => Colours.ColourDanger(jercConfigValues.dangerColour, jercConfigValues.dangerAlpha),
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

                        brushesToDraw.Add(new ObjectToDraw(verticesOffsetsToUse, true, brushSide.jercType));
                    }
                }
            }

            return brushesToDraw;
        }


        private static List<ObjectToDraw> GetBrushEntitiesToDraw(OverviewPositionValues overviewPositionValues, Dictionary<int, List<EntityBrushSide>> brushEntityBrushSideListById)
        {
            var brushEntitiesToDraw = new List<ObjectToDraw>();

            foreach (var brushEntityBrushSideByBrush in brushEntityBrushSideListById.Values)
            {
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

                    if (brushEntityBrushSide.entityType == EntityTypes.JercBox)
                    {
                        brushEntitiesToDraw.Add(new ObjectToDraw(verticesOffsetsToUse, false, brushEntityBrushSide.entityType, brushEntityBrushSide.rendercolor, brushEntityBrushSide.colourStroke, brushEntityBrushSide.strokeWidth));
                    }
                    else
                    {
                        brushEntitiesToDraw.Add(new ObjectToDraw(verticesOffsetsToUse, false, brushEntityBrushSide.entityType));
                    }
                }
            }

            return brushEntitiesToDraw;
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
                                    //var heightAboveMin = vertices.Min(x => x.z) - levelHeightOverride.zMinForRadarGradient;
                                    var heightAboveMin = vertices.Average(x => x.z) - levelHeightOverride.zMinForRadarGradient;
                                    var percentageAboveMin = (float)((Math.Ceiling(Convert.ToDouble(heightAboveMin)) / (levelHeightOverride.zMaxForRadarGradient - levelHeightOverride.zMinForRadarGradient)));
                                    colourUsing = Colours.GetGreyscaleGradient(percentageAboveMin * 255);
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
                        var heightAboveMin = vertices.Min(x => x.z) - levelHeightOverride.zMinForRadarGradient;
                        var percentageAboveMin = (float)((Math.Ceiling(Convert.ToDouble(heightAboveMin)) / (levelHeightOverride.zMaxForRadarGradient - levelHeightOverride.zMinForRadarGradient)));
                        colourUsing = Colours.GetGreyscaleGradient(percentageAboveMin * 255);
                    }

                    // surrounding vertices colours
                    if (objectToDraw.jercType != null && (objectToDraw.jercType == JercTypes.Path || objectToDraw.jercType == JercTypes.Cover || objectToDraw.jercType == JercTypes.Overlap))
                    {
                        var colours = new List<Color>();
                        for (int i = 0; i < verticesArray.Length; i++)
                        {
                            var heightAboveMin = objectToDraw.verticesToDraw[i].vertices.z - levelHeightOverride.zMinForRadarGradient;
                            var percentageAboveMin = (float)((Math.Ceiling(Convert.ToDouble(heightAboveMin)) / (levelHeightOverride.zMaxForRadarGradient - levelHeightOverride.zMinForRadarGradient)));
                            colours.Add(Colours.GetGreyscaleGradient(percentageAboveMin * 255));
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

                        using (LinearGradientBrush linearBrush = new LinearGradientBrush(pointToDraw1, pointToDraw2, verticesToDraw1.colour, verticesToDraw2.colour))
                        {
                            Pen pen = new Pen(linearBrush);

                            graphics.DrawLine(pen, pointToDraw1, pointToDraw2);

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
                if (jercConfigValues.exportDds && !forcePngOnly)
                    bmp.Save(filepath + ".dds");

                if (jercConfigValues.exportPng || forcePngOnly)
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
