using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Models
{
    public class AllBrushesToDraw
    {
        public List<ObjectToDraw> brushesToDrawPath;
        public List<ObjectToDraw> brushesToDrawOverlap;
        public List<ObjectToDraw> brushesToDrawCover;
        public List<ObjectToDraw> brushesToDrawDoor;
        public List<ObjectToDraw> brushesToDrawLadder;
        public List<ObjectToDraw> brushesToDrawDanger;

        public List<ObjectToDraw> brushesToDrawBuyzone;
        public List<ObjectToDraw> brushesToDrawBombsiteA;
        public List<ObjectToDraw> brushesToDrawBombsiteB;
        public List<ObjectToDraw> brushesToDrawRescueZone;

        public AllBrushesToDraw(List<ObjectToDraw> brushesToDrawPath, List<ObjectToDraw> brushesToDrawOverlap, List<ObjectToDraw> brushesToDrawCover, List<ObjectToDraw> brushesToDrawDoor, List<ObjectToDraw> brushesToDrawLadder, List<ObjectToDraw> brushesToDrawDanger,
            List<ObjectToDraw> brushesToDrawBuyzone, List<ObjectToDraw> brushesToDrawBombsiteA, List<ObjectToDraw> brushesToDrawBombsiteB, List<ObjectToDraw> brushesToDrawRescueZone
        )
        {
            this.brushesToDrawPath = brushesToDrawPath ?? new List<ObjectToDraw>();
            this.brushesToDrawOverlap = brushesToDrawOverlap ?? new List<ObjectToDraw>();
            this.brushesToDrawCover = brushesToDrawCover ?? new List<ObjectToDraw>();
            this.brushesToDrawDoor = brushesToDrawDoor ?? new List<ObjectToDraw>();
            this.brushesToDrawLadder = brushesToDrawLadder ?? new List<ObjectToDraw>();
            this.brushesToDrawDanger = brushesToDrawDanger ?? new List<ObjectToDraw>();

            this.brushesToDrawBuyzone = brushesToDrawBuyzone ?? new List<ObjectToDraw>();
            this.brushesToDrawBombsiteA = brushesToDrawBombsiteA ?? new List<ObjectToDraw>();
            this.brushesToDrawBombsiteB = brushesToDrawBombsiteB ?? new List<ObjectToDraw>();
            this.brushesToDrawRescueZone = brushesToDrawRescueZone ?? new List<ObjectToDraw>();
        }
    }
}
