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
        public int brushId;
        public string plane;
        public List<Vertices> vertices_plus;
        public string material;
        public string uaxis;
        public string vaxis;
        public float rotation;
        public int smoothing_groups;
        public DispInfo dispinfo;

        public bool fakeBrushSideCreatedFromOverlay;

        public static List<int> allBrushSideIds = new List<int>();

        private static int nextOverrideSideId = 2000000;


        public Side(IVNode side, int brushId)
        {
            id = int.Parse(side.Body.FirstOrDefault(x => x.Name == "id")?.Value);
            if (id == 0) // vertices added with ctrl+f in the vertex tool have an id set as '0'
            {
                id = nextOverrideSideId; //TODO: bad way to handle setting a random id, as this might already exist

                nextOverrideSideId++;
            }


            this.brushId = brushId;
            plane = side.Body.FirstOrDefault(x => x.Name == "plane")?.Value;
            vertices_plus = side.Body.FirstOrDefault(x => x.Name == "vertices_plus")?.Body.Select(x => new Vertices(x.Value)).ToList();
            material = side.Body.FirstOrDefault(x => x.Name == "material")?.Value;
            uaxis = side.Body.FirstOrDefault(x => x.Name == "uaxis")?.Value;
            vaxis = side.Body.FirstOrDefault(x => x.Name == "vaxis")?.Value;
            rotation = float.Parse(side.Body.FirstOrDefault(x => x.Name == "rotation")?.Value, Globalization.Style, Globalization.Culture);
            smoothing_groups = int.Parse(side.Body.FirstOrDefault(x => x.Name == "smoothing_groups")?.Value);

            var dispinfoIVNode = side.Body.FirstOrDefault(x => x.Name == "dispinfo");
            if (dispinfoIVNode != null)
                dispinfo = new DispInfo(dispinfoIVNode);

            allBrushSideIds.Add(id);

            fakeBrushSideCreatedFromOverlay = false;
        }


        // for overlays becoming fake brushes
        public Side(int brushId, List<Vertices> vertices)
        {
            var newIdTrying = int.MaxValue - brushId;
            while (allBrushSideIds.Any(x => x == newIdTrying))
            {
                newIdTrying--;

                if (newIdTrying < 0)
                {
                    newIdTrying = int.MaxValue - 1;
                }
            }
            id = newIdTrying;

            this.brushId = brushId;
            plane = string.Concat(vertices[0].GetPlaneFormatForSingleVertices(), " ", vertices[1].GetPlaneFormatForSingleVertices(), " ", vertices[2].GetPlaneFormatForSingleVertices()); // randomly leave out vertices3
            vertices_plus = vertices;
            //// ignore the rest of the variables ////

            fakeBrushSideCreatedFromOverlay = true;
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
