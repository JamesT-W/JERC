using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMFParser;

namespace JERC.Models
{
    public class DispInfo
    {
        public int power;
        public string startPosition;
        public int flags;
        public float elevation;
        public float subdiv;
        public List<IVNode> normals;
        public List<IVNode> distances;
        public List<IVNode> offsets;
        public List<IVNode> offset_normals;
        public List<IVNode> alphas;
        public List<IVNode> triangle_tags;
        public List<IVNode> allowed_verts;


        public DispInfo(IVNode dispInfo)
        {
            power = int.Parse(dispInfo.Body.FirstOrDefault(x => x.Name == "power")?.Value);
            startPosition = dispInfo.Body.FirstOrDefault(x => x.Name == "startPosition")?.Value;
            flags = int.Parse(dispInfo.Body.FirstOrDefault(x => x.Name == "flags")?.Value);
            elevation = float.Parse(dispInfo.Body.FirstOrDefault(x => x.Name == "elevation")?.Value);
            subdiv = float.Parse(dispInfo.Body.FirstOrDefault(x => x.Name == "subdiv")?.Value);

            normals = dispInfo.Body.Where(x => x.Name == "normals")?.ToList();
            distances = dispInfo.Body.Where(x => x.Name == "distances")?.ToList();
            offsets = dispInfo.Body.Where(x => x.Name == "offsets")?.ToList();
            offset_normals = dispInfo.Body.Where(x => x.Name == "offset_normals")?.ToList();
            alphas = dispInfo.Body.Where(x => x.Name == "alphas")?.ToList();
            triangle_tags = dispInfo.Body.Where(x => x.Name == "triangle_tags")?.ToList();
            allowed_verts = dispInfo.Body.Where(x => x.Name == "allowed_verts")?.ToList();
        }
    }
}
