using System.Drawing;

namespace JERC.Models
{
    public class RadarLevel
    {
        public Bitmap bmp;
        public Graphics graphics;
        public LevelHeight levelHeight;

        public RadarLevel(Bitmap bmp, Graphics graphics, LevelHeight levelHeight)
        {
            this.bmp = bmp;
            this.graphics = graphics;
            this.levelHeight = levelHeight;
        }
    }
}
