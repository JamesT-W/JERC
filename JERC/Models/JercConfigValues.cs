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
        public string backgroundFilename;
        public string alternateOutputPath;
        public bool onlyOutputToAlternatePath;
        public Color pathColourHigh;
        public Color pathColourLow;
        public Color coverColourHigh;
        public Color coverColourLow;
        public Color overlapColourHigh;
        public Color overlapColourLow;
        public int strokeWidth;
        public Color strokeColour;
        public bool strokeAroundMainMaterials;
        public bool strokeAroundRemoveMaterials;
        public bool exportSeparateLevelRadars;
        public bool exportTxt;
        public bool exportDds;
        public bool exportPng;


        public JercConfigValues(Dictionary<string, string> jercEntitySettingsValues)
        {
            // jerc_configure

            backgroundFilename = string.IsNullOrWhiteSpace(jercEntitySettingsValues["backgroundFilename"]) ? null : jercEntitySettingsValues["backgroundFilename"];
            alternateOutputPath = string.IsNullOrWhiteSpace(jercEntitySettingsValues["alternateOutputPath"]) ? null : jercEntitySettingsValues["alternateOutputPath"];
            onlyOutputToAlternatePath = jercEntitySettingsValues["onlyOutputToAlternatePath"] == "0";
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
            strokeAroundMainMaterials = jercEntitySettingsValues["strokeAroundMainMaterials"] == "0";
            strokeAroundRemoveMaterials = jercEntitySettingsValues["strokeAroundRemoveMaterials"] == "0";
            exportSeparateLevelRadars = jercEntitySettingsValues["exportSeparateLevelRadars"] == "0";
            exportTxt = jercEntitySettingsValues["exportTxt"] == "0";
            exportDds = jercEntitySettingsValues["exportDds"] == "0";
            exportPng = jercEntitySettingsValues["exportPng"] == "0";


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
