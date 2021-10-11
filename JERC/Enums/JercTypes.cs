using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Enums
{
    public enum JercTypes
    {
        None = 1,
        Remove = 2,
        Path = 4,
        Cover = 8,
        Overlap = 16,
        Door = 32,
        Ladder = 64,
        Ignore = 128,
        Buyzone = 256,
        Bombsite = 512,
        BombsiteA = 1024,
        BombsiteB = 2048,
        RescueZone = 4096,
        Hostage = 8192,
        TSpawn = 16384,
        CTSpawn = 32768
    }
}
