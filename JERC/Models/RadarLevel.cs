using System.Collections.Generic;
using System.Drawing;

namespace JERC.Models
{
    public class RadarLevel
    {
        public Bitmap bmpRadar;
        public Graphics graphicsRadar;
        public LevelHeight levelHeight;
        public Dictionary<string, Bitmap> bmpRawMasksByName;

        public RadarLevel(Bitmap bmpRadar, LevelHeight levelHeight, Dictionary<string, Bitmap> bmpRawMasksByName)
        {
            this.bmpRadar = bmpRadar;
            this.graphicsRadar = Graphics.FromImage(bmpRadar);
            this.levelHeight = levelHeight;
            this.bmpRawMasksByName = bmpRawMasksByName;
        }
    }
}
