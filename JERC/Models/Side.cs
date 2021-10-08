using JERC.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VMFParser;

namespace JERC.Models
{
    public class Side
    {
        public int id;
        public string plane;
        public List<Vertices> vertices_plus;
        public string material;
        public string uaxis;
        public string vaxis;
        public int rotation;
        public int smoothing_groups;
        public DispInfo dispinfo;

        public Side(IVNode side)
        {
            id = int.Parse(side.Body.FirstOrDefault(x => x.Name == "id")?.Value);
            plane = side.Body.FirstOrDefault(x => x.Name == "plane")?.Value;
            vertices_plus = side.Body.FirstOrDefault(x => x.Name == "vertices_plus")?.Body.Select(x => new Vertices(x.Value)).ToList();
            material = side.Body.FirstOrDefault(x => x.Name == "material")?.Value;
            uaxis = side.Body.FirstOrDefault(x => x.Name == "uaxis")?.Value;
            vaxis = side.Body.FirstOrDefault(x => x.Name == "vaxis")?.Value;
            rotation = int.Parse(side.Body.FirstOrDefault(x => x.Name == "rotation")?.Value, Globalization.Style, Globalization.Culture);
            smoothing_groups = int.Parse(side.Body.FirstOrDefault(x => x.Name == "smoothing_groups")?.Value);

            var displacementsStuff = side.Body.FirstOrDefault(x => x.Name == "dispinfo");
            if (displacementsStuff != null)
                dispinfo = new DispInfo(displacementsStuff);
        }
    }
}
