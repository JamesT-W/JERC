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
        BombsiteA = 512,
        BombsiteB = 1024,
        RescueZone = 2048,
        Hostage = 4096,
        TSpawn = 8192,
        CTSpawn = 16384
    }
}
