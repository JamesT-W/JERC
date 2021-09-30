using System.Drawing;

namespace JERC.Models
{
    public class RadarLevel
    {
        public Bitmap bmp;
        public Graphics graphics;
        public LevelHeight levelHeight;

        public RadarLevel(Bitmap bmp, LevelHeight levelHeight)
        {
            this.bmp = bmp;
            this.graphics = Graphics.FromImage(bmp);
            this.levelHeight = levelHeight;
        }
    }
}
