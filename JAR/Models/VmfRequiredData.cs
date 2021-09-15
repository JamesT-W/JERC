using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VMFParser;

namespace JAR.Models
{
    public class VmfRequiredData
    {
        public List<Prop> propsLayout;
        public List<Prop> propsCover;
        public List<Prop> propsNegative;
        public List<Prop> propsOverlap;

        public List<Brush> brushesLayout;
        public List<Brush> brushesCover;
        public List<Brush> brushesNegative;
        public List<Brush> brushesOverlap;

        public VmfRequiredData(IEnumerable<IVNode> propsLayout, IEnumerable<IVNode> propsCover, IEnumerable<IVNode> propsNegative, IEnumerable<IVNode> propsOverlap, IEnumerable<IVNode> brushesLayout, IEnumerable<IVNode> brushesCover, IEnumerable<IVNode> brushesNegative, IEnumerable<IVNode> brushesOverlap)
        {
            this.propsLayout = propsLayout.Select(x => new Prop(x)).ToList();
            this.propsCover = propsCover.Select(x => new Prop(x)).ToList();
            this.propsNegative = propsNegative.Select(x => new Prop(x)).ToList();
            this.propsOverlap = propsOverlap.Select(x => new Prop(x)).ToList();

            this.brushesLayout = brushesLayout.Select(x => new Brush(x)).ToList();
            this.brushesCover = brushesCover.Select(x => new Brush(x)).ToList();
            this.brushesNegative = brushesNegative.Select(x => new Brush(x)).ToList();
            this.brushesOverlap = brushesOverlap.Select(x => new Brush(x)).ToList();
        }
    }
}
