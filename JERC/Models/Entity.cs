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

        // jerc_box
        public string orderNum;
        public string rendercolor;
        public string colourAlpha;
        public string colourStroke;
        public string colourStrokeAlpha;
        public string strokeWidth;

        public Entity(IVNode entity)
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

            brushes = entity.Body.Any(x => x.Name == "solid") ? entity.Body.Where(x => x.Name == "solid").Select(x => new Brush(x)).ToList() : null;

            // jerc_box
            orderNum = entity.Body.FirstOrDefault(x => x.Name == "orderNum")?.Value;
            rendercolor = entity.Body.FirstOrDefault(x => x.Name == "rendercolor")?.Value;
            colourAlpha = entity.Body.FirstOrDefault(x => x.Name == "colourAlpha")?.Value;
            colourStroke = entity.Body.FirstOrDefault(x => x.Name == "colourStroke")?.Value;
            colourStrokeAlpha = entity.Body.FirstOrDefault(x => x.Name == "colourStrokeAlpha")?.Value;
            strokeWidth = entity.Body.FirstOrDefault(x => x.Name == "strokeWidth")?.Value;
        }
    }
}
