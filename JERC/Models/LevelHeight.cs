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
        public float zMin;
        public float zMax;

        public LevelHeight(int levelNum, string levelName, float zMin, float zMax)
        {
            this.levelNum = levelNum;
            this.levelName = levelName;
            this.zMin = zMin;
            this.zMax = zMax;
        }
    }
}
