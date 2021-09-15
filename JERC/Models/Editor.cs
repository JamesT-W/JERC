using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VMFParser;

namespace JERC.Models
{
    public class Editor
    {
        public string color;
        public int visgroupid;
        public int visgroupshown;
        public int visgroupautoshown;
        public string logicalpos; // entities, not brushes ?

        public Editor(IVNode side)
        {
            color = side.Body.FirstOrDefault(x => x.Name == "color")?.Value;
            visgroupid = int.Parse(side.Body.FirstOrDefault(x => x.Name == "visgroupid")?.Value);
            visgroupshown = int.Parse(side.Body.FirstOrDefault(x => x.Name == "visgroupshown")?.Value);
            visgroupautoshown = int.Parse(side.Body.FirstOrDefault(x => x.Name == "visgroupautoshown")?.Value);
            logicalpos = side.Body.FirstOrDefault(x => x.Name == "logicalpos")?.Value;
        }
    }
}
