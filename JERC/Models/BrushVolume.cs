using JERC.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Models
{
    public class BrushVolume
    {
        public int brushId;
        public List<BrushSide> brushSides = new List<BrushSide>();
        public JercTypes jercType;

        public BrushVolume(int brushId)
        {
            this.brushId = brushId;
        }
    }
}
