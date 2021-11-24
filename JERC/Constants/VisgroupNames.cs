
namespace JERC.Constants
{
    public static class VisgroupNames
    {
        // texture mode
        public static readonly string Jerc = "jerc";

        // separated visgroups mode
        public static readonly string JercRemove = "jerc_remove";
        public static readonly string JercPath = "jerc_path";
        public static readonly string JercCover = "jerc_cover";
        public static readonly string JercOverlap = "jerc_overlap";

        public static readonly string JercDoor = "jerc_door";
        public static readonly string JercLadder = "jerc_ladder";
        public static readonly string JercDanger = "jerc_danger";

        // everything else should just go in the jerc visgroup (bound to the correct entity)
    }
}
