using JERC.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMFParser;

namespace JERC.Models
{
    public class FuncInstance
    {
        public int id;
        public string classname;
        public Angle angles;
        public string file;
        public int fixup_style;
        public Vertices origin;
        public Editor editor;

        public FuncInstance(IVNode entity)
        {
            id = int.Parse(entity.Body.FirstOrDefault(x => x.Name == "id")?.Value);
            classname = entity.Body.FirstOrDefault(x => x.Name == "classname")?.Value;
            angles = entity.Body.Where(x => x.Name == "angles")?.Select(x => new Angle(x.Value)).FirstOrDefault();

            file = entity.Body.FirstOrDefault(x => x.Name == "file")?.Value;
            if (new string(file.TakeLast(4).ToArray()).ToLower() != ".vmf")
                file += ".vmf";

            int.TryParse(entity.Body.FirstOrDefault(x => x.Name == "fixup_style")?.Value, Globalization.Style, Globalization.Culture, out fixup_style);
            origin = entity.Body.Where(x => x.Name == "origin")?.Select(x => new Vertices(x.Value)).FirstOrDefault();
            editor = new Editor(entity.Body.FirstOrDefault(x => x.Name == "editor"));
        }
    }
}
