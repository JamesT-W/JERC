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
    }
}
