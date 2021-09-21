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

        public int width;
        public int height;

        public int paddingSizeX;
        public int paddingSizeY;

        public float paddingPercentageX;
        public float paddingPercentageY;

        public int outputResolution;

        //  used to align the image to center if the width != height
        public float brushVerticesOffsetX;
        public float brushVerticesOffsetY;

        public float offsetPercentageX;
        public float offsetPercentageY;

        public float posX;
        public float posY;
        public float scale;

        public OverviewPositionValues(JercConfigValues jercConfigValues, float brushVerticesPosMinX, float brushVerticesPosMaxX, float brushVerticesPosMinY, float brushVerticesPosMaxY, float scale)
        {
            this.brushVerticesPosMinX = brushVerticesPosMinX;
            this.brushVerticesPosMaxX = brushVerticesPosMaxX;
            this.brushVerticesPosMinY = brushVerticesPosMinY;
            this.brushVerticesPosMaxY = brushVerticesPosMaxY;

            width = (int)Math.Ceiling(brushVerticesPosMaxX - brushVerticesPosMinX) + (jercConfigValues.strokeWidth * 2); // adds the stroke width mutliplier value as a padding at left and right
            height = (int)Math.Ceiling(brushVerticesPosMaxY - brushVerticesPosMinY) + (jercConfigValues.strokeWidth * 2); // adds the stroke width mutliplier value as a padding at top and bottom

            outputResolution = width >= height ? width : height;

            paddingSizeX = width < height ? height - width : 0;
            paddingSizeY = height < width ? width - height : 0;

            paddingPercentageX = (float)paddingSizeX / (float)outputResolution;
            paddingPercentageY = (float)paddingSizeY / (float)outputResolution;

            brushVerticesOffsetX = width < height ? ((height - width) / 2) + jercConfigValues.strokeWidth : 0 + jercConfigValues.strokeWidth;
            brushVerticesOffsetY = height < width ? ((width - height) / 2) + jercConfigValues.strokeWidth : 0 + jercConfigValues.strokeWidth;

            offsetPercentageX = brushVerticesOffsetX / width;
            offsetPercentageY = brushVerticesOffsetY / height;

            /*posX = ((brushVerticesPosMaxX - brushVerticesPosMinX) + OverviewOffsets.GetCenteredValueByScalePosX(scale)) - 1024 + 256; // - 1024 for resolution, + 256 for offset (because cl_leveloverview is used at 1280x1024)
            posY = ((brushVerticesPosMaxY - brushVerticesPosMinY) + OverviewOffsets.GetCenteredValueByScalePosY(scale)) - 1024;*/ // - 1024 for resolution
            posX = brushVerticesPosMinX - brushVerticesOffsetX;
            posY = brushVerticesPosMinY + (scale * 1024) - brushVerticesOffsetY;

            this.scale = scale;
        }
    }
}
