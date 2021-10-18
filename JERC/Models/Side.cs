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
        public int brushId;
        public int id;
        public string plane;
        public List<Vertices> vertices_plus;
        public string material;
        public string uaxis;
        public string vaxis;
        public int rotation;
        public int smoothing_groups;
        public DispInfo dispinfo;


        public Side(IVNode side, int brushId)
        {
            this.brushId = brushId;
            id = int.Parse(side.Body.FirstOrDefault(x => x.Name == "id")?.Value);
            plane = side.Body.FirstOrDefault(x => x.Name == "plane")?.Value;
            vertices_plus = side.Body.FirstOrDefault(x => x.Name == "vertices_plus")?.Body.Select(x => new Vertices(x.Value)).ToList();
            material = side.Body.FirstOrDefault(x => x.Name == "material")?.Value;
            uaxis = side.Body.FirstOrDefault(x => x.Name == "uaxis")?.Value;
            vaxis = side.Body.FirstOrDefault(x => x.Name == "vaxis")?.Value;
            rotation = int.Parse(side.Body.FirstOrDefault(x => x.Name == "rotation")?.Value, Globalization.Style, Globalization.Culture);
            smoothing_groups = int.Parse(side.Body.FirstOrDefault(x => x.Name == "smoothing_groups")?.Value);

            var dispinfoIVNode = side.Body.FirstOrDefault(x => x.Name == "dispinfo");
            if (dispinfoIVNode != null)
                dispinfo = new DispInfo(dispinfoIVNode);
        }


        public bool isDisplacement
        {
            get
            {
                return dispinfo != null;
            }
        }
    }
}
