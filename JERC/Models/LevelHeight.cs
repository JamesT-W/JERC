using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Models
{
    public class LevelHeight
    {
        public int levelNum;
        public string levelName;
        public float zMinForTxt;
        public float zMaxForTxt;
        public float zMinForRadar;
        public float zMaxForRadar;
        public float zMinForRadarGradient;
        public float zMaxForRadarGradient;

        public LevelHeight(int levelNum, string levelName, float zMinForTxt, float zMaxForTxt, float zMinForRadar, float zMaxForRadar, float zMinForRadarGradient, float zMaxForRadarGradient)
        {
            this.levelNum = levelNum;
            this.levelName = levelName;
            this.zMinForTxt = zMinForTxt;
            this.zMaxForTxt = zMaxForTxt;
            this.zMinForRadar = zMinForRadar;
            this.zMaxForRadar = zMaxForRadar;
            this.zMinForRadarGradient = zMinForRadarGradient;
            this.zMaxForRadarGradient = zMaxForRadarGradient;
        }
    }
}
