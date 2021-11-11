using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Models
{
    public class AllDisplacementsToDraw
    {
        public List<ObjectToDraw> displacementsToDrawPath;
        public List<ObjectToDraw> displacementsToDrawOverlap;
        public List<ObjectToDraw> displacementsToDrawCover;
        public List<ObjectToDraw> displacementsToDrawDoor;
        public List<ObjectToDraw> displacementsToDrawLadder;
        public List<ObjectToDraw> displacementsToDrawDanger;

        public List<ObjectToDraw> displacementsToDrawBuyzone;
        public List<ObjectToDraw> displacementsToDrawBombsiteA;
        public List<ObjectToDraw> displacementsToDrawBombsiteB;
        public List<ObjectToDraw> displacementsToDrawRescueZone;

        public AllDisplacementsToDraw(List<ObjectToDraw> displacementsToDrawPath, List<ObjectToDraw> displacementsToDrawOverlap, List<ObjectToDraw> displacementsToDrawCover, List<ObjectToDraw> displacementsToDrawDoor, List<ObjectToDraw> displacementsToDrawLadder, List<ObjectToDraw> displacementsToDrawDanger,
            List<ObjectToDraw> displacementsToDrawBuyzone, List<ObjectToDraw> displacementsToDrawBombsiteA, List<ObjectToDraw> displacementsToDrawBombsiteB, List<ObjectToDraw> displacementsToDrawRescueZone
        )
        {
            this.displacementsToDrawPath = displacementsToDrawPath ?? new List<ObjectToDraw>();
            this.displacementsToDrawOverlap = displacementsToDrawOverlap ?? new List<ObjectToDraw>();
            this.displacementsToDrawCover = displacementsToDrawCover ?? new List<ObjectToDraw>();
            this.displacementsToDrawDoor = displacementsToDrawDoor ?? new List<ObjectToDraw>();
            this.displacementsToDrawLadder = displacementsToDrawLadder ?? new List<ObjectToDraw>();
            this.displacementsToDrawDanger = displacementsToDrawDanger ?? new List<ObjectToDraw>();

            this.displacementsToDrawBuyzone = displacementsToDrawBuyzone ?? new List<ObjectToDraw>();
            this.displacementsToDrawBombsiteA = displacementsToDrawBombsiteA ?? new List<ObjectToDraw>();
            this.displacementsToDrawBombsiteB = displacementsToDrawBombsiteB ?? new List<ObjectToDraw>();
            this.displacementsToDrawRescueZone = displacementsToDrawRescueZone ?? new List<ObjectToDraw>();
        }
    }
}
