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
        public List<int> visgroupidList = new List<int>();
        public int? visgroupshown;
        public int visgroupautoshown;
        public string logicalpos; // entities, not brushes ?

        public Editor(IVNode side)
        {
            color = side.Body.FirstOrDefault(x => x.Name == "color")?.Value;

            var allVisgroupIds = side.Body.Any(x => x.Name == "visgroupid") ? side.Body.Where(x => x.Name == "visgroupid").Select(x => int.Parse(x.Value)).ToList() : new List<int>();
            foreach (var visgroupId in allVisgroupIds)
            {
                visgroupidList.Add(visgroupId);
            }

            visgroupshown = side.Body.Any(x => x.Name == "visgroupshown") ? int.Parse(side.Body.FirstOrDefault(x => x.Name == "visgroupshown")?.Value) : null;
            visgroupautoshown = int.Parse(side.Body.FirstOrDefault(x => x.Name == "visgroupautoshown")?.Value);
            logicalpos = side.Body.FirstOrDefault(x => x.Name == "logicalpos")?.Value;
        }
    }
}
