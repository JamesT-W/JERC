using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VMFParser;

namespace JERC.Models
{
    public class Brush
    {
        public int id;
        public List<Side> side;
        public Editor editor;


        public Brush(IVNode brush)
        {
            id = int.Parse(brush.Body.FirstOrDefault(x => x.Name == "id")?.Value);
            side = brush.Body.Where(x => x.Name == "side")?.Select(x => new Side(x, id)).ToList();
            editor = new Editor(brush.Body.FirstOrDefault(x => x.Name == "editor"));
        }
    }
}
