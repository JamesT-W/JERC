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

        //  used to align the image to center if the width != height
        public float brushVerticesOffsetX;
        public float brushVerticesOffsetY;

        public int outputResolution;

        public float posX;
        public float posY;
        public float scale;

        public OverviewPositionValues(float brushVerticesPosMinX, float brushVerticesPosMaxX, float brushVerticesPosMinY, float brushVerticesPosMaxY, float scale)
        {
            this.brushVerticesPosMinX = brushVerticesPosMinX;
            this.brushVerticesPosMaxX = brushVerticesPosMaxX;
            this.brushVerticesPosMinY = brushVerticesPosMinY;
            this.brushVerticesPosMaxY = brushVerticesPosMaxY;

            width = (int)Math.Ceiling(brushVerticesPosMaxX - brushVerticesPosMinX) + (Sizes.StrokeWidthMultiplier * 2); // adds the stroke width mutliplier value as a margin at left and right
            height = (int)Math.Ceiling(brushVerticesPosMaxY - brushVerticesPosMinY) + (Sizes.StrokeWidthMultiplier * 2); // adds the stroke width mutliplier value as a margin at top and bottom

            brushVerticesOffsetX = width < height ? ((height - width) / 2) + Sizes.StrokeWidthMultiplier : 0 + Sizes.StrokeWidthMultiplier;
            brushVerticesOffsetY = height < width ? ((width - height) / 2) + Sizes.StrokeWidthMultiplier : 0 + Sizes.StrokeWidthMultiplier;

            outputResolution = width >= height ? width : height;

            /*posX = ((brushVerticesPosMaxX - brushVerticesPosMinX) + OverviewOffsets.GetCenteredValueByScalePosX(scale)) - 1024 + 256; // - 1024 for resolution, + 256 for offset (because cl_leveloverview is used at 1280x1024)
            posY = ((brushVerticesPosMaxY - brushVerticesPosMinY) + OverviewOffsets.GetCenteredValueByScalePosY(scale)) - 1024;*/ // - 1024 for resolution
            posX = brushVerticesPosMinX - brushVerticesOffsetX;
            posY = brushVerticesPosMinY + (scale * 1024) - brushVerticesOffsetY;

            this.scale = scale;
        }
    }
}
