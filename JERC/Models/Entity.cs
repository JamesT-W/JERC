using JERC.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VMFParser;

namespace JERC.Models
{
    public class Entity
    {
        public int id;
        public string targetname;
        public string classname;
        public string angles;
        public string model;
        public int? uniformscale;
        public string origin;
        public Editor editor;

        public List<Brush> brushes;

        // info_overlay
        public string basisNormal;
        public string basisOrigin;
        public string basisU;
        public string basisV;
        public string endU;
        public string endV;
        public string fademindist;
        public string fademaxdist;
        public string material;
        public string sides;
        public string startU;
        public string startV;
        public string uv0;
        public string uv1;
        public string uv2;
        public string uv3;
        public string renderOrder;

        // jerc_info_overlay
        public string orderNum;
        public bool overrideColour;
        public string rendercolor;
        public string colourAlpha;
        public string colourStroke;
        public string colourStrokeAlpha;
        public string strokeWidth;

        // overlays
        public string jim_vertices0;
        public string jim_vertices1;
        public string jim_vertices2;
        public string jim_vertices3;

        // jerc_box
        //public string orderNum;
        //public string rendercolor;
        //public string colourAlpha;
        //public string colourStroke;
        //public string colourStrokeAlpha;
        //public string strokeWidth;


        public Entity(IVNode entity, bool isOverlay = false)
        {
            // ignoring a lot of other values

            id = int.Parse(entity.Body.FirstOrDefault(x => x.Name == "id")?.Value);
            targetname = entity.Body.FirstOrDefault(x => x.Name == "targetname")?.Value;
            classname = entity.Body.FirstOrDefault(x => x.Name == "classname")?.Value;
            angles = entity.Body.FirstOrDefault(x => x.Name == "angles")?.Value;
            model = entity.Body.FirstOrDefault(x => x.Name == "model")?.Value;
            uniformscale = entity.Body.Any(x => x.Name == "uniformscale") ? int.Parse(entity.Body.FirstOrDefault(x => x.Name == "uniformscale")?.Value, Globalization.Style, Globalization.Culture) : null;
            origin = entity.Body.FirstOrDefault(x => x.Name == "origin")?.Value;
            editor = new Editor(entity.Body.FirstOrDefault(x => x.Name == "editor"));


            brushes = entity.Body.Any(x => x.Name == "solid") ? entity.Body.Where(x => x.Name == "solid").Select(x => new Brush(x)).ToList() : new List<Brush>();


            // info_overlay
            basisNormal = entity.Body.FirstOrDefault(x => x.Name == "BasisNormal")?.Value;
            basisOrigin = entity.Body.FirstOrDefault(x => x.Name == "BasisOrigin")?.Value;
            basisU = entity.Body.FirstOrDefault(x => x.Name == "BasisU")?.Value;
            basisV = entity.Body.FirstOrDefault(x => x.Name == "BasisV")?.Value;
            endU = entity.Body.FirstOrDefault(x => x.Name == "EndU")?.Value;
            endV = entity.Body.FirstOrDefault(x => x.Name == "EndV")?.Value;
            fademindist = entity.Body.FirstOrDefault(x => x.Name == "fademindist")?.Value;
            fademaxdist = entity.Body.FirstOrDefault(x => x.Name == "fademaxdist")?.Value;
            material = entity.Body.FirstOrDefault(x => x.Name == "material")?.Value;
            sides = entity.Body.FirstOrDefault(x => x.Name == "sides")?.Value;
            startU = entity.Body.FirstOrDefault(x => x.Name == "StartU")?.Value;
            startV = entity.Body.FirstOrDefault(x => x.Name == "StartV")?.Value;
            uv0 = entity.Body.FirstOrDefault(x => x.Name == "uv0")?.Value;
            uv1 = entity.Body.FirstOrDefault(x => x.Name == "uv1")?.Value;
            uv2 = entity.Body.FirstOrDefault(x => x.Name == "uv2")?.Value;
            uv3 = entity.Body.FirstOrDefault(x => x.Name == "uv3")?.Value;
            renderOrder = entity.Body.FirstOrDefault(x => x.Name == "RenderOrder")?.Value;

            // jerc_info_overlay
            orderNum = entity.Body.FirstOrDefault(x => x.Name == "orderNum")?.Value;
            overrideColour = entity.Body.Any(x => x.Name == "overrideColour") && int.Parse(entity.Body.FirstOrDefault(x => x.Name == "overrideColour")?.Value) == 1;
            rendercolor = entity.Body.FirstOrDefault(x => x.Name == "rendercolor")?.Value;
            colourAlpha = entity.Body.FirstOrDefault(x => x.Name == "colourAlpha")?.Value;
            colourStroke = entity.Body.FirstOrDefault(x => x.Name == "colourStroke")?.Value;
            colourStrokeAlpha = entity.Body.FirstOrDefault(x => x.Name == "colourStrokeAlpha")?.Value;
            strokeWidth = entity.Body.FirstOrDefault(x => x.Name == "strokeWidth")?.Value;

            // overlays
            jim_vertices0 = entity.Body.FirstOrDefault(x => x.Name == "jim_vertices0")?.Value;
            jim_vertices1 = entity.Body.FirstOrDefault(x => x.Name == "jim_vertices1")?.Value;
            jim_vertices2 = entity.Body.FirstOrDefault(x => x.Name == "jim_vertices2")?.Value;
            jim_vertices3 = entity.Body.FirstOrDefault(x => x.Name == "jim_vertices3")?.Value;


            // make sure uv values and overlay are already set
            if (isOverlay)
            {
                CreateFakeBrushesFromOverlay();
            }


            // jerc_box
            //orderNum = entity.Body.FirstOrDefault(x => x.Name == "orderNum")?.Value;
            //rendercolor = entity.Body.FirstOrDefault(x => x.Name == "rendercolor")?.Value;
            //colourAlpha = entity.Body.FirstOrDefault(x => x.Name == "colourAlpha")?.Value;
            //colourStroke = entity.Body.FirstOrDefault(x => x.Name == "colourStroke")?.Value;
            //colourStrokeAlpha = entity.Body.FirstOrDefault(x => x.Name == "colourStrokeAlpha")?.Value;
            //strokeWidth = entity.Body.FirstOrDefault(x => x.Name == "strokeWidth")?.Value;
        }


        private void CreateFakeBrushesFromOverlay()
        {
            //var fakeBrush = new Brush(id, true, new Vertices(jim_vertices0, origin), new Vertices(jim_vertices1, origin), new Vertices(jim_vertices2, origin), new Vertices(jim_vertices3, origin));
            var fakeBrush = new Brush(id, true, new Vertices(jim_vertices0), new Vertices(jim_vertices1), new Vertices(jim_vertices2), new Vertices(jim_vertices3));
            brushes.Add(fakeBrush);
        }
    }
}
