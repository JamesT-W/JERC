using JERC.Constants;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Models
{
    public class JercConfigValues
    {
        public string alternateOutputPath;
        public bool onlyOutputToAlternatePath;
        public bool exportRadarAsSeparateLevels;
        public bool useSeparateGradientEachLevel;
        public string backgroundFilename;
        public Color pathColourHigh;
        public Color pathColourLow;
        public Color coverColourHigh;
        public Color coverColourLow;
        public Color overlapColourHigh;
        public Color overlapColourLow;
        public int strokeWidth;
        public Color strokeColour;
        public bool strokeAroundLayoutMaterials;
        public bool strokeAroundRemoveMaterials;
        public bool strokeAroundEntities;
        public int defaultLevelNum;
        public bool levelBackgroundEnabled;
        public int levelBackgroundDarkenAlpha;
        public int levelBackgroundBlurAmount;
        public string higherLevelOutputName;
        public string lowerLevelOutputName;
        public bool exportTxt;
        public bool exportDds;
        public bool exportPng;
        public bool exportBackgroundLevelsImage;


        public JercConfigValues(Dictionary<string, string> jercEntitySettingsValues, int jercDividerCount)
        {
            // jerc_config

            alternateOutputPath = string.IsNullOrWhiteSpace(jercEntitySettingsValues["alternateOutputPath"]) ? null : jercEntitySettingsValues["alternateOutputPath"];
            if (!string.IsNullOrWhiteSpace(alternateOutputPath) && (alternateOutputPath.LastOrDefault() != '\\' || alternateOutputPath.LastOrDefault() != '/'))
                alternateOutputPath += '/';

            onlyOutputToAlternatePath = jercEntitySettingsValues.ContainsKey("onlyOutputToAlternatePath") && jercEntitySettingsValues["onlyOutputToAlternatePath"] == "0";
            exportRadarAsSeparateLevels = jercEntitySettingsValues.ContainsKey("exportRadarAsSeparateLevels") && jercEntitySettingsValues["exportRadarAsSeparateLevels"] == "0";
            useSeparateGradientEachLevel = jercEntitySettingsValues.ContainsKey("useSeparateGradientEachLevel") && jercEntitySettingsValues["useSeparateGradientEachLevel"] == "0";
            backgroundFilename = string.IsNullOrWhiteSpace(jercEntitySettingsValues["backgroundFilename"]) ? null : jercEntitySettingsValues["backgroundFilename"];
            pathColourHigh = jercEntitySettingsValues.ContainsKey("pathColourHigh") ? GetColourStringAsColour(jercEntitySettingsValues["pathColourHigh"]) : Colours.ColourError;
            pathColourLow = jercEntitySettingsValues.ContainsKey("pathColourLow") ? GetColourStringAsColour(jercEntitySettingsValues["pathColourLow"]) : Colours.ColourError;
            coverColourHigh = jercEntitySettingsValues.ContainsKey("coverColourHigh") ? GetColourStringAsColour(jercEntitySettingsValues["coverColourHigh"]) : Colours.ColourError;
            coverColourLow = jercEntitySettingsValues.ContainsKey("coverColourLow") ? GetColourStringAsColour(jercEntitySettingsValues["coverColourLow"]) : Colours.ColourError;
            overlapColourHigh = jercEntitySettingsValues.ContainsKey("overlapColourHigh") ? GetColourStringAsColour(jercEntitySettingsValues["overlapColourHigh"]) : Colours.ColourError;
            overlapColourLow = jercEntitySettingsValues.ContainsKey("overlapColourLow") ? GetColourStringAsColour(jercEntitySettingsValues["overlapColourLow"]) : Colours.ColourError;
            strokeWidth = jercEntitySettingsValues.ContainsKey("strokeWidth") ? int.Parse(jercEntitySettingsValues["strokeWidth"]) : 0;
            strokeColour = jercEntitySettingsValues.ContainsKey("strokeColour") ? GetColourStringAsColour(jercEntitySettingsValues["strokeColour"]) : Colours.ColourError;
            strokeAroundLayoutMaterials = jercEntitySettingsValues.ContainsKey("strokeAroundLayoutMaterials") && jercEntitySettingsValues["strokeAroundLayoutMaterials"] == "0";
            strokeAroundRemoveMaterials = jercEntitySettingsValues.ContainsKey("strokeAroundRemoveMaterials") && jercEntitySettingsValues["strokeAroundRemoveMaterials"] == "0";
            strokeAroundEntities = jercEntitySettingsValues.ContainsKey("strokeAroundEntities") && jercEntitySettingsValues["strokeAroundEntities"] == "0";

            defaultLevelNum = jercEntitySettingsValues.ContainsKey("defaultLevelNum") ? int.Parse(jercEntitySettingsValues["defaultLevelNum"]) : 0;
            if (defaultLevelNum < 0)
                defaultLevelNum = 0;
            else if (defaultLevelNum > jercDividerCount)
                defaultLevelNum = jercDividerCount;

            levelBackgroundEnabled = jercEntitySettingsValues.ContainsKey("levelBackgroundEnabled") && jercEntitySettingsValues["levelBackgroundEnabled"] == "0";
            levelBackgroundDarkenAlpha = jercEntitySettingsValues.ContainsKey("levelBackgroundDarkenAlpha") ? Math.Clamp(int.Parse(jercEntitySettingsValues["levelBackgroundDarkenAlpha"]), 0, 255) : 0;
            levelBackgroundBlurAmount = jercEntitySettingsValues.ContainsKey("levelBackgroundBlurAmount") ? int.Parse(jercEntitySettingsValues["levelBackgroundBlurAmount"]) : 0;
            higherLevelOutputName = string.IsNullOrWhiteSpace(jercEntitySettingsValues["higherLevelOutputName"]) ? null : jercEntitySettingsValues["higherLevelOutputName"];
            lowerLevelOutputName = string.IsNullOrWhiteSpace(jercEntitySettingsValues["lowerLevelOutputName"]) ? null : jercEntitySettingsValues["lowerLevelOutputName"];
            exportTxt = jercEntitySettingsValues.ContainsKey("exportTxt") && jercEntitySettingsValues["exportTxt"] == "0";
            exportDds = jercEntitySettingsValues.ContainsKey("exportDds") && jercEntitySettingsValues["exportDds"] == "0";
            exportPng = jercEntitySettingsValues.ContainsKey("exportPng") && jercEntitySettingsValues["exportPng"] == "0";
            exportBackgroundLevelsImage = jercEntitySettingsValues.ContainsKey("exportBackgroundLevelsImage") && jercEntitySettingsValues["exportBackgroundLevelsImage"] == "0";


            // 


        }


        private static Color GetColourStringAsColour(string colourString)
        {
            var colourStringSplit = colourString.Split(" ");

            if (colourStringSplit.Length != 3)
                return Colours.ColourError; // error, must be 3 values only

            var argb = new int[4] { 255, int.Parse(colourStringSplit[0]), int.Parse(colourStringSplit[1]), int.Parse(colourStringSplit[2]) };

            return Color.FromArgb(argb[0], argb[1], argb[2], argb[3]);
        }
    }
}
