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
        AfterJERCBrushesForEntities = 3, //(eg. Bombsite A Material)
        AfterBrushEntities = 4 //(eg. func_buyzone)
    }
}
