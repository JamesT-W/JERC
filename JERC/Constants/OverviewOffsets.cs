using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Constants
{
    public static class OverviewOffsets
    {
        public static readonly float OverviewScaleDivider = 1024;

        public static readonly float OverviewIncreasedUnitsShownPerScaleIntegerPosX = 1280;
        public static readonly float OverviewIncreasedUnitsShownPerScaleIntegerPosY = 1024;

        private static readonly float OverviewCenteredOffsetPerScaleIntegerPosX = -640;
        private static readonly float OverviewCenteredOffsetPerScaleIntegerPosY = 512;

        public static float GetCenteredValueByScalePosX(float scale)
        {
            return scale * OverviewCenteredOffsetPerScaleIntegerPosX;
        }

        public static float GetCenteredValueByScalePosY(float scale)
        {
            return scale * OverviewCenteredOffsetPerScaleIntegerPosY;
        }
    }
}
