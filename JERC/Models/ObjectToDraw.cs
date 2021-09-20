using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Models
{
    public class ObjectToDraw
    {
        public Point[] vertices;
        public Pen pen;
        public SolidBrush solidBrush;

        public ObjectToDraw(PointF[] vertices, Pen pen, SolidBrush solidBrush)
        {
            this.vertices = vertices.Select(x => new Point((int)x.X, (int)x.Y)).ToArray();
            this.pen = pen;
            this.solidBrush = solidBrush;
        }
    }
}
