using System;
using System.Collections.Generic;
using System.Text;
using VMFParser;

namespace JAR.Models
{
    public class VmfRequiredData
    {
        public IEnumerable<IVNode> propsLayout;
        public IEnumerable<IVNode> propsCover;
        public IEnumerable<IVNode> propsNegative;
        public IEnumerable<IVNode> propsOverlap;

        public IEnumerable<IVNode> brushesLayout;
        public IEnumerable<IVNode> brushesCover;
        public IEnumerable<IVNode> brushesNegative;
        public IEnumerable<IVNode> brushesOverlap;

        public VmfRequiredData(IEnumerable<IVNode> propsLayout, IEnumerable<IVNode> propsCover, IEnumerable<IVNode> propsNegative, IEnumerable<IVNode> propsOverlap, IEnumerable<IVNode> brushesLayout, IEnumerable<IVNode> brushesCover, IEnumerable<IVNode> brushesNegative, IEnumerable<IVNode> brushesOverlap)
        {
            this.propsLayout = propsLayout;
            this.propsCover = propsCover;
            this.propsNegative = propsNegative;
            this.propsOverlap = propsOverlap;

            this.brushesLayout = brushesLayout;
            this.brushesCover = brushesCover;
            this.brushesNegative = brushesNegative;
            this.brushesOverlap = brushesOverlap;
        }
    }
}
