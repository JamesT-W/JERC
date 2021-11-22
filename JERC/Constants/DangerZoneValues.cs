using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JERC.Constants
{
    public static class DangerZoneValues
    {
        public static int DangerZoneOverviewSize => 20480;
        public static int DangerZonePlayareaSize => 16384;
        public static int DangerZonePaddingSize => (DangerZoneOverviewSize - DangerZonePlayareaSize);
    }
}
