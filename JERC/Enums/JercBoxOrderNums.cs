using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Enums
{
    public enum JercBoxOrderNums
    {
        None = -1,

        First = 0,
        BetweenPathAndOverlapBrushes = 1,
        AfterJERCBrushesAndDisplacements = 2,
        AfterBrushEntities = 3, //(eg. func_buyzone)
        AfterJERCBrushesForEntities = 4 //(eg. Bombsite A Material)
    }
}
