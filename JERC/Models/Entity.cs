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
        public string classname;
        public string angles;
        public string model;
        public int? uniformscale;
        public string origin;
        public Editor editor;

        public List<Brush> brushes;

        public Entity(IVNode entity)
        {
            // ignoring a lot of other values

            id = int.Parse(entity.Body.FirstOrDefault(x => x.Name == "id")?.Value);
            classname = entity.Body.FirstOrDefault(x => x.Name == "classname")?.Value;
            angles = entity.Body.FirstOrDefault(x => x.Name == "angles")?.Value;
            model = entity.Body.FirstOrDefault(x => x.Name == "model")?.Value;
            uniformscale = entity.Body.Any(x => x.Name == "uniformscale") ? int.Parse(entity.Body.FirstOrDefault(x => x.Name == "uniformscale")?.Value) : null;
            origin = entity.Body.FirstOrDefault(x => x.Name == "origin")?.Value;
            editor = new Editor(entity.Body.FirstOrDefault(x => x.Name == "editor"));

            brushes = entity.Body.Any(x => x.Name == "solid") ? entity.Body.Where(x => x.Name == "solid").Select(x => new Brush(x)).ToList() : null;
        }
    }
}
