using JERC.Constants;
using System.Collections.Generic;

namespace JERC.Models
{
    public class VisgroupIdsInVmf
    {
        // texture mode
        public int Jerc;

        // separated visgroups mode
        public int JercRemove;
        public int JercPath;
        public int JercCover;
        public int JercOverlap;

        public int JercDoor;
        public int JercLadder;
        public int JercDanger;

        // everything else should just go in the jerc visgroup (bound to the correct entity)


        public VisgroupIdsInVmf()
        { }


        public List<int> GetAllIdsInList()
        {
            return new List<int>()
            {
                Jerc,
                JercRemove,
                JercPath,
                JercCover,
                JercOverlap,
                JercDoor,
                JercLadder,
                JercDanger
            };
        }


        public int? GetVisgroupId(string visgroupName)
        {
            if (visgroupName == VisgroupNames.Jerc)
                return Jerc;
            if (visgroupName == VisgroupNames.JercRemove)
                return JercRemove;
            if (visgroupName == VisgroupNames.JercPath)
                return JercPath;
            if (visgroupName == VisgroupNames.JercCover)
                return JercCover;
            if (visgroupName == VisgroupNames.JercOverlap)
                return JercOverlap;
            if (visgroupName == VisgroupNames.JercDoor)
                return JercDoor;
            if (visgroupName == VisgroupNames.JercLadder)
                return JercLadder;
            if (visgroupName == VisgroupNames.JercDanger)
                return JercDanger;

            Logger.LogWarning("Could not find visgroup ID using visgroup name");

            return null;
        }


        public string GetVisgroupName(int visgroupId)
        {
            if (visgroupId == Jerc)
                return VisgroupNames.Jerc;
            if (visgroupId == JercRemove)
                return VisgroupNames.JercRemove;
            if (visgroupId == JercPath)
                return VisgroupNames.JercPath;
            if (visgroupId == JercCover)
                return VisgroupNames.JercCover;
            if (visgroupId == JercOverlap)
                return VisgroupNames.JercOverlap;
            if (visgroupId == JercDoor)
                return VisgroupNames.JercDoor;
            if (visgroupId == JercLadder)
                return VisgroupNames.JercLadder;
            if (visgroupId == JercDanger)
                return VisgroupNames.JercDanger;

            Logger.LogWarning("Could not find visgroup name using visgroup ID");

            return null;
        }
    }
}
