using JERC.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Models
{
    public class OverviewPositionValues
    {
        public float brushVerticesPosMinX;
        public float brushVerticesPosMaxX;
        public float brushVerticesPosMinY;
        public float brushVerticesPosMaxY;

        public int widthWithoutStroke;
        public int heightWithoutStroke;

        public int widthWithStroke;
        public int heightWithStroke;

        public int paddingSizeX;
        public int paddingSizeY;

        // calculated using radarSizeMultiplier in jerc_config
        public float radarSizeMultiplierChangeAmountWidth;
        public float radarSizeMultiplierChangeAmountHeight;

        // calculated by looking at difference between width and height
        public float paddingPercentageX;
        public float paddingPercentageY;

        public int outputResolution;

        //  used to align the image to center if the width != height
        public int brushVerticesOffsetX;
        public int brushVerticesOffsetY;

        //public float offsetPercentageX;
        //public float offsetPercentageY;

        public float posX;
        public float posY;
        public float scale;

        public OverviewPositionValues(ConfigurationValues configurationValues, float brushVerticesPosMinX, float brushVerticesPosMaxX, float brushVerticesPosMinY, float brushVerticesPosMaxY, float scale)
        {
            this.brushVerticesPosMinX = brushVerticesPosMinX;
            this.brushVerticesPosMaxX = brushVerticesPosMaxX;
            this.brushVerticesPosMinY = brushVerticesPosMinY;
            this.brushVerticesPosMaxY = brushVerticesPosMaxY;

            var widthBeforeMultiplier = (int)(Math.Ceiling(brushVerticesPosMaxX - brushVerticesPosMinX));
            var heightBeforeMultiplier = (int)(Math.Ceiling(brushVerticesPosMaxY - brushVerticesPosMinY));

            widthWithoutStroke = (int)(widthBeforeMultiplier / configurationValues.radarSizeMultiplier);
            heightWithoutStroke = (int)(widthBeforeMultiplier / configurationValues.radarSizeMultiplier);

            widthWithStroke = (int)((widthBeforeMultiplier + (configurationValues.strokeWidth * 2)) / configurationValues.radarSizeMultiplier); // adds the stroke width mutliplier value as a padding at left and right
            heightWithStroke = (int)((heightBeforeMultiplier + (configurationValues.strokeWidth * 2)) / configurationValues.radarSizeMultiplier); // adds the stroke width mutliplier value as a padding at top and bottom

            if (configurationValues.overviewGamemodeType == 1 && configurationValues.strokeAroundBrushEntities && (widthWithStroke > DangerZoneValues.DangerZoneOverviewSize || heightWithStroke > DangerZoneValues.DangerZoneOverviewSize))
            {
                Logger.LogImportantWarning("Danger Zone maps should not be larger than 20480x20480 units, errors are likely to occur");
            }

            radarSizeMultiplierChangeAmountWidth = widthBeforeMultiplier - widthWithStroke;
            radarSizeMultiplierChangeAmountHeight = heightBeforeMultiplier - heightWithStroke;

            outputResolution = widthWithStroke >= heightWithStroke ? widthWithStroke : heightWithStroke;

            paddingSizeX = widthWithStroke < heightWithStroke ? heightWithStroke - widthWithStroke : 0;
            paddingSizeY = heightWithStroke < widthWithStroke ? widthWithStroke - heightWithStroke : 0;

            paddingPercentageX = (float)paddingSizeX / (float)outputResolution;
            paddingPercentageY = (float)paddingSizeY / (float)outputResolution;

            brushVerticesOffsetX = (int)(widthWithStroke < heightWithStroke ? ((heightWithStroke - widthWithStroke) / 2) + configurationValues.strokeWidth - (radarSizeMultiplierChangeAmountWidth / 2) : 0 + configurationValues.strokeWidth - (radarSizeMultiplierChangeAmountWidth / 2));
            brushVerticesOffsetY = (int)(heightWithStroke < widthWithStroke ? ((widthWithStroke - heightWithStroke) / 2) + configurationValues.strokeWidth - (radarSizeMultiplierChangeAmountHeight / 2) : 0 + configurationValues.strokeWidth - (radarSizeMultiplierChangeAmountHeight / 2));

            //offsetPercentageX = (brushVerticesOffsetX / width);
            //offsetPercentageY = (brushVerticesOffsetY / height);

            /*posX = ((brushVerticesPosMaxX - brushVerticesPosMinX) + OverviewOffsets.GetCenteredValueByScalePosX(scale)) - 1024 + 256; // - 1024 for resolution, + 256 for offset (because cl_leveloverview is used at 1280x1024)
            posY = ((brushVerticesPosMaxY - brushVerticesPosMinY) + OverviewOffsets.GetCenteredValueByScalePosY(scale)) - 1024;*/ // - 1024 for resolution
            posX = brushVerticesPosMinX - brushVerticesOffsetX;
            posY = brushVerticesPosMinY + (scale * 1024) - brushVerticesOffsetY;

            this.scale = scale;
        }
    }
}
