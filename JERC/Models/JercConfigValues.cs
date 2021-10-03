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
        public bool exportRadarAsSeparateLevels;
        public bool exportBackgroundLevelsImage;


        public JercConfigValues(Dictionary<string, string> jercEntitySettingsValues, int jercDividerCount)
        {
            // jerc_configure

            alternateOutputPath = string.IsNullOrWhiteSpace(jercEntitySettingsValues["alternateOutputPath"]) ? null : jercEntitySettingsValues["alternateOutputPath"];
            onlyOutputToAlternatePath = jercEntitySettingsValues["onlyOutputToAlternatePath"] == "0";
            backgroundFilename = string.IsNullOrWhiteSpace(jercEntitySettingsValues["backgroundFilename"]) ? null : jercEntitySettingsValues["backgroundFilename"];
            pathColourHigh = GetColourStringAsColour(jercEntitySettingsValues["pathColourHigh"]);
            pathColourLow = GetColourStringAsColour(jercEntitySettingsValues["pathColourLow"]);
            coverColourHigh = GetColourStringAsColour(jercEntitySettingsValues["coverColourHigh"]);
            coverColourLow = GetColourStringAsColour(jercEntitySettingsValues["coverColourLow"]);
            overlapColourHigh = GetColourStringAsColour(jercEntitySettingsValues["overlapColourHigh"]);
            overlapColourLow = GetColourStringAsColour(jercEntitySettingsValues["overlapColourLow"]);
            /*if (!int.TryParse(jercEntitySettingsValues["strokeWidth"], out strokeWidth))
                strokeWidth = defaultStrokeWidthMultiplier;*/
            strokeWidth = int.Parse(jercEntitySettingsValues["strokeWidth"]);
            strokeColour = GetColourStringAsColour(jercEntitySettingsValues["strokeColour"]);
            strokeAroundLayoutMaterials = jercEntitySettingsValues["strokeAroundLayoutMaterials"] == "0";
            strokeAroundRemoveMaterials = jercEntitySettingsValues["strokeAroundRemoveMaterials"] == "0";
            strokeAroundEntities = jercEntitySettingsValues["strokeAroundEntities"] == "0";

            defaultLevelNum = int.Parse(jercEntitySettingsValues["defaultLevelNum"]);
            if (defaultLevelNum < 0)
                defaultLevelNum = 0;
            else if (defaultLevelNum > jercDividerCount)
                defaultLevelNum = jercDividerCount;

            levelBackgroundEnabled = jercEntitySettingsValues["levelBackgroundEnabled"] == "0";
            levelBackgroundDarkenAlpha = Math.Clamp(int.Parse(jercEntitySettingsValues["levelBackgroundDarkenAlpha"]), 0, 255);
            levelBackgroundBlurAmount = int.Parse(jercEntitySettingsValues["levelBackgroundBlurAmount"]);
            higherLevelOutputName = string.IsNullOrWhiteSpace(jercEntitySettingsValues["higherLevelOutputName"]) ? null : jercEntitySettingsValues["higherLevelOutputName"];
            lowerLevelOutputName = string.IsNullOrWhiteSpace(jercEntitySettingsValues["lowerLevelOutputName"]) ? null : jercEntitySettingsValues["lowerLevelOutputName"];
            exportTxt = jercEntitySettingsValues["exportTxt"] == "0";
            exportDds = jercEntitySettingsValues["exportDds"] == "0";
            exportPng = jercEntitySettingsValues["exportPng"] == "0";
            exportRadarAsSeparateLevels = jercEntitySettingsValues["exportRadarAsSeparateLevels"] == "0";
            exportBackgroundLevelsImage = jercEntitySettingsValues["exportBackgroundLevelsImage"] == "0";


            // 


        }


        private static Color GetColourStringAsColour(string colourString)
        {
            var colourStringSplit = colourString.Split(" ");

            if (colourStringSplit.Length != 4)
                return Color.Red;

            var argb = new int[4] { int.Parse(colourStringSplit[3]), int.Parse(colourStringSplit[0]), int.Parse(colourStringSplit[1]), int.Parse(colourStringSplit[2]) };

            return Color.FromArgb(argb[0], argb[1], argb[2], argb[3]);
        }
    }
}
