using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VMFParser;

namespace JAR.Models
{
    public class Entity
    {
        public int id;
        public string classname;
        public string angles;
        public string model;
        public int uniformscale;
        public string origin;
        public Editor editor;

        public Entity(IVNode prop)
        {
            // ignoring a lot of other values

            id = int.Parse(prop.Body.FirstOrDefault(x => x.Name == "id")?.Value);
            classname = prop.Body.FirstOrDefault(x => x.Name == "classname")?.Value;
            angles = prop.Body.FirstOrDefault(x => x.Name == "angles")?.Value;
            model = prop.Body.FirstOrDefault(x => x.Name == "model")?.Value;
            uniformscale = int.Parse(prop.Body.FirstOrDefault(x => x.Name == "uniformscale")?.Value);
            origin = prop.Body.FirstOrDefault(x => x.Name == "origin")?.Value;
            editor = new Editor(prop.Body.FirstOrDefault(x => x.Name == "editor"));
        }
    }
}
