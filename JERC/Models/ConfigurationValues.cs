using JERC.Constants;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Models
{
    public class ConfigurationValues
    {
        // jerc_config
        public string alternateOutputPath;
        public bool onlyOutputToAlternatePath;
        public bool exportRadarAsSeparateLevels;
        public bool useSeparateGradientEachLevel;
        public bool ignoreDisplacementXYChanges;
        public bool rotateCutDispsAutomatic;
        public string backgroundFilename;
        public float radarSizeMultiplier;
        public int overlapAlpha;
        public int dangerAlpha;
        public Color pathColourHigh;
        public Color pathColourLow;
        public Color overlapColourHigh;
        public Color overlapColourLow;
        public Color coverColourHigh;
        public Color coverColourLow;
        public Color doorColour;
        public Color ladderColour;
        public Color dangerColour;
        public Color overlaysColour;
        public int strokeWidth;
        public Color strokeColour;
        public bool strokeAroundLayoutMaterials;
        public bool strokeAroundRemoveMaterials;
        public bool strokeAroundEntities;
        public bool strokeAroundBrushEntities;
        public bool strokeAroundOverlays;
        public int defaultLevelNum;
        public bool levelBackgroundEnabled;
        public int levelBackgroundDarkenAlpha;
        public int levelBackgroundBlurAmount;
        public string higherLevelOutputName;
        public string lowerLevelOutputName;
        public bool exportTxt;
        public bool exportDds;
        public bool exportPng;
        public bool exportRawMasks;
        public bool exportBackgroundLevelsImage;

        // jerc_disp_rotation
        public List<int> displacementRotationSideIds90 = new();
        public List<int> displacementRotationSideIds180 = new();
        public List<int> displacementRotationSideIds270 = new();


        public ConfigurationValues(Dictionary<string, string> jercEntitySettingsValues, int jercDividerCount, bool jercDispRotationEntityProvided)
        {
            // jerc_config
            alternateOutputPath = string.IsNullOrWhiteSpace(jercEntitySettingsValues["alternateOutputPath"]) ? null : jercEntitySettingsValues["alternateOutputPath"];
            if (!string.IsNullOrWhiteSpace(alternateOutputPath) && alternateOutputPath.LastOrDefault() != '\\' && alternateOutputPath.LastOrDefault() != '/')
                alternateOutputPath += '/';

            onlyOutputToAlternatePath = jercEntitySettingsValues.ContainsKey("onlyOutputToAlternatePath") && jercEntitySettingsValues["onlyOutputToAlternatePath"] == "1";
            exportRadarAsSeparateLevels = jercEntitySettingsValues.ContainsKey("exportRadarAsSeparateLevels") && jercEntitySettingsValues["exportRadarAsSeparateLevels"] == "1";
            useSeparateGradientEachLevel = jercEntitySettingsValues.ContainsKey("useSeparateGradientEachLevel") && jercEntitySettingsValues["useSeparateGradientEachLevel"] == "1";
            ignoreDisplacementXYChanges = jercEntitySettingsValues.ContainsKey("ignoreDisplacementXYChanges") && jercEntitySettingsValues["ignoreDisplacementXYChanges"] == "1";
            rotateCutDispsAutomatic = jercEntitySettingsValues.ContainsKey("rotateCutDispsAutomatic") && jercEntitySettingsValues["rotateCutDispsAutomatic"] == "1";
            backgroundFilename = string.IsNullOrWhiteSpace(jercEntitySettingsValues["backgroundFilename"]) ? null : jercEntitySettingsValues["backgroundFilename"];

            radarSizeMultiplier = jercEntitySettingsValues.ContainsKey("radarSizeMultiplier") && jercEntitySettingsValues["radarSizeMultiplier"] != null ? float.Parse(jercEntitySettingsValues["radarSizeMultiplier"], Globalization.Style, Globalization.Culture) : 0.95f;
            if (radarSizeMultiplier < 0.01)
                radarSizeMultiplier = 0.01f;
            else if (radarSizeMultiplier > 1)
                radarSizeMultiplier = 1;

            overlapAlpha = jercEntitySettingsValues.ContainsKey("overlapAlpha") && jercEntitySettingsValues["overlapAlpha"] != null ? Math.Clamp(int.Parse(jercEntitySettingsValues["overlapAlpha"]), 0, 255) : 0;
            dangerAlpha = jercEntitySettingsValues.ContainsKey("dangerAlpha") && jercEntitySettingsValues["dangerAlpha"] != null ? Math.Clamp(int.Parse(jercEntitySettingsValues["dangerAlpha"]), 0, 255) : 0;
            pathColourHigh = jercEntitySettingsValues.ContainsKey("pathColourHigh") && jercEntitySettingsValues["pathColourHigh"] != null ? GetColourStringAsColour(jercEntitySettingsValues["pathColourHigh"]) : Colours.ColourError;
            pathColourLow = jercEntitySettingsValues.ContainsKey("pathColourLow") && jercEntitySettingsValues["pathColourLow"] != null ? GetColourStringAsColour(jercEntitySettingsValues["pathColourLow"]) : Colours.ColourError;
            overlapColourHigh = jercEntitySettingsValues.ContainsKey("overlapColourHigh") && jercEntitySettingsValues["overlapColourHigh"] != null ? GetColourStringAsColour(jercEntitySettingsValues["overlapColourHigh"]) : Colours.ColourError;
            overlapColourLow = jercEntitySettingsValues.ContainsKey("overlapColourLow") && jercEntitySettingsValues["overlapColourLow"] != null ? GetColourStringAsColour(jercEntitySettingsValues["overlapColourLow"]) : Colours.ColourError;
            coverColourHigh = jercEntitySettingsValues.ContainsKey("coverColourHigh") && jercEntitySettingsValues["coverColourHigh"] != null ? GetColourStringAsColour(jercEntitySettingsValues["coverColourHigh"]) : Colours.ColourError;
            coverColourLow = jercEntitySettingsValues.ContainsKey("coverColourLow") && jercEntitySettingsValues["coverColourLow"] != null ? GetColourStringAsColour(jercEntitySettingsValues["coverColourLow"]) : Colours.ColourError;
            doorColour = jercEntitySettingsValues.ContainsKey("doorColour") && jercEntitySettingsValues["doorColour"] != null ? GetColourStringAsColour(jercEntitySettingsValues["doorColour"]) : Colours.ColourError;
            ladderColour = jercEntitySettingsValues.ContainsKey("ladderColour") && jercEntitySettingsValues["ladderColour"] != null ? GetColourStringAsColour(jercEntitySettingsValues["ladderColour"]) : Colours.ColourError;
            dangerColour = jercEntitySettingsValues.ContainsKey("dangerColour") && jercEntitySettingsValues["dangerColour"] != null ? GetColourStringAsColour(jercEntitySettingsValues["dangerColour"]) : Colours.ColourError;
            overlaysColour = jercEntitySettingsValues.ContainsKey("overlaysColour") && jercEntitySettingsValues["overlaysColour"] != null ? GetColourStringAsColour(jercEntitySettingsValues["overlaysColour"]) : Colours.ColourError;
            strokeWidth = jercEntitySettingsValues.ContainsKey("strokeWidth") && jercEntitySettingsValues["strokeWidth"] != null ? int.Parse(jercEntitySettingsValues["strokeWidth"]) : 0;
            strokeColour = jercEntitySettingsValues.ContainsKey("strokeColour") && jercEntitySettingsValues["strokeColour"] != null ? GetColourStringAsColour(jercEntitySettingsValues["strokeColour"]) : Colours.ColourError;
            strokeAroundLayoutMaterials = jercEntitySettingsValues.ContainsKey("strokeAroundLayoutMaterials") && jercEntitySettingsValues["strokeAroundLayoutMaterials"] == "1";
            strokeAroundRemoveMaterials = jercEntitySettingsValues.ContainsKey("strokeAroundRemoveMaterials") && jercEntitySettingsValues["strokeAroundRemoveMaterials"] == "1";
            strokeAroundEntities = jercEntitySettingsValues.ContainsKey("strokeAroundEntities") && jercEntitySettingsValues["strokeAroundEntities"] == "1";
            strokeAroundBrushEntities = jercEntitySettingsValues.ContainsKey("strokeAroundBrushEntities") && jercEntitySettingsValues["strokeAroundBrushEntities"] == "1";
            strokeAroundOverlays = jercEntitySettingsValues.ContainsKey("strokeAroundOverlays") && jercEntitySettingsValues["strokeAroundOverlays"] == "1";

            defaultLevelNum = jercEntitySettingsValues.ContainsKey("defaultLevelNum") && jercEntitySettingsValues["defaultLevelNum"] != null ? int.Parse(jercEntitySettingsValues["defaultLevelNum"]) : 0;
            if (defaultLevelNum < 0)
                defaultLevelNum = 0;
            else if (defaultLevelNum > jercDividerCount)
                defaultLevelNum = jercDividerCount;

            levelBackgroundEnabled = jercEntitySettingsValues.ContainsKey("levelBackgroundEnabled") && jercEntitySettingsValues["levelBackgroundEnabled"] == "1";
            levelBackgroundDarkenAlpha = jercEntitySettingsValues.ContainsKey("levelBackgroundDarkenAlpha") && jercEntitySettingsValues["levelBackgroundDarkenAlpha"] != null ? Math.Clamp(int.Parse(jercEntitySettingsValues["levelBackgroundDarkenAlpha"]), 0, 255) : 0;
            levelBackgroundBlurAmount = jercEntitySettingsValues.ContainsKey("levelBackgroundBlurAmount") && jercEntitySettingsValues["levelBackgroundBlurAmount"] != null ? int.Parse(jercEntitySettingsValues["levelBackgroundBlurAmount"]) : 0;
            higherLevelOutputName = string.IsNullOrWhiteSpace(jercEntitySettingsValues["higherLevelOutputName"]) ? null : jercEntitySettingsValues["higherLevelOutputName"];
            lowerLevelOutputName = string.IsNullOrWhiteSpace(jercEntitySettingsValues["lowerLevelOutputName"]) ? null : jercEntitySettingsValues["lowerLevelOutputName"];
            exportTxt = jercEntitySettingsValues.ContainsKey("exportTxt") && jercEntitySettingsValues["exportTxt"] == "1";
            exportDds = jercEntitySettingsValues.ContainsKey("exportDds") && jercEntitySettingsValues["exportDds"] == "1";
            exportPng = jercEntitySettingsValues.ContainsKey("exportPng") && jercEntitySettingsValues["exportPng"] == "1";
            exportRawMasks = jercEntitySettingsValues.ContainsKey("exportRawMasks") && jercEntitySettingsValues["exportRawMasks"] == "1";
            exportBackgroundLevelsImage = jercEntitySettingsValues.ContainsKey("exportBackgroundLevelsImage") && jercEntitySettingsValues["exportBackgroundLevelsImage"] == "1";


            // jerc_disp_rotation
            if (jercDispRotationEntityProvided)
            {
                var dispRotationSideIds90 = string.IsNullOrWhiteSpace(jercEntitySettingsValues["displacementRotationSideIds90"]) ? new List<string>() : jercEntitySettingsValues["displacementRotationSideIds90"].Split(" ").Distinct().ToList();
                var dispRotationSideIds180 = string.IsNullOrWhiteSpace(jercEntitySettingsValues["displacementRotationSideIds180"]) ? new List<string>() : jercEntitySettingsValues["displacementRotationSideIds180"].Split(" ").Distinct().ToList();
                var dispRotationSideIds270 = string.IsNullOrWhiteSpace(jercEntitySettingsValues["displacementRotationSideIds270"]) ? new List<string>() : jercEntitySettingsValues["displacementRotationSideIds270"].Split(" ").Distinct().ToList();

                foreach (var brushSideIdString in dispRotationSideIds90)
                {
                    if (int.TryParse(brushSideIdString, Globalization.Style, Globalization.Culture, out int brushSideId))
                        displacementRotationSideIds90.Add(brushSideId);
                    else
                        Logger.LogImportantWarning($"Could not rotate displacement 90 degrees clockwise due to parsing error. Brush side ID: {brushSideIdString}");
                }

                foreach (var brushSideIdString in dispRotationSideIds180)
                {
                    if (int.TryParse(brushSideIdString, Globalization.Style, Globalization.Culture, out int brushSideId))
                        displacementRotationSideIds180.Add(brushSideId);
                    else
                        Logger.LogImportantWarning($"Could not rotate displacement 180 degrees due to parsing error. Brush side ID: {brushSideIdString}");
                }

                foreach (var brushSideIdString in dispRotationSideIds270)
                {
                    if (int.TryParse(brushSideIdString, Globalization.Style, Globalization.Culture, out int brushSideId))
                        displacementRotationSideIds270.Add(brushSideId);
                    else
                        Logger.LogImportantWarning($"Could not rotate displacement 90 degrees anti-clockwise due to parsing error. Brush side ID: {brushSideIdString}");
                }
            }
        }


        private static Color GetColourStringAsColour(string colourString)
        {
            if (string.IsNullOrWhiteSpace(colourString))
                return Colours.ColourError;

            var colourStringSplit = colourString.Split(" ");

            if (colourStringSplit.Length != 3)
                return Colours.ColourError; // error, must be 3 values only

            var argb = new int[4] { 255, int.Parse(colourStringSplit[0]), int.Parse(colourStringSplit[1]), int.Parse(colourStringSplit[2]) };

            return Color.FromArgb(argb[0], argb[1], argb[2], argb[3]);
        }
    }
}
