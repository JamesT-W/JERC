using System;
using System.Collections.Generic;
using System.Text;

namespace JERC.Constants
{
    public static class TextureNames
    {
        public static readonly string RemoveTextureName = "jerc/remove";
        public static readonly string PathTextureName = "jerc/path";
        public static readonly string CoverTextureName = "jerc/cover";
        public static readonly string OverlapTextureName = "jerc/overlap";
        public static readonly string DoorTextureName = "jerc/door";
        public static readonly List<string> LadderTextureNames = new List<string>() { "jerc/ladder", "tools/toolsinvisibleladder" };
        public static readonly string DangerTextureName = "jerc/danger";

        public static readonly string IgnoreTextureName = "jerc/ignore";

        public static readonly string JercBoxTextureName = "jerc/jerc_box";

        public static readonly string BuyzoneTextureName = "jerc/buyzone";
        public static readonly string BombsiteATextureName = "jerc/bombsite_a";
        public static readonly string BombsiteBTextureName = "jerc/bombsite_b";
        public static readonly string RescueZoneTextureName = "jerc/rescue_zone";
        public static readonly string HostageTextureName = "jerc/hostage";
        public static readonly string TSpawnTextureName = "jerc/t_spawn";
        public static readonly string CTSpawnTextureName = "jerc/ct_spawn";

        public static readonly List<string> AllBombsiteTextureNames = new List<string> { BombsiteATextureName, BombsiteBTextureName };
    }
}
